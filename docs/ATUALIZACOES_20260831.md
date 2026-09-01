# Atualizações Operacionais de 31/08/2026

## Versão

- Web MVC: `1.0.9.0`.
- Situação: implantada em produção em `01/09/2026`.

## Novo módulo de Anomalias GM

Foi preparado o módulo web `Anomalias`, com funcionamento por filial e identidade visual padrão do Quasar Dealer.

Principais recursos:

- cadastro da reclamação a partir do número do item;
- listagem das NFs e volumes relacionados ao item pesquisado;
- escolha do tipo de anomalia por item, permitindo tipos diferentes no mesmo processo;
- cálculo automático da data limite conforme a emissão da NF e o prazo do tipo;
- indicação de NF fora do prazo e de item sem saldo;
- ordenação das ocorrências pelo prazo limite mais próximo;
- quantidade reclamada obrigatoriamente inteira;
- controle transacional do saldo reclamável;
- consulta e tratamento de itens em processo, aceitos ou rejeitados;
- card de itens pendentes no dashboard;
- exportação do formulário oficial GM para os tipos A, B e C;
- exportação do formulário de Danificados para o tipo G.

## Formulários GM

Os modelos oficiais ficam em `Simplify.Quasar/App_Data/Templates` e são distribuídos junto com a aplicação.

- `Formulario Anomalias GM.xls`: tipos A, B e C;
- `Formulario Danificados GM.xls`: tipo G.

Os arquivos gerados recebem o número do processo. Dados da empresa são obtidos conforme a filial, incluindo o código GM. Preço unitário e imposto são capturados das linhas DNI do upload `Trânsito GM`.

## Banco de dados

A implantação futura exige a execução, na ordem, dos scripts:

1. `20260831_AnomaliasGM_Fase1.sql`;
2. `20260831_AnomaliasGM_FormularioValores.sql`;
3. `20260831_AnomaliasGM_Danificados.sql`.

Os scripts criam as tabelas, índices, restrições, parâmetros e opções de menu do módulo, além dos novos campos usados nos formulários.

## Situação de implantação

Implantação concluída em produção em `01/09/2026`, incluindo:

- versão Web MVC `1.0.9.0`;
- sete tabelas do processo de Anomalias GM e respectivos índices, restrições e cadastros auxiliares;
- campos `Empresa.CodigoGM`, `NotaFiscalItem.PrecoUnitario`, `NotaFiscalItem.Imposto`, `TransitoUploadColumns.PrecoUnitario` e `TransitoUploadColumns.Imposto`;
- registros ativos do `AppMenu` para cadastrar e consultar Anomalias;
- formulários oficiais GM em `App_Data/Templates`;
- conteúdo do Manual referente a Anomalias;
- layout de impressão das etiquetas de localização no formato `100 x 50 mm`.

## Manual e interface

- o portal do Manual passa a relacionar Recebimento, Estoque, Separação, Expedição, Devolução, Anomalias e Cadastros;
- o catálogo e os procedimentos foram cruzados com os módulos e telas atuais;
- o módulo Garantia é identificado como estrutura sem fluxo operacional ativo nesta versão;
- os textos corrompidos das abas de Romaneios foram corrigidos;
- os diálogos de aceite e rejeição de Anomalias foram alinhados ao padrão visual do Quasar.
