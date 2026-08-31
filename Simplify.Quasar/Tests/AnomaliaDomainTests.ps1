param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\bin\Simplify.Quasar.dll')
)

$ErrorActionPreference = 'Stop'

function Assert-Equal($expected, $actual, [string]$message) {
    if ($expected -ne $actual) {
        throw "$message Esperado: '$expected'; obtido: '$actual'."
    }
}

function Assert-Throws([scriptblock]$action, [string]$message) {
    try {
        & $action
    } catch {
        return
    }
    throw "$message Era esperada uma exceção."
}

[System.Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath)) | Out-Null
$npoiAssembly = Join-Path (Split-Path -Parent $AssemblyPath) 'NPOI.Core.dll'
[System.Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $npoiAssembly)) | Out-Null

$prazo = New-Object Simplify.Quasar.Areas.AnomaliaApp.Services.AnomaliaPrazoService
$limite = $prazo.CalcularDataLimite([datetime]'2026-09-01', 7)
Assert-Equal '2026-09-08' $limite.ToString('yyyy-MM-dd') 'Prazo inclusivo incorreto.'
Assert-Equal 7 $prazo.CalcularDiasDecorridos([datetime]'2026-09-01 23:59:59', [datetime]'2026-09-08 00:00:01') 'Dias decorridos incorretos.'
$prazo.Validar([datetime]'2026-09-01', 7, [datetime]'2026-09-08 23:59:59')
Assert-Throws { $prazo.Validar([datetime]'2026-09-01', 7, [datetime]'2026-09-09') } 'Prazo vencido foi aceito.'

$saldo = New-Object Simplify.Quasar.Areas.AnomaliaApp.Services.AnomaliaSaldoService
$tipoA = $saldo.Calcular('A', [decimal]2, $null, [decimal]1)
Assert-Equal ([decimal]1) $tipoA.SaldoDisponivel 'Saldo do tipo A incorreto.'
Assert-Throws { $saldo.ValidarQuantidade([decimal]1.0001, $tipoA) } 'Sobrerreclamação do tipo A foi aceita.'
$saldoInteiro = $saldo.Calcular('A', [decimal]2, $null, [decimal]0)
Assert-Throws { $saldo.ValidarQuantidade([decimal]1.5, $saldoInteiro) } 'Quantidade reclamada fracionada foi aceita.'
$saldo.ValidarQuantidade([decimal]2, $saldoInteiro)

$tipoB = $saldo.Calcular('B', [decimal]10, [decimal]13, [decimal]1)
Assert-Equal ([decimal]2) $tipoB.SaldoDisponivel 'Saldo de excesso do tipo B incorreto.'
Assert-Throws { $saldo.Calcular('B', [decimal]10, [decimal]9, [decimal]0) } 'Excesso sem quantidade recebida superior foi aceito.'

$excel = New-Object Simplify.Quasar.Areas.AnomaliaApp.Services.AnomaliaExcelService
$entradaType = [Simplify.Quasar.Areas.AnomaliaApp.Services.AnomaliaArquivoItemEntrada]
$listType = [System.Collections.Generic.List``1].MakeGenericType($entradaType)

function New-Itens([string]$tipo, [int]$quantidade) {
    $list = [Activator]::CreateInstance($listType)
    1..$quantidade | ForEach-Object {
        $item = New-Object Simplify.Quasar.Areas.AnomaliaApp.Services.AnomaliaArquivoItemEntrada
        $item.AnomaliaItemId = $_
        $item.TipoCodigo = $tipo
        $list.Add($item)
    }
    Write-Output -NoEnumerate $list
}

$lotesA = @($excel.PrepararLotes((New-Itens 'A' 23), $false))
Assert-Equal '5,5,5,5,3' (($lotesA | ForEach-Object { $_.ItemIds.Count }) -join ',') 'Lotes A incorretos.'
$lotesG = @($excel.PrepararLotes((New-Itens 'G' 12), $true))
Assert-Equal '10,2' (($lotesG | ForEach-Object { $_.ItemIds.Count }) -join ',') 'Lotes G incorretos.'
Assert-Equal $true (($lotesG | Where-Object { -not $_.Reenvio }).Count -eq 0) 'Lote de reenvio perdeu sua identificação.'

$reenvioSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\Areas\AnomaliaApp\Services\AnomaliaReenvioService.cs') -Raw
if ($reenvioSource -match 'INSERT\s+INTO\s+AnomaliaGmItem') {
    throw 'O serviço de reenvio não pode inserir AnomaliaGmItem, pois isso consumiria saldo novamente.'
}

function Assert-TemplateFormulario(
    [string]$fileName,
    [string]$sheetName,
    [string]$firstItemCell,
    [string]$lastItemCell
) {
    $path = Join-Path $PSScriptRoot "..\App_Data\Templates\$fileName"
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Modelo oficial não localizado: $fileName."
    }

    $stream = [System.IO.File]::OpenRead((Resolve-Path -LiteralPath $path))
    try {
        $workbook = New-Object NPOI.HSSF.UserModel.HSSFWorkbook($stream)
        $sheet = $workbook.GetSheet($sheetName)
        $source = $workbook.GetSheet('Processo1')
        if ($null -eq $sheet -or $null -eq $source) {
            throw "O modelo $fileName não contém as abas esperadas."
        }

        function Get-Cell([object]$targetSheet, [string]$address) {
            $match = [regex]::Match($address, '^([A-Z]+)(\d+)$')
            $column = 0
            foreach ($character in $match.Groups[1].Value.ToCharArray()) {
                $column = ($column * 26) + ([int]$character - [int][char]'A' + 1)
            }
            return $targetSheet.GetRow(([int]$match.Groups[2].Value) - 1).GetCell($column - 1)
        }

        Assert-Equal 'Formula' ((Get-Cell $sheet $firstItemCell).CellType.ToString()) "Primeira linha do modelo $fileName incompatível."
        Assert-Equal 'Formula' ((Get-Cell $sheet $lastItemCell).CellType.ToString()) "Capacidade do modelo $fileName incompatível."
        Assert-Equal 'Formula' ((Get-Cell $sheet 'P5').CellType.ToString()) "Data do processo no modelo $fileName incompatível."
        Assert-Equal 'Formula' ((Get-Cell $sheet 'B8').CellType.ToString()) "Código GM no modelo $fileName incompatível."

        foreach ($row in $sheet) {
            foreach ($cell in $row.Cells) {
                if ($cell.CellType.ToString() -eq 'Formula' -and
                    $cell.CellFormula -match '#REF!' -and
                    -not ($fileName -eq 'Formulario Anomalias GM.xls' -and $cell.Address.ToString() -eq 'K22')) {
                    throw "O modelo $fileName contém referência inválida em $($cell.Address)."
                }
            }
        }
    } finally {
        if ($null -ne $workbook) { $workbook.Close() }
        $stream.Dispose()
    }
}

Assert-TemplateFormulario 'Formulario Anomalias GM.xls' 'anomalia de A a D' 'G17' 'G21'
Assert-TemplateFormulario 'Formulario Danificados GM.xls' 'anomalia de G a J' 'G16' 'G25'

Write-Output 'OK - testes de domínio de Anomalias GM aprovados.'
