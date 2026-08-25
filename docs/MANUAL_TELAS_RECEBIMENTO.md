# Manual por Tela - Recebimento

## Objetivo

Documentar as telas operacionais do módulo `Recebimento` no coletor, com orientação prática de uso, campos, botões e validações.

## Telas Cobertas

- menu de `Recebimento`
- `Descarregar`
- `Armazenar`
- `Conferir`

---

## Tela 01. Menu Recebimento

### Captura Sugerida

![Menu Recebimento](assets/manual-recebimento/01-menu-recebimento.png)

### Objetivo

Servir como menu de entrada para os fluxos de recebimento no coletor.

### Acesso

1. Entrar no coletor.
2. Selecionar `Recebimento` no menu principal.

### Itens da Tela

| Elemento | Tipo | O que faz |
|---|---|---|
| `Descarregar` | Botão | Abre o fluxo de descarregamento |
| `Armazenar` | Botão | Abre o fluxo de armazenagem |
| `Home` | Botão inferior | Retorna ao menu principal |
| `Sair` | Botão inferior | Encerra a sessao |

---

## Tela 02. Descarregar

### Captura Sugerida

![Descarregar](assets/manual-recebimento/02-descarregar.png)

### Objetivo

Registrar a leitura de volumes no recebimento por área e acompanhar contadores operacionais.

### Acesso

1. Abrir `Recebimento`.
2. Selecionar `Descarregar`.

### Campos

| Campo | Tipo | Obrigatório | O que representa | O que fazer |
|---|---|---|---|---|
| `Área` | Lista | Sim | Área operacional do descarregamento | Selecionar a área antes da leitura dos volumes |
| `Volume NR` | Texto | Sim | Código do volume a ser processado | Ler o barcode ou habilitar o teclado pelo ícone para digitar |

### Indicadores

| Indicador | Significado |
|---|---|
| `Pendentes` | Volumes ainda não conferidos; toque no card para listar somente os números pendentes |
| `Conferidos` | Volumes já processados corretamente |
| `Total` | Total da área selecionada |
| `Incorretos` | Volumes invalidados ou processados com divergência |

### Botões e Ações

| Botão | Ação |
|---|---|
| Ícone `Teclado` | Habilita ou desabilita o teclado virtual para digitação manual |
| Card `Pendentes` | Consulta novamente a área e abre a lista dos números de volumes pendentes |
| `Atualizar` na lista | Recarrega os pendentes da área selecionada |
| `Voltar` | Retorna ao menu de recebimento |
| `Menu` | Retorna ao menu principal |
| `Sair` | Encerra a sessao |
| `Fechar` no popup | Fecha a mensagem apresentada |

### Como Operar

1. Selecionar a `Área`.
2. Aguardar habilitacao do campo `Volume NR`.
3. Ler o volume; o leitor deve enviar `Enter` ou `Tab` ao final do código.
4. Para digitação manual, tocar no ícone de teclado e confirmar pelo teclado virtual.
5. Tocar em `Pendentes` quando precisar identificar os volumes ainda não lidos.
6. Repetir o processo conforme necessidade e acompanhar os contadores.

### Validações e Comportamentos

| Situação | Comportamento esperado |
|---|---|
| Área não selecionada | Campo de volume permanece desabilitado |
| Foco no campo de volume | O teclado virtual permanece oculto, mas o leitor físico continua habilitado |
| Leitor sem terminador | O código é preenchido, mas não é processado até o envio de `Enter` ou `Tab` |
| Card `Pendentes` acionado | Abre uma lista rolável contendo somente `VolumeNr` com status pendente |
| Falha ao carregar áreas | Popup de erro |
| Falha no processamento do volume | Popup de erro |
| Leitura finaliza um contexto | O sistema pode exibir mensagem informativa |

---

## Tela 03. Armazenar

### Captura Sugerida

![Armazenar](assets/manual-recebimento/03-armazenar.png)

### Objetivo

Validar material, conferir a locação e registrar a armazenagem efetiva do item.

### Acesso

1. Abrir `Recebimento`.
2. Selecionar `Armazenar`.

### Campos

| Campo | Tipo | Obrigatório | O que representa | O que fazer |
|---|---|---|---|---|
| `Item` | Texto | Sim | Código do item a armazenar | Informar o item e sair do campo para validar |
| `Descrição` | Somente leitura | Não | Descrição retornada pelo sistema | Conferir se corresponde ao item |
| `Locação` | Somente leitura | Não | Locação esperada para armazenagem | Usar como referência da conferência |
| `Confirmar Locação` | Texto | Sim | Confirmação da locação física | Informar exatamente a locação esperada |
| `Quantidade` | Número | Sim | Quantidade a armazenar | Informar quantidade válida e confirmar |

### Botões e Ações

| Botão | Ação |
|---|---|
| `Confirmar` | Executa a armazenagem do item |
| `Reiniciar` | Limpa o formulário |
| `Voltar` | Retorna ao menu de recebimento |
| `Menu` | Retorna ao menu principal |
| `Sair` | Encerra a sessao |
| `Fechar` no popup | Fecha a mensagem de validação |

### Como Operar

1. Informar o `Item`.
2. Conferir `Descrição` e `Locação`.
3. Preencher `Confirmar Locação`.
4. Informar a `Quantidade`.
5. Clicar em `Confirmar`.

### Regras e Validações

| Situação | Comportamento esperado |
|---|---|
| Item sem locação cadastrada | Popup informando a ausencia de locação |
| Falha ao buscar item | Popup de erro |
| Locação divergente | Popup informando locação incorreta e gravação de histórico |
| Quantidade não numerica, nula ou menor que 1 | Bloqueio da confirmação |
| Quantidade maior que a disponível | Popup informando excesso e gravação de histórico |
| Armazenagem concluída | Item e histórico são gravados e o formulário é reiniciado |

### Observações Operacionais

- a tela grava histórico tanto para sucesso quanto para erro
- o usuário logado é usado como responsável pela operação

---

## Tela 04. Conferir

### Captura Sugerida

![Conferir Recebimento](assets/manual-recebimento/04-conferir-recebimento.png)

### Objetivo

Conferir quantitativamente os itens de um volume, registrar o operador e tratar divergências entre a quantidade faturada e a quantidade recebida.

### Campos e Informações

| Elemento | Finalidade |
|---|---|
| `Volume` | Localiza o volume na filial do usuário |
| `Qtde NF` | Quantidade faturada do item |
| `Qtde Conferida` | Quantidade física informada pelo operador |
| `Diferença` | Resultado entre quantidade conferida e faturada |
| `Qtde Armazenada` | Quantidade já armazenada |
| `Conferido` | Confirma a intenção de finalizar o item |
| Alerta `ITEM CRÍTICO` | Destaca material crítico e sua observação |

### Como Operar

1. Informar ou ler o volume e tocar em `Localizar`.
2. Conferir os dados do primeiro item pendente.
3. Informar a `Qtde Conferida`.
4. Marcar `Conferido`.
5. Tocar em `Confirmar`.
6. Em caso de diferença, confirmar explicitamente a conferência a maior ou a menor.
7. Repetir até a finalização do volume.

### Regras

- quantidade negativa não é aceita
- divergência exige confirmação adicional
- item alterado por outro operador é recarregado e não é sobrescrito silenciosamente
- item já conferido por outro usuário não pode ser alterado
- ao concluir todos os itens, o sistema informa a finalização do volume

---

## Tela Web. Conferência de Volumes

### Objetivo

Pesquisar um volume e confirmar a quantidade recebida de cada item.

### Comportamento

- O botão `Confirmar` substitui o flag visual de conferência.
- A tela não exibe quantidade armazenada.
- Depois que todos os itens forem confirmados, a grade e o volume pesquisado são limpos.
- O foco retorna ao campo `Volume Nr` para uma nova consulta.
- Quantidade, usuário e data/hora da conferência ficam registrados.

## Tela Web. Consulta por Volume Nr

### Colunas

`Nota Fiscal`, `Item Nr`, `Descricao`, `Faturado`, `Conferido`, `Conferente`, `Diferenca`, `Armazenado`, `Estoquista` e `Status`.

- Conferente e data/hora aparecem juntos.
- Estoquista e data/hora aparecem juntos.
- Diferença diferente de zero aparece em vermelho.
- Pedido, observação e auditoria genérica não aparecem na grade.

## Regras de Nota Fiscal e Devolução

- Fora de devolução, a NF é única por filial, movimento e número.
- Devoluções podem repetir NF e itens.
- A exclusão de devolução exige confirmação padrão e remove processo, itens e complementos.

## Tela Web. Histórico de Recebimento

- A coluna `Data / Hora` usa o valor local persistido em `HistoricoRecebimento.DataHora`.
- A resposta do servidor já envia `dd/MM/yyyy HH:mm:ss`; o navegador não aplica conversao de fuso.
- A mesma regra de formatação no servidor é usada nas grades de Pendências, Conferência de Volumes, Recebimento ADM e Trânsito.
- A grade solicita somente a página atual ao servidor; pesquisa, total filtrado e ordenação são processados no banco.
- O intervalo consultado é definido por `AppConfig.PeriodoRecebimento`.

## Observações do Módulo

- `Recebimento` possui fluxos operacionais documentados para `Descarregar`, `Conferir` e `Armazenar`
- as telas Web de conferência e consulta por volume seguem as regras quantitativas descritas neste manual
- as melhorias entregues em 25/08/2026 estão consolidadas em [Atualizações Operacionais](ATUALIZACOES_20260825.md)
