# Manual por Tela - Cadastros e Administração

## Objetivo

Relacionar os cadastros e controles administrativos que sustentam os processos operacionais do Quasar Dealer.

O acesso a cada opção depende do perfil do usuário. Alterações devem ser feitas por usuário autorizado e na filial correta.

## Filiais e Empresas

Em `Compras > Filiais` é possível incluir, alterar, consultar e excluir empresas/filiais.

O cadastro empresarial é utilizado por vários módulos. No processo de Anomalias, os dados da empresa e o `CodigoGM` são enviados aos formulários oficiais.

## Configuração

O módulo reúne:

- parâmetros da aplicação;
- parâmetros gerais;
- arquivos externos;
- áreas;
- equipamentos;
- materiais;
- impressoras.

### Áreas

Cadastre a descrição e o tipo operacional da área. O tipo determina onde a área será exibida, como no recebimento.

### Equipamentos e materiais

Mantenha os dados usados no estoque, nas zonas e nas regras operacionais. Antes de excluir, confirme que o registro não está vinculado a processos ativos.

### Impressoras e parâmetros

Cadastre endereço, porta e demais informações utilizadas pela impressão. Os parâmetros de impressão automática também dependem da configuração da filial.

## Controle de Acesso

O módulo reúne:

- usuários;
- perfis;
- funções;
- atividades de usuários conectados.

### Usuários

Permite incluir, alterar, consultar, excluir e associar perfil ao usuário.

### Perfis

Permite definir áreas e funções acessíveis. Após criar ou alterar um módulo, confira se o perfil correto recebeu o acesso correspondente.

### Funções

Representam permissões específicas utilizadas pela aplicação. Alterações incorretas podem ocultar ações ou liberar operações indevidas.

## Cadastros operacionais

Além dos módulos administrativos, os módulos operacionais mantêm cadastros próprios, entre eles:

- fornecedores no Recebimento;
- áreas, zonas, locações e equipamentos no Estoque;
- áreas de pedido e áreas de romaneio na Separação;
- clientes, transportadoras, rotas, paradas e veículos na Expedição.

Consulte o manual do respectivo módulo antes de alterar um cadastro operacional.

## Garantia

O módulo `Garantia` possui atualmente apenas a página inicial e a estrutura de menu. Não há fluxo operacional ativo documentável nesta versão.

## Boas práticas

- confirme a filial e o perfil antes de alterar dados;
- evite excluir registros já utilizados;
- registre parâmetros exatamente como definidos pelo ambiente;
- teste alterações de acesso com um usuário do perfil afetado;
- acione o suporte quando não houver segurança sobre o impacto do cadastro.
