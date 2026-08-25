# Manual por Tela - Separação

## Objetivo

Documentar a tela operacional de `Separação` no coletor, incluindo ownership de tarefa, validação de locação e conclusão de linhas.

## Tela 01. Separação

### Capturas Sugeridas

![Separação - Zona](assets/manual-separacao/01-separacao-zona.png)

![Separação - Linha Atual](assets/manual-separacao/02-separacao-linha-atual.png)

![Separação - Finalizada](assets/manual-separacao/03-separacao-finalizada.png)

### Objetivo

Permitir ao operador assumir uma tarefa por zona e executá-la linha a linha.

### Acesso

1. Entrar no coletor.
2. Selecionar `Separação`.

### Estrutura da Tela

Blocos principais:

- seleção da zona
- inicio da separação
- linha atual da tarefa
- confirmação de locação
- confirmação de quantidade
- botões de ação

### Campos

| Campo | Tipo | Obrigatório | O que representa | O que fazer |
|---|---|---|---|---|
| `Zona` | Lista/autocomplete | Sim | Zona com tarefas pendentes | Selecionar a zona antes de iniciar |
| `Item Nr` | Somente leitura | Não | Item atual da linha | Consultar referência |
| `Descrição` | Somente leitura | Não | Descrição do item atual | Consultar referência |
| `Confirmar locação` | Texto | Sim | Confirmação da locação da linha | Informar a locação lida |
| `Quantidade separada` | Número | Sim | Quantidade efetivamente separada | Informar a quantidade a confirmar |

### Informações de Destaque

| Campo | Significado |
|---|---|
| `Locação` em destaque | Locação esperada para a linha atual |
| `Qtde` em destaque | Quantidade pendente da linha atual |

### Botões e Ações

| Botão | Ação |
|---|---|
| `Iniciar separação` | Assume a próxima tarefa disponível da zona |
| `Confirmar` | Confirma a linha atual |
| `Passby` | Pula a linha atual e busca a próxima |
| `Buscar nova tarefa` | Inicia nova tarefa após conclusão |
| `Trocar zona` | Limpa o contexto e permite escolher outra zona |
| `Voltar` | Libera a tarefa e sai da tela, quando aplicável |
| `Menu` | Retorna ao menu principal |
| `Sair` | Encerra a sessao |

### Como Operar

1. Selecionar a `Zona`.
2. Clicar em `Iniciar separação`.
3. Conferir a `Locação`, `Qtde`, `Item Nr` e `Descrição`.
4. Informar `Confirmar locação`.
5. Informar `Quantidade separada`.
6. Clicar em `Confirmar`.
7. Repetir para as proximas linhas.

### Regras e Validações

| Situação | Comportamento esperado |
|---|---|
| Nenhuma zona selecionada | O sistema bloqueia o inicio |
| Falha ao carregar zonas | Popup de erro |
| Nenhuma tarefa disponível | Popup informativo |
| Locação diferente da tarefa | Popup informando divergência |
| Quantidade nula, zero ou invalida | Popup solicitando quantidade válida |
| Passby da linha | O sistema busca a próxima linha |
| Tarefa finalizada | Exibe mensagem de sucesso |
| Saida com tarefa em andamento | O sistema tenta liberar a tarefa |

### Ownership e Concorrência

- a tarefa assumida fica vinculada ao usuário
- ao sair da tela, o sistema tenta liberar a tarefa
- o ownership evita conflito operacional entre usuários
