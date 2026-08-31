# Procedimentos de Trabalho e Processos

## Objetivo

Documentar os principais processos operacionais suportados pelo WMS Quasar no contexto Automec, considerando o comportamento atual observado na aplicacao web, na API e no coletor.

Este documento complementa o manual de utilizacao com foco maior em:

- regras transversais
- macroprocessos
- fluxos operacionais
- pontos de controle
- status e ownership de processo

## Base desta Versao

Esta versão foi revisada em `2026-08-25` com base em:

- `web/Simplify.Quasar/Areas`
- `api/Routes`
- `coletor/src/views`
- scripts SQL de `web/docs/sql`
- ajustes recentes na conferencia de romaneios de expedicao

## Aplicacoes Envolvidas

### Web MVC

Uso principal para:

- cadastros
- menus
- operacao administrativa
- parte dos fluxos operacionais

### API

Uso principal para:

- operacao do coletor
- regras transacionais novas
- autenticacao e integracao

### Coletor

Uso principal para:

- operacao em dispositivo movel
- leitura e confirmacao de tarefas

## Regras Transversais

### 1. Filial ativa

A filial ativa no login e referencia para consultas e gravacoes operacionais.

### 2. Usuario logado

Ownership de tarefas e conferencias deve considerar o usuario autenticado.

### 3. Data e hora oficial

A data/hora do servidor deve ser usada para registro operacional.

### 4. Controle de acesso

O acesso depende de:

- sessao valida
- perfil
- area liberada
- menu liberado

### 5. Concorrencia operacional

Fluxos com ownership nao devem permitir alteracao concorrente por usuarios distintos.

## Modulos Atuais

- `RecebimentoApp`
- `SeparacaoApp`
- `ExpedicaoApp`
- `EstoqueApp`
- `ControleAcessoApp`
- `ConfiguracaoApp`
- `DevolucaoApp`
- `AnomaliaApp`
- `ComprasApp`
- `GarantiaApp`
- `AdminApp`

## Macroprocessos

### 1. Acesso e controle

Processos:

- login
- troca de senha
- cadastro de usuario
- cadastro de perfil
- vinculo de areas
- liberacao de menus

### 2. Recebimento

Processos:

- descarregamento
- conferencia de volumes
- armazenagem

### 3. Estoque

Processos:

- consulta de item
- consulta de locacao
- movimentacao
- contagem
- transferencia

### 4. Separacao

Processos:

- alocacao por zona
- geracao de tarefas
- execucao de tarefa
- consulta de tarefa

### 5. Expedicao

Processos:

- conferencia de volumes
- conferencia de romaneios
- despacho
- lancamentos e documentos auxiliares

### 6. Devolução

Processos:

- cadastro de devolução e itens
- consulta e atualização do processo
- tratamento de ocorrências por item
- impressão do processo

### 7. Anomalias GM

Processos:

- pesquisa de item, NF e volume
- validação de prazo e saldo
- cadastro do tipo por item
- aceite ou rejeição dos itens
- exportação dos formulários oficiais

## POP 01. Login e Liberacao de Acesso

### Objetivo

Garantir acesso seguro por usuario, filial e perfil.

### Passos

1. Informar usuario, senha e filial.
2. Validar se o usuario esta ativo e autorizado.
3. Trocar senha, se necessario.
4. Carregar menu e areas permitidas.

## POP 02. Cadastro de Usuarios, Perfis e Areas

### Objetivo

Manter segregacao de funcao e acesso minimo necessario.

### Passos

1. Cadastrar ou alterar usuario.
2. Definir perfil e filial.
3. Configurar areas liberadas.
4. Revisar menus e permissoes associadas.

## POP 03. Recebimento

### Objetivo

Registrar e processar a entrada fisica e sistemica de materiais.

### Passos

1. Acessar o fluxo de recebimento apropriado.
2. Localizar documento, volume ou item.
3. Confirmar descarregamento ou conferencia.
4. Encaminhar para armazenagem.

### Controle de volumes pendentes na descarga

1. Selecionar a área operacional.
2. Tocar no card `Pendentes`.
3. Conferir a lista atualizada, que apresenta somente `VolumeNr` pendentes.
4. Fechar a lista para restaurar o foco do leitor.
5. Ler o barcode com terminador `Enter` ou `Tab`.

O teclado virtual fica desativado por padrão. Para entrada manual, o operador deve acionar o ícone de teclado.

### Conferência quantitativa

1. Localizar o volume.
2. Informar a quantidade conferida de cada item.
3. Marcar o item como conferido.
4. Confirmar divergências quando existirem.
5. Repetir até concluir todos os itens do volume.

## POP 04. Armazenagem

### Objetivo

Registrar a alocacao fisica do item em estoque.

### Passos

1. Validar o material.
2. Validar a quantidade.
3. Atualizar o item da nota fiscal.
4. Gravar historico da operacao.

## POP 05. Consulta e Movimentacao de Estoque

### Objetivo

Permitir consulta rapida e movimentacao controlada de itens e locacoes.

### Passos

1. Consultar item ou locacao.
2. Validar saldo, local e situacao.
3. Registrar movimentacao quando aplicavel.

### Coleta e transferência por Locação de Espera

Na coleta:

1. Validar a Locação de Espera.
2. Consultar o item na localização de origem.
3. Informar a quantidade dentro do saldo disponível.
4. Confirmar a movimentação intermediária.

Na transferência:

1. Consultar a Locação de Espera.
2. Selecionar uma movimentação pendente.
3. Confirmar fisicamente a Locação Final.
4. Informar e confirmar a quantidade.
5. Aguardar a atualização da lista antes de continuar.

## POP 06. Alocacao de Pedidos por Zona

### Objetivo

Gerar tarefas de separacao agrupadas por zona para romaneios elegiveis.

### Pre-requisitos

- zona cadastrada
- locacao vinculada a zona
- area com `Alocar = true`
- romaneio elegivel

### Regras atuais

- a zona vem da locacao do item
- a quebra usa prioritariamente `Zona.QtdeLinha`
- a prioridade operacional usada e a de `AreaRomaneio`
- ao gerar tarefa:
  - `Romaneio.StatusId = 2`
  - `RomaneioItem.StatusId = 2`
  - `RomaneioItem.TarefaNr` e preenchido

## POP 07. Consulta de Tarefas

### Objetivo

Permitir acompanhamento das tarefas geradas no fluxo de separacao.

### Campos de destaque

- tarefa nr
- item nr
- locacao
- descricao
- quantidade
- status
- criado em

## POP 08. Execucao de Tarefa de Separacao no Coletor

### Objetivo

Executar a tarefa linha a linha, com ownership por operador.

### Fluxo geral

1. Assumir tarefa.
2. Consultar linha atual.
3. Confirmar linha ou realizar pass-by.
4. Acompanhar status ate a conclusao.

### Regras

- a tarefa assumida nao deve ser alterada por outro usuario
- o abandono deve liberar o contexto operacional

## POP 09. Conferencia de Volumes em Expedicao

### Objetivo

Conferir volumes por transportadora ou documento antes do despacho.

### Fluxo geral

1. Selecionar transportadora ou documento.
2. Conferir pendentes, lidos e total.
3. Registrar historico dos volumes.

## POP 10. Conferencia de Romaneios de Expedicao

### Objetivo

Confirmar os itens do romaneio de expedicao ate o encerramento total do processo.

### Canais

- web: `Expedicao > Conferir Romaneios`
- coletor: `Expedicao > Conferir Separacao`

### Regras atuais implementadas

- o inicio da conferencia e automatico ao selecionar o romaneio
- o romaneio assumido fica bloqueado para outros usuarios
- a lista de romaneios disponiveis nao deve exibir romaneios assumidos por outro usuario
- tentativa de acesso concorrente deve retornar bloqueio

### Assumir conferencia

Ao iniciar:

- `Romaneio.StatusId = 9`
- `Romaneio.ConferenteId = usuario logado`
- `Romaneio.DataConferente = data/hora atual`
- `RomaneioItem.StatusId = 9`
- `RomaneioItem.ConferenteId = usuario logado`
- `RomaneioItem.DataConferente = data/hora atual`

### Confirmar quantidade

#### Quantidade maior que a pendente

- nao permitido

#### Quantidade parcial

- soma em `QtdeConferida`
- item permanece em conferencia

#### Quantidade total

- item vai para `StatusId = 4`

#### Quantidade zero

- requer confirmacao
- item vai para `StatusId = 6`
- significado operacional: `Em Busca`

### Soltar romaneio

Ao abandonar:

- `Romaneio.StatusId = 8`
- `Romaneio.ConferenteId = NULL`
- `Romaneio.DataConferente = NULL`
- somente itens nao confirmados retornam para `StatusId = 8`
- somente itens nao confirmados limpam `ConferenteId` e `DataConferente`

### Finalizacao do romaneio

Quando todos os itens estiverem concluidos:

- `Romaneio.StatusId = 4`

Itens concluidos para esse criterio:

- `StatusId = 4`
- `StatusId = 6`

## POP 11. Romaneios Nao Gerados

### Objetivo

Controlar romaneios pendentes de geracao e o fechamento do tratamento.

### Fluxo geral

1. Informar faixa de romaneios.
2. Pesquisar.
3. Gerar a planilha.
4. Finalizar o lote, quando permitido.

## POP 12. Expedicao de Notas Fiscais

### Objetivo

Preparar e processar notas de saida para despacho.

### Fluxo geral

1. Importar ou lancar dados.
2. Definir transportadora e contexto.
3. Gerar etiquetas e documentos auxiliares.
4. Finalizar operacao.

## POP 13. Importar Arquivo de Transportadora e Imprimir Etiquetas

### Objetivo

Importar o PDF da transportadora, gerar uma etiqueta por volume novo e executar a impressao conforme a configuracao da filial.

### Fluxo geral

1. Selecionar transportadora, movimento e PDF.
2. Confirmar o upload.
3. Ignorar NFs que ja existam em `DocExpedicao`.
4. Deduplicar NF/contato/volume extraidos mais de uma vez.
5. Substituir o lote anterior da filial pelo lote atual.
6. Imprimir automaticamente ou abrir o prompt do navegador, conforme `ImprimirDireto`.
7. Manter o ultimo lote visivel na grade.

### Regras

- NF existente nao e recriada e nao gera etiqueta.
- sem etiquetas, exibir apenas o popup padrao `Nenhuma etiqueta foi gerada.`
- na impressao automatica concluida, informar quantas etiquetas foram impressas e a transportadora.
- o fluxo automatico depende exclusivamente de `AppConfig` e do cadastro `Impressora`.
- o lote nao e acumulativo; o proximo upload valido substitui o atual.

## POP 14. Cadastro e Tratamento de Anomalias GM

### Objetivo

Registrar reclamações de itens recebidos da GM, respeitando o prazo e o saldo reclamável, e gerar o formulário oficial correspondente.

### Fluxo geral

1. Confirmar a filial ativa.
2. Pesquisar o número do item.
3. Selecionar a NF e o volume elegíveis.
4. Escolher o tipo A, B, C ou G para o item.
5. Informar uma quantidade inteira dentro do saldo disponível.
6. Preencher os dados específicos e adicionar o item.
7. Finalizar o cadastro para gerar o número de controle.
8. Acompanhar e tratar os itens na consulta de anomalias.
9. Exportar o formulário GM adequado.

### Regras

- o processo, a consulta, o saldo e os dados empresariais são isolados por filial;
- o prazo é calculado por `DataEmissao + PrazoDias` do tipo;
- NFs vencidas continuam visíveis, mas não podem ser selecionadas;
- o saldo é validado novamente dentro da transação de gravação;
- reenvio não representa nova reclamação e não consome saldo novamente;
- tipos A, B e C utilizam o formulário de Anomalias;
- tipo G utiliza o formulário de Danificados.

Consulte [Manual por Tela - Anomalias](MANUAL_TELAS_ANOMALIAS.md).

## POP 15. Cadastro e Tratamento de Devolução

### Objetivo

Registrar e acompanhar a devolução de materiais, incluindo o tratamento das ocorrências por item.

### Fluxo geral

1. Selecionar movimento, retirada, status e motivo.
2. Localizar a NF de venda.
3. Informar dados complementares e transportadora.
4. Adicionar os itens, quantidades e valores.
5. Salvar e imprimir o processo quando necessário.
6. Consultar o processo para atualizar o andamento.
7. Tratar as ocorrências e quantidades de cada item.

Consulte [Manual por Tela - Devolução](MANUAL_TELAS_DEVOLUCAO.md).

## Cadastros de Apoio Relevantes

- `AreaPedido`
- `AreaRomaneio`
- `Zona`
- `Locacao`
- `AppMenu`
- `PerfilAreaAcesso`

## Scripts SQL Relevantes

- `sql/20260607_DevolucaoComplemento.sql`
- `sql/20260702_AlocacaoPedidosZona.sql`
- `sql/20260718_Recebimento_NotaFiscal_Unicidade.sql`
- `sql/20260718_Conferencia_Volume_Quantidades.sql`

## Referencias

- [Manual de Utilizacao](MANUAL_UTILIZACAO.md)
- [Arquitetura e Integracoes](ARQUITETURA_E_INTEGRACOES.md)
- [Guia de Desenvolvimento e Execucao](GUIA_DESENVOLVIMENTO.md)
- [Atualizacoes Operacionais de 20/07/2026](ATUALIZACOES_20260720.md)
- [Atualizações Operacionais de 25/08/2026](ATUALIZACOES_20260825.md)
- [Atualizações Operacionais de 31/08/2026](ATUALIZACOES_20260831.md)
