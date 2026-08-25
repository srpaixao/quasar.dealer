# Atualizacoes Operacionais - 20/07/2026

## Objetivo

Consolidar as regras entregues para recebimento, devolucao, conferencia de volumes e importacao/impressao de etiquetas de transportadora.

## Recebimento de Notas Fiscais

- Para processos que nao sejam devolucao (`TipoId <> 2`), uma NF nao pode repetir a combinacao `FilialId + Movimento + Numero`.
- A mesma numeracao continua permitida quando a filial ou o movimento forem diferentes.
- Devolucoes (`TipoId = 2`) podem repetir NF e itens.
- O upload de recebimento deve ser idempotente para `NotaFiscal` e `NotaFiscalItem`.
- A tela `Recebimento Notas Fiscais` exibe a ultima atualizacao e, abaixo dela, o nome do ultimo arquivo processado.

Script de banco relacionado:

- `sql/20260718_Recebimento_NotaFiscal_Unicidade.sql`

## Devolucao

- A tela `Devolucao Detalhes do Processo` possui o botao `Excluir` no rodape.
- Antes da exclusao, o sistema apresenta o popup padrao de confirmacao.
- A confirmacao remove o processo de `Devolucao`, `DevolucaoItem` e `DevolucaoComplemento`.

## Conferencia de Volumes no Recebimento

- A conferencia armazena quantidade conferida, usuario e data/hora.
- O botao `Confirmar` conclui a conferencia; a tela web antiga nao apresenta um flag separado.
- Depois de finalizar todos os itens do volume, a grade e o numero pesquisado sao limpos e o foco retorna para uma nova consulta.
- A tela operacional nao exibe quantidade armazenada.
- A consulta por volume apresenta: `Nota Fiscal`, `Item Nr`, `Descricao`, `Faturado`, `Conferido`, `Conferente`, `Diferenca`, `Armazenado`, `Estoquista` e `Status`.
- Conferente/estoquista e suas datas ficam agrupados na mesma celula.
- Diferencas diferentes de zero sao exibidas em vermelho.
- A tela `Recebimento > Historico` apresenta `DataHora` exatamente no horario local gravado em `HistoricoRecebimento`, sem conversao UTC no navegador.

Script de banco relacionado:

- `sql/20260718_Conferencia_Volume_Quantidades.sql`

## Importacao de Arquivo de Transportadora

### Regras de dados

- NFs ja existentes em `DocExpedicao`, na mesma filial, nao sao gravadas novamente e nao geram novas etiquetas.
- A comparacao normaliza zeros a esquerda, espacos, pontuacao e chaves de acesso de 44 digitos.
- Linhas repetidas extraidas do PDF sao deduplicadas por NF, contato e volume.
- `NotaFiscalTransportadora` guarda apenas o ultimo lote processado da filial; um upload valido substitui o lote anterior.
- A grade permanece mostrando o ultimo lote tanto na impressao automatica quanto na manual.
- Se o upload valido nao gerar etiquetas, a grade fica vazia e o popup padrao informa somente `Nenhuma etiqueta foi gerada.`

### Impressao parametrizada

Todos os dados de infraestrutura sao obtidos dos cadastros:

| Origem | Parametro/campo |
|---|---|
| `AppConfig` | `ImprimirDireto` |
| `AppConfig` | `ImpressoraPadrao` |
| `AppConfig` | `PrinterServerIP` |
| `AppConfig` | `PrinterServerPort` |
| `Impressora` | `Nome` |
| `Impressora` | `IP` |
| `Impressora` | `Porta` |

Nao existem IPs, portas ou nomes de impressora alternativos fixos no codigo.

### `ImprimirDireto = True`

1. O sistema localiza a impressora pelo nome configurado em `ImpressoraPadrao`.
2. Valida nome, IP e porta TCP da impressora.
3. Valida IP e porta do servidor de impressao.
4. Envia nome da impressora, IP da impressora, porta da impressora e ZPL ao servidor.
5. Atualiza os documentos e mantem o ultimo lote visivel na grade.
6. Exibe o popup padrao com a quantidade de etiquetas impressas e a transportadora.

### `ImprimirDireto = False`

1. A tela de upload permanece aberta.
2. Uma area isolada e invisivel contem somente os codigos ZPL.
3. O prompt do navegador e aberto automaticamente.
4. Nao e exibida uma view adicional nem popup interno de selecao de impressora.
5. O ultimo lote permanece visivel na grade.

Observacao: `window.print()` imprime o ZPL como texto renderizado. O envio ZPL bruto para impressora termica depende do fluxo de impressao direta.

## Implantacao

- As alteracoes de impressao estao somente no projeto Web e exigem Publish do Web.
- Nao ha script novo para parametros de impressao quando os quatro registros ja existem em `AppConfig` e a impressora ja esta cadastrada.
- Mudancas de schema da conferencia exigem o script `20260718_Conferencia_Volume_Quantidades.sql` em ambientes que ainda nao o receberam.
- Depois do Publish, reciclar o pool da aplicacao e validar as configuracoes da filial.

## Validacoes Recomendadas

1. Importar um PDF com NFs novas e confirmar uma etiqueta por volume.
2. Reimportar o mesmo PDF e confirmar que nenhuma NF ou etiqueta seja recriada.
3. Importar outro lote e confirmar que a grade substitua o lote anterior.
4. Testar `ImprimirDireto = True` e conferir o popup final.
5. Testar `ImprimirDireto = False` e confirmar que a tela de upload permaneça aberta.
6. Conferir a tela de consulta por volume e o destaque de divergencias.

## Datas e fuso horario nas grades Web

- Datas operacionais gravadas no SQL sao tratadas como horario local da operacao.
- As respostas das grades enviam datas de exibicao ja formatadas pelo servidor, evitando que `moment()` ou `new Date()` reinterpretam o valor como UTC.
- A regra foi aplicada em Historico de Recebimento, Pendencias, Conferencia de Volumes, Recebimento ADM, Transito e nas consultas/lancamentos de Expedicao.
- Valores ISO usados para controle de concorrencia permanecem separados das datas apresentadas ao usuario.

## Paginacao e desempenho das grades operacionais

- As grades AJAX ligadas a tabelas de grande crescimento usam paginacao no servidor.
- Pesquisa, contagem, ordenacao e `Skip/Take` sao executados no SQL Server; o Web recebe somente a pagina solicitada.
- A regra foi aplicada em Historico de Recebimento, Pendencias, Transito, Recebimento ADM, Conferencia de Volumes, Retorno Interno, Expedicao, Estoque e Locacoes.
- Historico, Pendencias e Transito usam `PeriodoRecebimento`.
- As consultas de Expedicao usam `PeriodoExpedicao`.
- ADM e Conferencia de Volumes nao descartam pendencias antigas pelo periodo, evitando ocultar processos ainda abertos.
- A grade do ultimo lote da transportadora permanece local porque contem somente o lote mais recente, e nao um historico acumulativo.

Scripts recomendados para producao:

- `20260720_Recebimento_Historico_Performance.sql`;
- `20260720_Grades_Operacionais_Performance.sql`.
