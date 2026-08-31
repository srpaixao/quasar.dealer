# Manual de Utilização

## Objetivo

Este manual descreve o uso operacional das funções principais atualmente suportadas pelo WMS Quasar no contexto Automec.

O foco é orientar usuários operacionais, supervisores e usuários-chave sobre:

- acesso ao sistema
- navegação por módulo
- passos básicos de uso
- cuidados operacionais
- comportamento atual da conferência de romaneios em expedição

Complementos visuais recomendados:

- [Catálogo de Telas](MANUAL_TELAS_INDEX.md)
- [Manual por Tela - Recebimento](MANUAL_TELAS_RECEBIMENTO.md)
- [Manual por Tela - Estoque](MANUAL_TELAS_ESTOQUE.md)
- [Manual por Tela - Separação](MANUAL_TELAS_SEPARACAO.md)
- [Manual por Tela - Expedição](MANUAL_TELAS_EXPEDICAO.md)
- [Manual por Tela - Devolução](MANUAL_TELAS_DEVOLUCAO.md)
- [Manual por Tela - Anomalias](MANUAL_TELAS_ANOMALIAS.md)
- [Manual por Tela - Cadastros e Administração](MANUAL_TELAS_CADASTROS.md)

## Canais de Uso

O sistema hoje opera em dois canais principais:

### 1. Web

Uso indicado para:

- cadastros
- consultas administrativas
- processos de backoffice
- operações mais detalhadas

### 2. Coletor

Uso indicado para:

- leitura operacional rápida
- processos em dispositivo móvel
- tarefas sequenciais de operação

## Acesso ao Sistema

### Login

1. Informe usuário, senha e filial.
2. Confirme o acesso.
3. Caso a senha esteja expirada, realize a troca antes de prosseguir.

### Regras Gerais

- a filial selecionada influencia as consultas e atualizações
- menus e telas dependem do perfil do usuário
- algumas telas bloqueiam registros para um único operador por vez

## Módulos do Sistema

### Recebimento

Principais objetivos:

- descarregar
- conferir volumes
- armazenar itens recebidos

### Estoque

Principais objetivos:

- consultar item
- consultar locação
- contar
- transferir
- coletar

### Separação

Principais objetivos:

- trabalhar tarefas de separação
- acompanhar zonas e linhas
- concluir ou abandonar atividades em andamento

### Expedição

Principais objetivos:

- despachar
- conferir volumes
- conferir romaneios de separação

### Anomalias

Principais objetivos:

- cadastrar reclamações GM por item, NF e volume
- controlar prazo e saldo disponível por tipo de anomalia
- acompanhar itens em processo, aceitos e rejeitados
- exportar os formulários oficiais GM

### Devolução

Principais objetivos:

- cadastrar processos e itens de devolução
- consultar e atualizar o andamento
- tratar ocorrências por item
- imprimir o documento do processo

### Cadastros e Administração

Principais objetivos:

- manter filiais, parâmetros, áreas, equipamentos, materiais e impressoras
- administrar usuários, perfis, funções e acessos
- manter cadastros auxiliares dos módulos operacionais

## Manual Rápido por Processo

### Anomalias

Fluxo mais comum:

1. abra `Anomalias > Cadastrar Anomalia`;
2. pesquise pelo número do item;
3. escolha a NF/volume dentro do prazo e com saldo;
4. selecione o tipo de anomalia e informe uma quantidade inteira;
5. preencha os dados específicos do item e clique em `Adicionar`;
6. repita para os demais itens e finalize o cadastro;
7. acompanhe o processo em `Consultar Anomalias`;
8. na tela de detalhes, trate os itens e exporte o formulário correspondente.

Consulte as regras completas em [Manual por Tela - Anomalias](MANUAL_TELAS_ANOMALIAS.md).

### Recebimento

Fluxos mais comuns:

- descarregar
- conferir
- armazenar

Orientação geral:

1. entre no módulo correto
2. localize o documento, volume ou item
3. registre a leitura ou a confirmação
4. avance para a próxima etapa operacional

No fluxo `Descarregar`:

- toque no card `Pendentes` para consultar somente os números dos volumes ainda não lidos
- use o leitor configurado para enviar `Enter` ou `Tab` ao final do barcode
- o teclado virtual permanece oculto por padrão; use o ícone de teclado apenas para digitação manual

No fluxo `Conferir`:

- localize o volume
- informe a quantidade física de cada item
- marque `Conferido` e confirme
- confirme explicitamente divergências a maior ou a menor

### Estoque

Fluxos mais comuns:

- consulta de item
- consulta de locação
- contagem
- transferência

Orientação geral:

1. informe o item ou a locação
2. valide o retorno do sistema
3. registre a movimentação quando aplicável

Para `Coletar` e `Transferir`, a operação começa pela Locação de Espera. Na coleta, ela recebe a movimentação intermediária; na transferência, ela determina a lista de itens que devem seguir para a localização final.

### Separação

Fluxos mais comuns:

- assumir tarefa
- consultar linha atual
- confirmar linha
- abandonar tarefa

Orientação geral:

1. assuma a tarefa disponível
2. siga a linha atual apresentada
3. confirme a leitura ou quantidade
4. finalize ou abandone quando necessário

## Expedição

### Despachar

Usado para etapas operacionais de expedição ligadas ao despacho.

Orientação geral:

1. acesse `Expedicao`
2. abra `Despachar`
3. siga as validações da tela para documento, volume ou referência exigida

### Conferir Volume

Usado para conferência de volumes de expedição.

Orientação geral:

1. acesse `Expedicao`
2. abra `Conferir Volume`
3. selecione a transportadora ou documento
4. realize as leituras e acompanhe o resumo

### Conferir Romaneios

O fluxo de conferência de romaneios está disponível:

- no coletor: `Expedicao > Conferir Separacao`
- no web: `Expedicao > Conferir Romaneios`

### Importar Arquivo de Transportadora

1. Acesse `Expedicao > Importar arquivo de Transportadora`.
2. Selecione transportadora, movimento e PDF.
3. Clique em `Importar`; o processo começa diretamente, sem confirmação adicional.
4. O sistema gera uma etiqueta para cada volume novo.
5. NFs já existentes em `DocExpedicao` não são importadas novamente e não geram etiquetas.
6. No modo manual, revise a seleção; todas as etiquetas começam marcadas.
7. Use `Marcar Todos`, `Desmarcar Todos` ou marque somente os registros desejados.
8. Clique em `Imprimir selecionadas`, confira o preview e escolha a impressora no diálogo do Windows.
9. A grade mostra somente o último lote processado da filial.

Com impressão automática, o sistema usa a impressora padrão cadastrada. Com impressão manual, a tela permanece aberta e o navegador apresenta o preview das etiquetas.

Se nenhuma etiqueta for gerada, o sistema informa essa situação.

### Imprimir Etiquetas

1. Acesse `Expedição > Imprimir Etiquetas`.
2. Leia a chave DANFE ou informe o número da nota fiscal.
3. Confira a transportadora e a quantidade de volumes.
4. Informe o intervalo desejado.
5. Clique em `Imprimir`.
6. Confira o preview e escolha a impressora no diálogo do Windows.

A impressão não apresenta uma confirmação adicional antes do preview.

### Regra do Cliente para Geração de Etiqueta

- cliente identificado com `Etiqueta = 1` gera etiqueta;
- cliente identificado com `Etiqueta = 0` ou `NULL` não gera etiqueta;
- quando existem clientes com o mesmo nome, basta um cadastro com `Etiqueta = 1` para permitir a impressão;
- se todos os clientes homônimos estiverem em `0` ou `NULL`, a impressão é bloqueada;
- quando o cliente não for identificado com segurança, o sistema considera `Etiqueta = 1`.

## Conferência de Romaneios de Expedição

### Objetivo

Confirmar quantidades dos itens de um romaneio de expedição até a conclusão total do romaneio ou o envio de itens para busca.

### Regras Gerais

- ao selecionar o romaneio, a conferência inicia automaticamente
- não existe botão separado para iniciar a conferência
- o romaneio fica bloqueado para o usuário que o assumiu
- outro usuário não deve ver esse romaneio como disponível
- se houver tentativa de acesso concorrente, o sistema deve bloquear

### O que acontece ao iniciar a conferência

Ao assumir o romaneio:

- o romaneio vai para `StatusId = 9`
- os itens do romaneio em conferência vao para `StatusId = 9`
- `ConferenteId` recebe o usuário logado
- `DataConferente` recebe a data/hora atual do servidor

### Como operar no Web

1. Acesse `Expedicao > Conferir Romaneios`.
2. Selecione o `Romaneio Nr`.
3. A tela inicia a conferência automaticamente.
4. Informe a `Qtde Conferida` em cada linha desejada.
5. Clique em `Confirmar` ao final da grade.

Comportamento visual atual:

- `Item Nr` é exibido junto da descrição
- a grade apresenta `Zona`, `Item Nr`, `Qtde Pedido`, `Qtde Pendente` e `Qtde Conferida`
- não há cards auxiliares nesta tela

### Como operar no Coletor

1. Acesse `Expedicao > Conferir Separacao`.
2. Selecione o romaneio.
3. A conferência inicia automaticamente.
4. Informe a quantidade do item atual.
5. Confirme a leitura e avance.

### Regra para quantidade maior que a pendente

- o sistema não permite confirmar quantidade maior que a pendente
- a mensagem de erro deve ser tratada em popup

### Regra para quantidade parcial

Se a quantidade informada for menor que a pendente:

- `QtdeConferida` é somada
- o item continua em conferência
- a pendência continua visível

### Regra para quantidade total

Se a quantidade informada completar o item:

- `QtdeConferida` é atualizada
- o item vai para `StatusId = 4`
- o item deixa de ficar pendente

### Regra para quantidade zero

Se a quantidade informada for zero:

1. o sistema pede confirmação
2. ao confirmar, o item vai para `StatusId = 6`
3. o significado operacional é `Em Busca`

Mensagem padrão:

- `Confirma quantidade zero para este item?`

### Soltar Romaneio

Se o usuário precisar sair do fluxo sem concluir:

- o sistema permite soltar o romaneio
- o romaneio volta para `StatusId = 8`
- apenas itens ainda não confirmados voltam para `StatusId = 8`
- itens já confirmados não devem ser alterados
- `ConferenteId` e `DataConferente` dos não confirmados são limpos

### Finalização do Romaneio

Quando todos os itens estiverem concluidos:

- o romaneio vai para `StatusId = 4`

Itens considerados concluidos:

- `StatusId = 4` (`Finalizado`)
- `StatusId = 6` (`Em Busca`)

## Boas Práticas Operacionais

- sempre confirme se a filial ativa está correta antes de operar
- não compartilhe usuário entre operadores
- ao abandonar um fluxo, use a opcao prevista pela tela sempre que possível
- em divergência de quantidade, não force informações acima do pendente
- em casos de falta física, use o fluxo de quantidade zero para envio a busca

## Quando Acionar Suporte ou Usuário-Chave

Acione suporte funcional ou TI quando houver:

- romaneio preso com usuário incorreto
- item sem liberacao para continuidade
- romaneio que não aparece e deveria aparecer
- diferença entre status operacional esperado e comportamento da tela
- mensagem recorrente de bloqueio sem usuário ativo conhecido

## Referências

- [Procedimentos de Trabalho e Processos](PROCEDIMENTOS_TRABALHO_E_PROCESSOS.md)
- [Arquitetura e Integrações](ARQUITETURA_E_INTEGRACOES.md)
- [Guia de Desenvolvimento e Execução](GUIA_DESENVOLVIMENTO.md)
- [Atualizações Operacionais de 20/07/2026](ATUALIZACOES_20260720.md)
- [Atualizações Operacionais de 25/08/2026](ATUALIZACOES_20260825.md)
