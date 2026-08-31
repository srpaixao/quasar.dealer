# Quasar Dealer

Sistema web de gestão de peças, estoque e operação de concessionárias.

## Atualizações preparadas em 31/08/2026

O repositório inclui o novo módulo de Anomalias GM, ainda sem implantação em produção:

- cadastro por item, NF e volume;
- tipos A, B, C e G definidos por item;
- controle transacional de prazo e saldo;
- consulta, aceite e rejeição dos itens;
- card de pendências no dashboard;
- formulários oficiais de Anomalias e Danificados;
- scripts SQL, menu e documentação de implantação;
- Manual do Quasar atualizado com processos operacionais e cadastros.

## Versões da Entrega de 25/08/2026

- Web MVC: `1.0.8.0`
- Coletor: `1.0.7`
- API: ASP.NET Core `net9.0`

Principais mudanças:

- lista de números de volumes pendentes na descarga de recebimento
- teclado virtual do volume habilitado somente por ação do operador
- leitura de barcode processada por terminador `Enter` ou `Tab`
- conferência quantitativa de recebimento
- coleta e transferência orientadas por Locação de Espera
- conferência de separação em expedição

## Tecnologias

- .NET Framework 4.8
- ASP.NET MVC 5
- Entity Framework 6 (Database First)
- SQL Server
- Bootstrap, AdminLTE e jQuery

## Configuração local

O `Web.config` real não é versionado porque pode conter credenciais e configurações específicas do ambiente.

1. Copie `Simplify.Quasar/Web.config.example` para `Simplify.Quasar/Web.config`.
2. Configure a connection string `Quasar_Entities` somente no arquivo local.
3. Restaure os pacotes NuGet.
4. Abra `Simplify.Quasar.sln` no Visual Studio 2022.
5. Execute a aplicação em IIS Express.

## Componentes

- `Simplify.Quasar/`: aplicação web ASP.NET MVC.
- `Api/`: API utilizada pelo coletor.
- `Coletor/`: aplicação web móvel para coleta e operação no depósito.
- `docs/`: manuais, processos, arquitetura, notas de versão e scripts de evolução.

## Documentação

- [Índice da documentação](docs/README.md)
- [Atualizações operacionais de 25/08/2026](docs/ATUALIZACOES_20260825.md)
- [Manual de utilização](docs/MANUAL_UTILIZACAO.md)
- [Procedimentos de trabalho](docs/PROCEDIMENTOS_TRABALHO_E_PROCESSOS.md)

### Configuração da API

1. Copie `Api/appsettings.example.json` para `Api/appsettings.json`.
2. Configure a conexão com o banco e uma chave JWT forte somente no arquivo local.
3. Execute `dotnet restore` e `dotnet run` dentro de `Api/`.

### Configuração do coletor

1. Copie `Coletor/.env.example` para `Coletor/.env`.
2. Ajuste `VITE_API_BASE_URL` para o endereço da API.
3. Execute `npm install` e `npm run dev` dentro de `Coletor/`.

## Segurança

- Não envie senhas, tokens ou arquivos `*.pubxml.user` ao repositório.
- Backups de banco, uploads, logs, arquivos `.env`, `appsettings` reais e arquivos compilados não devem ser versionados.
- O `Web.config` de produção deve permanecer exclusivamente no provedor.
