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

## Escopo deste repositório

Este repositório contém a aplicação web Quasar.Dealer. A API e o aplicativo móvel são projetos independentes e não fazem parte deste código.

## Segurança

- Não envie senhas, tokens ou arquivos `*.pubxml.user` ao repositório.
- Backups de banco, uploads, logs e arquivos compilados não devem ser versionados.
- O `Web.config` de produção deve permanecer exclusivamente no provedor.
