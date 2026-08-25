# Manual por Tela - Estoque

## Objetivo

Documentar as telas operacionais do módulo `Estoque` no coletor.

## Telas Cobertas

- menu de `Estoque`
- `Consultar Locação`
- `Consultar Item`
- `Coletar`
- `Transferir`
- `Contar` em estado atual observado

---

## Tela 01. Menu Estoque

### Captura Sugerida

![Menu Estoque](assets/manual-estoque/01-menu-estoque.png)

### Objetivo

Servir como menu de entrada para os fluxos de estoque.

### Itens da Tela

| Elemento | Tipo | O que faz |
|---|---|---|
| `Consultar Locação` | Botão | Abre a consulta da locação |
| `Consultar Item` | Botão | Abre a consulta do item |
| `Coletar` | Botão | Abre o fluxo de coleta |
| `Transferir` | Botão | Abre o fluxo de transferência |
| `Contagem` | Botão | Função prevista no menu |
| `Home` | Botão inferior | Retorna ao menu principal |
| `Sair` | Botão inferior | Encerra a sessao |

---

## Tela 02. Consultar Locação

### Captura Sugerida

![Consultar Locação](assets/manual-estoque/02-consultar-locacao.png)

### Objetivo

Consultar dados da locação, seu contexto logístico e os itens alocados nela.

### Acesso

1. Abrir `Estoque`.
2. Selecionar `Consultar Locação`.

### Campos

| Campo | Tipo | Obrigatório | O que representa | O que fazer |
|---|---|---|---|---|
| `Locação` | Texto | Sim | Locação a consultar | Informar a locação e sair do campo |
| `Descrição` | Somente leitura | Não | Descrição da locação | Conferir retorno |
| `Tipo` | Somente leitura | Não | Tipo da locação | Consultar informação |
| `Curva` | Somente leitura | Não | Curva associada | Consultar informação |
| `Estratégia` | Somente leitura | Não | Estrategia da locação | Consultar informação |
| `Área` | Somente leitura | Não | Área da locação | Consultar informação |
| `Equipamento` | Somente leitura | Não | Equipamento relacionado | Consultar informação |
| `Observações` | Somente leitura | Não | Observações da locação | Consultar informação |

### Indicadores e Sinais

| Indicador | Significado |
|---|---|
| `Locação bloqueada` | A locação está marcada como bloqueada |

### Itens da Locação

Ao clicar em `Exibir Itens`, a tela mostra navegação item a item com:

| Campo | Significado |
|---|---|
| `Item Nr` | Código do item |
| `Descrição` | Descrição do item |
| `Saldo` | Saldo atual |
| `Indisponível` | Quantidade indisponivel |
| `Pedido Pendente` | Quantidade pendente de pedido |
| `Curva` | Curva do item |
| `UN` | Unidade |

### Botões e Ações

| Botão | Ação |
|---|---|
| `Exibir Itens (N)` | Abre a navegação dos itens da locação |
| `Anterior` | Vai para o item anterior |
| `Próximo` | Vai para o item seguinte |
| `Exibir dados da locação` | Retorna da visao do item para os dados da locação |
| `Reiniciar` | Limpa a consulta |
| `Voltar` | Retorna ao menu de estoque |
| `Menu` | Retorna ao menu principal |
| `Sair` | Encerra a sessao |

### Validações

| Situação | Comportamento esperado |
|---|---|
| Locação não informada | Consulta não é executada |
| Falha ao consultar locação | Popup de erro |

---

## Tela 03. Consultar Item

### Captura Sugerida

![Consultar Item](assets/manual-estoque/03-consultar-item.png)

### Objetivo

Consultar rapidamente a posição e os saldos de um item.

### Acesso

1. Abrir `Estoque`.
2. Selecionar `Consultar Item`.

### Campos

| Campo | Tipo | Obrigatório | O que representa | O que fazer |
|---|---|---|---|---|
| `Item Nr` | Texto | Sim | Código do item | Informar o item e sair do campo |
| `Descrição` | Somente leitura | Não | Descrição do item | Conferir retorno |
| `Locação` | Somente leitura | Não | Locação principal | Consultar informação |
| `Saldo` | Somente leitura | Não | Saldo atual | Consultar informação |
| `Indisponível` | Somente leitura | Não | Quantidade indisponivel | Consultar informação |
| `Pedido Pendente` | Somente leitura | Não | Quantidade pendente | Consultar informação |
| `Curva` | Somente leitura | Não | Curva do item | Consultar informação |
| `UN` | Somente leitura | Não | Unidade de medida | Consultar informação |

### Indicadores

| Indicador | Significado |
|---|---|
| `Item crítico` | Item marcado como crítico |

### Botões e Ações

| Botão | Ação |
|---|---|
| `Reiniciar` | Limpa a consulta |
| `Voltar` | Retorna ao menu de estoque |
| `Menu` | Retorna ao menu principal |
| `Sair` | Encerra a sessao |

### Validações

| Situação | Comportamento esperado |
|---|---|
| Item não encontrado | Popup de erro |
| Falha de consulta | Popup de erro |

---

## Tela 04. Coletar

### Captura Sugerida

![Coletar](assets/manual-estoque/04-coletar.png)

### Objetivo

Registrar a coleta de um item da locação de origem para uma Locação de Espera validada.

### Acesso

1. Abrir `Estoque`.
2. Selecionar `Coletar`.

### Campos

| Campo | Tipo | Obrigatório | O que representa | O que fazer |
|---|---|---|---|---|
| `Locação de Espera` | Texto | Sim | Local intermediário que receberá os itens coletados | Ler e validar antes de consultar itens |
| `Item` | Texto | Sim | Item a ser coletado | Informar o item e consultar |
| `Descrição` | Somente leitura | Não | Descrição do item | Conferir retorno |
| `Locação` | Somente leitura | Não | Origem da coleta | Conferir retorno |
| `Saldo` | Somente leitura | Não | Quantidade atual | Conferir retorno |
| `Quantidade` | Número | Sim | Quantidade retirada da origem | Informar inteiro positivo dentro do saldo disponível |

### Botões e Ações

| Botão | Ação |
|---|---|
| `Confirmar` | Registra a coleta |
| `Trocar locação` | Encerra o contexto atual e permite informar outra Locação de Espera |
| `Voltar` | Retorna ao menu de estoque |
| `Menu` | Retorna ao menu principal |
| `Sair` | Encerra a sessao |

### Validações

| Situação | Comportamento esperado |
|---|---|
| Locação de Espera inválida | Popup de erro e bloqueio da consulta de itens |
| Item não informado | Popup solicitando o item |
| Item não cadastrado | Popup informando item não cadastrado |
| Quantidade maior que o saldo | Operação bloqueada |
| Falha ao consultar ou gravar coleta | Popup de erro |

---

## Tela 05. Transferir

### Captura Sugerida

![Transferir](assets/manual-estoque/05-transferir.png)

### Objetivo

Consultar as movimentações pendentes de uma Locação de Espera e transferi-las para as localizações finais previstas.

### Acesso

1. Abrir `Estoque`.
2. Selecionar `Transferir`.

### Campos

| Campo | Tipo | Obrigatório | O que representa | O que fazer |
|---|---|---|---|---|
| `Locação de Espera` | Texto | Sim | Local intermediário que contém as coletas pendentes | Ler para carregar os itens |
| `Locação Final` | Somente leitura | Não | Destino esperado da movimentação selecionada | Conferir retorno |
| `Confirmar Locação Final` | Texto | Sim | Confirmação física do destino | Ler a localização final |
| `Quantidade` | Número | Sim | Quantidade a transferir | Informar valor válido e confirmar |

### Botões e Ações

| Botão | Ação |
|---|---|
| Seta do item | Seleciona a movimentação pendente |
| `Confirmar` | Finaliza a movimentação |
| `Trocar locação` | Limpa a lista e permite consultar outra Locação de Espera |
| `Voltar` | Retorna ao menu de estoque |
| `Menu` | Retorna ao menu principal |
| `Sair` | Encerra a sessao |

### Como Operar

1. Informar a `Locação de Espera`.
2. Selecionar uma das movimentações pendentes listadas.
3. Conferir a `Locação Final`.
4. Ler `Confirmar Locação Final`.
5. Informar a `Quantidade`.
6. Clicar em `Confirmar` e aguardar a atualização da lista.

### Validações

| Situação | Comportamento esperado |
|---|---|
| Locação de Espera inválida | Popup de erro |
| Nenhuma movimentação pendente | Mensagem informativa de lista concluída |
| Movimentação sem destino final | Seleção bloqueada |
| Locação Final divergente | Popup informando localização incorreta |
| Quantidade nula ou invalida | Popup solicitando quantidade válida |
| Quantidade maior que a disponível | Operação bloqueada |
| Movimentação íntegra com quantidade parcial | Operação bloqueada; deve transferir a quantidade coletada |
| Erro ao finalizar movimentação | Popup de erro |

---

## Tela 06. Contar

### Captura Sugerida

![Contar](assets/manual-estoque/06-contar.png)

### Objetivo

A tela `Estoque / Contar` existe no projeto, mas no estado atualmente observado aparece mais como estrutura de layout e diálogos do que como fluxo operacional completo documentado.

### Recomendação

Antes de transformar essa tela em material oficial de treinamento, validar:

- se o fluxo está homologado
- quais campos e regras ainda serão expostos
