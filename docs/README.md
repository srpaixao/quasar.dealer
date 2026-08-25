# Documentação do Projeto

## Objetivo

Centralizar a documentação funcional, operacional e técnica do WMS Quasar no contexto Automec.

## Documentos Disponíveis

### Funcional e Operacional

- [Manual de Utilização](MANUAL_UTILIZACAO.md)
- [Catálogo de Telas](MANUAL_TELAS_INDEX.md)
- [Manuais por Tela](MANUAL_TELAS_GERAL.md)
- [Manual por Tela - Recebimento](MANUAL_TELAS_RECEBIMENTO.md)
- [Manual por Tela - Estoque](MANUAL_TELAS_ESTOQUE.md)
- [Manual por Tela - Separação](MANUAL_TELAS_SEPARACAO.md)
- [Manual por Tela - Expedição](MANUAL_TELAS_EXPEDICAO.md)
- [Procedimentos de Trabalho e Processos](PROCEDIMENTOS_TRABALHO_E_PROCESSOS.md)
- [Atualizações Operacionais de 20/07/2026](ATUALIZACOES_20260720.md)
- [Atualizações Operacionais de 25/08/2026](ATUALIZACOES_20260825.md)

### Técnica

- [Arquitetura e Integrações](ARQUITETURA_E_INTEGRACOES.md)
- [Guia de Desenvolvimento e Execução](GUIA_DESENVOLVIMENTO.md)
- [Template de Manual por Tela](TEMPLATE_MANUAL_TELA.md)

### Banco e Evolução

- `sql/20260607_DevolucaoComplemento.sql`
- `sql/20260702_AlocacaoPedidosZona.sql`
- `sql/20260718_Recebimento_NotaFiscal_Unicidade.sql`
- `sql/20260718_Conferencia_Volume_Quantidades.sql`

Não existe script adicional para os parâmetros de impressão quando `ImprimirDireto`, `ImpressoraPadrao`, `PrinterServerIP` e `PrinterServerPort` já estiverem cadastrados.

## Quando Atualizar Esta Pasta

Atualize esta documentação sempre que houver:

- novo módulo ou nova tela relevante
- mudanca de menu
- mudanca de fluxo operacional
- alteracao de regra de negocio
- novo endpoint consumido pelo coletor
- mudanca de build, execução ou dependencia de ambiente

## Prioridade de Leitura

Para novos integrantes:

1. `../../README.md`
2. `../README.md`
3. `MANUAL_UTILIZACAO.md`
4. `ARQUITETURA_E_INTEGRACOES.md`
5. `GUIA_DESENVOLVIMENTO.md`
