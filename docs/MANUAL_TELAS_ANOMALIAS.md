# Manual por Tela - Anomalias

## Objetivo

Orientar o cadastro, a consulta e o acompanhamento de reclamações de peças GM no módulo `Anomalias` do Quasar Dealer.

O processo é sempre realizado na filial ativa. Cada item de uma mesma nota fiscal pode possuir um tipo de anomalia diferente.

## Acesso

No menu principal, abra `Anomalias` e escolha uma das opções:

- `Cadastrar Anomalia`: inicia um novo processo;
- `Consultar Anomalias`: pesquisa processos já cadastrados.

O dashboard também apresenta o card `Anomalias`, com a quantidade de itens que ainda estão em processo.

---

## Tela 01. Cadastrar Anomalia

### Localizar o item faturado

1. Informe o número do item GM no campo de pesquisa.
2. Clique em `Pesquisar`.
3. O sistema exibirá todas as combinações de NF e volume relacionadas ao item na filial atual.
4. Analise a quantidade faturada, o saldo disponível e a situação do prazo.
5. Clique em `Selecionar` na NF/volume que deseja reclamar.

As ocorrências são apresentadas do menor prazo limite para o maior. O sistema mantém visíveis também as NFs sem saldo ou fora do prazo, identificando o motivo pelo qual não podem ser utilizadas.

### Informações da lista de ocorrências

| Coluna | O que representa |
|---|---|
| `NF` | Número da nota fiscal GM |
| `Data emissão` | Data utilizada para calcular o prazo da reclamação |
| `Volume` | Volume de recebimento relacionado ao item |
| `Quantidade NF` | Quantidade faturada do item |
| `Saldo disponível` | Quantidade ainda disponível para reclamação |
| `Prazo / situação` | Data limite e indicação de NF dentro ou fora do prazo |

O saldo padrão de A, C e G considera a quantidade faturada já consumida por reclamações anteriores. Para o tipo B, o saldo considera o excesso efetivamente recebido.

### Informar os dados da anomalia

Após selecionar a NF/volume, o Quasar abre o popup padrão do sistema.

1. Selecione o `Tipo de Anomalia`.
2. Confira a quantidade já reclamada, o saldo disponível e a data limite.
3. Informe a quantidade reclamada em número inteiro.
4. Preencha os campos específicos do tipo selecionado.
5. Clique em `Adicionar`.

Tipos disponíveis:

| Tipo | Utilização | Informações adicionais |
|---|---|---|
| `A` | Falta | Quantidade reclamada e observação, quando necessária |
| `B` | Excesso | Quantidade efetivamente recebida e dados do item recebido |
| `C` | Item incorreto | Quantidade recebida, item efetivamente recebido e observação |
| `G` | Danificado | Detalhe do defeito ou dano, instalação no veículo e condições da embalagem |

> O tipo é escolhido por item. Um único processo pode conter itens com tipos diferentes.

### Prazo da reclamação

A data limite é calculada automaticamente a partir da data de emissão da NF e do `PrazoDias` configurado para o tipo de anomalia.

Regra de validação:

`data atual - data de emissão da NF <= prazo permitido para o tipo`

Se o prazo estiver vencido, o sistema informa a quantidade de dias decorridos e bloqueia a inclusão do item para aquele tipo.

### Saldo da reclamação

O sistema controla o saldo de forma transacional. Mesmo que dois usuários estejam trabalhando simultaneamente, a soma das reclamações não pode ultrapassar o saldo disponível.

- a quantidade deve ser inteira e maior que zero;
- a quantidade não pode ultrapassar o saldo apresentado;
- reclamações já cadastradas reduzem o saldo;
- um reenvio representa a mesma reclamação original e não consome saldo novamente.

### Finalizar o cadastro

1. Revise os itens adicionados na seção `Itens da Anomalia`.
2. Remova algum item pelo ícone de lixeira, se necessário.
3. Preencha `Observações do processo`, quando aplicável.
4. Clique em `Finalizar Cadastro`.

O Quasar cria um número de controle único para a filial. Esse número identifica o processo e nomeia os formulários exportados.

---

## Tela 02. Consultar Anomalias

Utilize os filtros para localizar processos por:

- `Controle Nr`;
- `Tipo`;
- `Status`.

A grade apresenta a data de abertura, tipos envolvidos, total de itens, quantidades em processo, aceitas e rejeitadas, status e usuário responsável pelo cadastro.

Clique em `Detalhes` para abrir o processo.

---

## Tela 03. Detalhes do Processo

A tela detalha todos os itens e apresenta NF, volume, quantidade faturada, quantidade reclamada, dados da anomalia, status e data limite.

### Tratar um item

Enquanto o item estiver `Em processo`:

- clique em `Aceitar` para confirmar o atendimento;
- clique em `Rejeitar` e informe obrigatoriamente o motivo da rejeição.

Quando não houver mais itens em processo, o processo passa a ser apresentado como finalizado.

### Exportar formulários GM

Os botões são exibidos conforme os tipos existentes no processo:

- `Exportar Formulário GM`: tipos A, B e C;
- `Exportar Danificados`: tipo G.

O Quasar preenche os modelos oficiais com os dados da empresa da filial, do processo, da nota fiscal e dos itens. O preço unitário e o imposto são obtidos do arquivo de recebimento `Trânsito GM`.

Quando a quantidade de itens exceder a capacidade de um formulário, o sistema gera mais de uma planilha e entrega um arquivo compactado. Cada arquivo utiliza o número do processo como identificação.

---

## Mensagens e bloqueios comuns

| Mensagem ou situação | Como proceder |
|---|---|
| `NF fora do prazo` | Escolher outra ocorrência dentro do prazo; a NF vencida não pode ser utilizada |
| `Sem saldo para reclamar` | Verificar reclamações anteriores ou escolher outra NF/volume elegível |
| Saldo B igual a zero | Conferir a quantidade efetivamente recebida; somente o excesso pode ser reclamado |
| Quantidade inválida | Informar um número inteiro entre 1 e o saldo disponível |
| Formulário sem dados empresariais | Solicitar a conferência do cadastro da empresa e do `CodigoGM` da filial |

## Boas práticas

- confirme a filial antes de iniciar o processo;
- pesquise sempre pelo número exato do item;
- confira NF e volume antes de selecionar;
- registre uma descrição objetiva do dano no tipo G;
- finalize o cadastro somente após revisar todos os itens;
- não altere manualmente as células estruturais dos formulários exportados.
