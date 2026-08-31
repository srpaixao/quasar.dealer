# Manual por Tela - Devolução

## Objetivo

Orientar o cadastro, a consulta e o tratamento de processos de devolução no Quasar Dealer.

## Tela 01. Cadastrar Devolução

1. Selecione o tipo de movimento, a necessidade de retirada, o status e o motivo.
2. Informe a NF de venda e clique em `Buscar` para carregar cliente, data da venda e vendedor.
3. Informe a NF de devolução, sinistro, placa e transportadora quando aplicável.
4. Preencha as observações gerais.
5. Localize o material, informe quantidade inteira, valor unitário e observação do item.
6. Clique em `Adicionar` e confira o item na grade.
7. Repita a inclusão para os demais itens.
8. Clique em `Salvar Devolução`.

O sistema gera um número de controle para o processo. Após salvar, a opção de impressão fica disponível.

## Tela 02. Consultar Processos

A consulta lista o número de controle, cliente, transportadora, status e data/hora do cadastro.

Clique no número de controle para abrir os detalhes. Na tela de detalhes é possível atualizar status, movimento, retirada, transportadora, motivo, NF de devolução e observações, conforme o perfil de acesso.

Também estão disponíveis as ações:

- `Imprimir`: abre o documento do processo;
- `Salvar`: grava as alterações;
- `Excluir`: solicita confirmação antes de excluir o processo.

## Tela 03. Ocorrências

A lista de ocorrências apresenta controle, nota fiscal, emissor, quantidade de linhas, quantidade de peças e última atualização.

Ao abrir uma ocorrência:

1. confira os dados da devolução;
2. selecione o novo status de cada item;
3. informe a quantidade tratada;
4. registre a observação do tratamento;
5. salve as alterações.

## Validações principais

- a filial ativa define os dados disponíveis;
- a NF de venda precisa ser localizada antes do cadastro;
- quantidade deve ser inteira e maior que zero;
- campos obrigatórios são validados antes de salvar;
- a exclusão exige confirmação explícita;
- alterações de ocorrência devem manter quantidade tratada dentro da quantidade registrada.

## Boas práticas

- confira cliente, NF e filial antes de adicionar itens;
- não misture itens de processos diferentes;
- registre observações objetivas;
- atualize o status sempre que houver avanço operacional;
- imprima o processo somente após revisar os dados salvos.
