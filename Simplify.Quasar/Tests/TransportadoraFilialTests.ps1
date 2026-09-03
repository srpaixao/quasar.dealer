$ErrorActionPreference = 'Stop'

$controllerPath = Join-Path $PSScriptRoot '..\Areas\ExpedicaoApp\Controllers\TransportadoraController.cs'
$source = Get-Content -LiteralPath $controllerPath -Raw -Encoding UTF8

$createStart = $source.IndexOf('public ActionResult Create(TransportadoraViewModel vm)', [System.StringComparison]::Ordinal)
$editStart = $source.IndexOf('public ActionResult Edit(TransportadoraViewModel vm)', [System.StringComparison]::Ordinal)

if ($createStart -lt 0 -or $editStart -le $createStart) {
    throw 'Não foi possível localizar o fluxo POST de criação da transportadora.'
}

$createSource = $source.Substring($createStart, $editStart - $createStart)
$filialAssignment = $createSource.IndexOf('transportadora.FilialId = filialId;', [System.StringComparison]::Ordinal)
$addEntity = $createSource.IndexOf('db.Transportadora.Add(transportadora);', [System.StringComparison]::Ordinal)
$saveChanges = $createSource.IndexOf('db.SaveChanges();', [System.StringComparison]::Ordinal)

if ($filialAssignment -lt 0) {
    throw 'O cadastro da transportadora não atribui a filial atual.'
}

if ($filialAssignment -gt $addEntity -or $addEntity -gt $saveChanges) {
    throw 'A filial deve ser atribuída antes da inclusão e persistência da transportadora.'
}

Write-Output 'OK - cadastro de Transportadora atribui a filial atual antes da persistência.'
