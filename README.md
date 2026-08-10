# Quasar Dealer

Sistema web de gestão de peças, estoque e operação de concessionárias.

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
