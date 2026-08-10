# QuasarApi

API backend do projeto **Quasar Dealer Nova Chevrolet**, construída em **ASP.NET Core 9** com **Entity Framework Core** e **SQL Server**. A aplicação expõe endpoints HTTP para autenticação, cadastros básicos e operações logísticas ligadas a recebimento, armazenagem, estoque e expedição.

Este README foi escrito para acelerar o onboarding de quem precisa manter ou evoluir o projeto.

## Visão geral do domínio

A API atende fluxos operacionais de logística/armazenagem. Os principais conceitos do domínio presentes no código são:

- `Empresa`: filiais/empresas ativas disponíveis para uso.
- `Usuario`: autenticação e administração de usuários por filial.
- `Area`: áreas operacionais, usadas principalmente no recebimento e conferência de volumes.
- `Material`: cadastro de itens.
- `Estoque`: saldo, indisponibilidade, pedidos pendentes e locação de itens.
- `Locacao`: posições físicas de armazenagem.
- `NotaFiscal` e `NotaFiscalItem`: documentos e itens recebidos.
- `RetornoInterno` e `RetornoInternoItem`: retornos internos também considerados na armazenagem.
- `Volume` e `StatusVolume`: volumes recebidos/conferidos por área.
- `Transportadora`: transportadoras usadas em expedição.
- `DocExpedicao`: documentos disponíveis para despacho.
- `HistoricoDespacho`: histórico dos volumes expedidos.
- `DMS`: configuração da API cliente externa.

Em termos práticos, a API cobre estes fluxos:

1. autenticação do operador;
2. consulta de cadastros e parâmetros;
3. conferência de volumes no recebimento;
4. validação e atualização de armazenagem;
5. consulta e movimentação de estoque;
6. despacho de volumes na expedição.

## Stack técnica

- `.NET 9` (`net9.0`)
- `ASP.NET Core Minimal APIs`
- `Entity Framework Core 9`
- `SQL Server`
- `JWT Bearer Authentication`
- `BCrypt` para validação e geração de hash de senha
- `Swagger / Swashbuckle` para documentação e testes manuais

Dependências definidas em [`QuasarApi.csproj`](/mnt/d/Quasar/Dev/Nova/api/QuasarApi.csproj):

- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Swashbuckle.AspNetCore`
- `BCrypt.Net-Next`

## Estrutura do projeto

```text
api/
├── Program.cs
├── Extensions/
├── Middleware/
├── Database/
│   ├── AppDbContext.cs
│   └── Models/
├── Routes/
│   ├── Management/
│   └── Operations/
├── Services/
├── DTO/
├── Helpers/
└── appsettings*.json
```

### Pastas principais

- [`Program.cs`](/mnt/d/Quasar/Dev/Nova/api/Program.cs): ponto de entrada, montagem da aplicação, leitura de configuração, registro de rotas e endpoint raiz.
- [`Extensions/BuilderExtensions.cs`](/mnt/d/Quasar/Dev/Nova/api/Extensions/BuilderExtensions.cs): registro de infraestrutura, `DbContext`, Swagger, CORS, autenticação JWT e serviços de domínio.
- [`Extensions/AppExtensions.cs`](/mnt/d/Quasar/Dev/Nova/api/Extensions/AppExtensions.cs): configuração do pipeline HTTP.
- [`Extensions/RoutesExtensions.cs`](/mnt/d/Quasar/Dev/Nova/api/Extensions/RoutesExtensions.cs): centraliza o mapeamento das rotas.
- [`Database/AppDbContext.cs`](/mnt/d/Quasar/Dev/Nova/api/Database/AppDbContext.cs): contexto EF Core e mapeamento das entidades.
- [`Database/Models`](/mnt/d/Quasar/Dev/Nova/api/Database/Models): entidades persistidas no banco.
- [`Routes/Management`](/mnt/d/Quasar/Dev/Nova/api/Routes/Management): endpoints de cadastro/configuração.
- [`Routes/Operations`](/mnt/d/Quasar/Dev/Nova/api/Routes/Operations): endpoints operacionais.
- [`Services`](/mnt/d/Quasar/Dev/Nova/api/Services): regras encapsuladas em serviços reaproveitáveis.
- [`Middleware/ExceptionMiddleware.cs`](/mnt/d/Quasar/Dev/Nova/api/Middleware/ExceptionMiddleware.cs): tratamento global de exceções.

## Como a aplicação sobe

O bootstrap atual está em [`Program.cs`](/mnt/d/Quasar/Dev/Nova/api/Program.cs):

1. cria o `WebApplicationBuilder`;
2. chama `AddArchitectures()` para registrar controllers, Swagger, `DbContext`, CORS e autenticação;
3. chama `AddServices()` para registrar serviços de domínio;
4. recarrega `appsettings.json` e `appsettings.{Environment}.json`;
5. constrói a aplicação;
6. aplica `UseServices()` para Swagger, CORS, autenticação, autorização e middleware de exceção;
7. publica um endpoint raiz `GET /`;
8. registra todos os módulos via `MapRoutes(builder)`.

O endpoint `GET /` funciona como health/info endpoint e informa:

- versão hardcoded da API;
- servidor e banco lidos da connection string;
- ambiente ativo.

## Configuração

Os ambientes disponíveis no repositório são:

- [`appsettings.json`](/mnt/d/Quasar/Dev/Nova/api/appsettings.json)
- [`appsettings.Development.json`](/mnt/d/Quasar/Dev/Nova/api/appsettings.Development.json)
- [`appsettings.Test.json`](/mnt/d/Quasar/Dev/Nova/api/appsettings.Test.json)
- [`appsettings.Production.json`](/mnt/d/Quasar/Dev/Nova/api/appsettings.Production.json)

Chaves mais importantes:

- `ConnectionStrings:DefaultConnection`: conexão SQL Server usada pelo `AppDbContext`.
- `AllowedOrigins`: lista de origens esperadas para CORS.
- `Jwt:Key`: chave usada para assinatura e validação do token.
- `Jwt:Issuers` e `Jwt:Audiences`: presentes em configuração, mas hoje não são realmente validados.

### Observações importantes sobre configuração

- A aplicação lê `appsettings` e depois `EnvironmentVariables`, então variáveis de ambiente podem sobrescrever valores.
- Exemplos de override por ambiente:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="Data Source=localhost,1433;Initial Catalog=Quasar_Nova;User ID=sa;Password=SuaSenha;Encrypt=False;TrustServerCertificate=False"
export Jwt__Key="uma-chave-segura"
```

- O projeto **não possui pasta de migrations** versionada. Na prática, isso indica que o banco precisa existir previamente com um schema compatível.
- Os arquivos atuais de `appsettings` contêm credenciais reais/sensíveis. Para ambientes compartilhados, o correto é mover isso para secret manager, variáveis de ambiente ou cofre de segredos.

## Execução local

### Pré-requisitos

- SDK do `.NET 9`
- Instância de `SQL Server` acessível pela connection string configurada
- Banco com schema compatível com as entidades do projeto

### Comandos básicos

```bash
dotnet restore
dotnet build
dotnet run --launch-profile http
```

Ou, para subir com HTTPS:

```bash
dotnet run --launch-profile https
```

Perfis definidos em [`Properties/launchSettings.json`](/mnt/d/Quasar/Dev/Nova/api/Properties/launchSettings.json):

- `http`: `http://localhost:5049`
- `https`: `https://localhost:7268` e `http://localhost:5049`
- `Quasar Dev`: ambiente `Test`
- `Quasar Prod`: ambiente `Production`

### Swagger

O Swagger é habilitado via `UseSwagger()` e `UseSwaggerUI()` sem restrição por ambiente. Em execução local, normalmente estará disponível em:

- `http://localhost:5049/swagger`
- `https://localhost:7268/swagger`

## Banco de dados e acesso a dados

O contexto principal é [`Database/AppDbContext.cs`](/mnt/d/Quasar/Dev/Nova/api/Database/AppDbContext.cs). Ele registra `DbSet`s para as entidades centrais do processo:

- `Area`
- `Cliente`
- `Empresa`
- `Equipamento`
- `Estoque`
- `Fornecedor`
- `Transportadora`
- `Locacao`
- `Material`
- `NotaFiscal`
- `NotaFiscalItem`
- `RetornoInterno`
- `RetornoInternoItem`
- `Usuario`
- `Volume`
- `StatusVolume`
- `HistoricoArmazenagem`
- `Movimentacao`
- `MovimentacaoDestino`
- `DocExpedicao`
- `HistoricoDespacho`
- `DMS`

Características relevantes:

- há chaves simples e compostas, como `Volume` com chave `{ NotaFiscalNr, VolumeNr }`;
- parte do mapeamento é feita por convenção e parte manualmente no `OnModelCreating`;
- a maioria das consultas filtra por `FilialId`, então esse parâmetro é essencial para quase todos os fluxos.

## Autenticação e autorização

O login está implementado em [`Routes/Operations/AuthRoutes.cs`](/mnt/d/Quasar/Dev/Nova/api/Routes/Operations/AuthRoutes.cs).

### Entrada esperada

```json
{
  "usuario": "operador",
  "senha": "senha-plana",
  "empresaId": 1,
  "filialId": 1
}
```

### Comportamento atual

- o usuário é localizado por `Login` e `FilialId`;
- a senha é validada com `BCrypt`, via [`Helpers/CryptoHelper.cs`](/mnt/d/Quasar/Dev/Nova/api/Helpers/CryptoHelper.cs);
- o token JWT retorna no corpo da resposta;
- o token expira em 1 hora;
- a aplicação também tenta ler token de cookie chamado `token`, embora o login atualmente **não grave esse cookie**;
- `ValidateIssuer` e `ValidateAudience` estão desabilitados, apesar de `Issuer` e `Audience` existirem em configuração.

### Uso nas rotas

A maior parte das rotas operacionais usa `RequireAuthorization()`. Algumas rotas de gestão atualmente não exigem autenticação, então vale revisar isso antes de expor o sistema publicamente.

## Módulos e endpoints

### Gestão e cadastros

| Prefixo/rota | Objetivo | Endpoints principais |
| --- | --- | --- |
| `/auth` | autenticação | `POST /auth/login` |
| `/empresas` | listar filiais ativas | `GET /empresas` |
| `/usuarios` | CRUD de usuários | `GET /usuarios`, `GET /usuarios/{id}`, `POST`, `PUT`, `PATCH`, `DELETE` |
| `/materiais` | consulta de material | `GET /materiais/{codigo}` |
| `/areas` | CRUD de áreas | `GET /areas`, `GET /areas/{id}`, `POST`, `PUT`, `DELETE` |
| `/config` | parâmetros externos | `GET /config/cliente-api` |

### Operações logísticas

| Prefixo/rota | Objetivo | Endpoints principais |
| --- | --- | --- |
| `/recebimento` | conferência de volumes | `GET /volumeresumo/{statusId}/{areaId}`, `POST /volumeupdate` |
| `/armazenagem` | validação e baixa de armazenagem | `GET /validarmaterial/{codigo}`, `GET /validarquantidade/{codigo}`, `POST /atualizarItemNotaFiscal`, `POST /gravarHistorico` |
| `/estoque` | consulta e transferência interna | `GET /consultaritem/{itemnr}`, `GET /consultarlocacao/{codigo}`, `GET /consultarmovimentacao/{itemnr}`, `POST /movimentacao`, `PUT /movimentacao/{id}` |
| `/transportadoras` | lista simples para seleção | `GET /transportadoras` |
| `/expedicao` | despacho e conferência de volumes expedidos | `GET /transportadoras`, `GET /volumes/resumo/{transportadoraId}`, `GET /volumes/pendentes/{transportadoraId}`, `GET /volumes/lidos/{transportadoraId}`, `GET /doc`, `GET /historico/volumes`, `POST /historico` |

## Regras de negócio relevantes já implementadas

### Recebimento

As regras centrais estão em [`Services/VolumeService.cs`](/mnt/d/Quasar/Dev/Nova/api/Services/VolumeService.cs) e [`Services/ConferenciaVolumeService.cs`](/mnt/d/Quasar/Dev/Nova/api/Services/ConferenciaVolumeService.cs):

- `ResumoVolumesAsync` consolida volumes por número, trazendo quantidade de itens, status e data de criação.
- `UpdateVolumeAsync` realiza a conferência do volume por área.
- se o volume não existe para a área/filial, a API cria um registro com `StatusId = 3` e retorna como volume incorreto.
- quando o volume é conferido, os itens da nota fiscal vinculados ao volume têm o status atualizado.
- se não restarem itens pendentes da nota fiscal, a nota é marcada como finalizada.
- o retorno inclui resumo agregado: total, pendentes, conferidos e incorretos.

### Armazenagem

As regras estão principalmente em [`Routes/Operations/ArmazenagemRoutes.cs`](/mnt/d/Quasar/Dev/Nova/api/Routes/Operations/ArmazenagemRoutes.cs):

- valida material e locação de estoque;
- calcula quantidade disponível para armazenar somando saldo pendente de nota fiscal e retorno interno;
- atualiza quantidades armazenadas em `NotaFiscalItem` e `RetornoInternoItem`;
- finaliza documentos quando todos os itens atingem o status esperado;
- grava histórico de armazenagem.

Grande parte da regra está diretamente no handler da rota, com transações explícitas via EF Core.

### Estoque

As regras estão em [`Routes/Operations/EstoqueRoutes.cs`](/mnt/d/Quasar/Dev/Nova/api/Routes/Operations/EstoqueRoutes.cs):

- consulta material e dados de estoque por item;
- consulta locação e todos os itens presentes naquela posição;
- consulta movimentações ainda não finalizadas;
- cria movimentação de coleta/transferência;
- finaliza movimentação existente.

### Expedição

As regras estão em [`Routes/Operations/ExpedicaoRoutes.cs`](/mnt/d/Quasar/Dev/Nova/api/Routes/Operations/ExpedicaoRoutes.cs):

- lista transportadoras habilitadas para emissão de etiqueta;
- calcula resumo de volumes por transportadora;
- retorna documentos pendentes e volumes já lidos;
- consulta documento por número;
- grava histórico de despacho e incrementa `QtdVolConf` no `DocExpedicao`.

## Serviços registrados no DI

Atualmente, o container registra:

- `IAreaService` -> `AreaService`
- `IVolumeService` -> `VolumeService`
- `IConferenciaVolumeService` -> `ConferenciaVolumeService`

Observação importante: nem toda regra de negócio está nesses serviços. Uma parte expressiva ainda fica diretamente nos arquivos de rota.

## Tratamento de erro

O middleware global em [`Middleware/ExceptionMiddleware.cs`](/mnt/d/Quasar/Dev/Nova/api/Middleware/ExceptionMiddleware.cs):

- captura exceções não tratadas;
- responde `500` em JSON;
- devolve `Message` genérica;
- devolve também `Detailed` com a mensagem original da exceção.

Isso ajuda em desenvolvimento, mas pode expor detalhes internos em produção.

## Convenções e particularidades do código

- o projeto usa **Minimal APIs**, apesar de `AddControllers()` e `MapControllers()` estarem habilitados;
- o namespace do contexto está como `QuasarApi.DataBase`, enquanto a pasta é `Database`;
- o projeto contém um `package.json`, mas o build principal da API é .NET;
- vários fluxos dependem de `filialId` por query string ou payload;
- `CurrentDateTime.GetCurrentDateTime()` tenta converter UTC para `E. South America Standard Time`, com fallback para `DateTime.Now`;
- Swagger e CORS são habilitados globalmente;
- a política CORS hoje está efetivamente aberta com `AllowAnyOrigin()`, embora exista `AllowedOrigins` em configuração.

## Riscos e pontos de atenção para quem for manter

- **Banco sem migrations versionadas**: mudanças de schema precisam ser controladas fora deste repositório.
- **Credenciais em arquivo**: há dados sensíveis nos `appsettings`.
- **Regras espalhadas nas rotas**: parte importante da lógica de negócio está nos handlers, o que dificulta testes e reaproveitamento.
- **Autorização inconsistente**: nem todos os endpoints de gestão usam `RequireAuthorization()`.
- **CORS permissivo**: configuração atual não respeita a lista de origens cadastradas.
- **Detalhes de erro expostos**: o middleware devolve a mensagem interna da exceção.

## Sugestão de leitura para onboarding

Se você precisa entender rapidamente o projeto, leia nesta ordem:

1. [`Program.cs`](/mnt/d/Quasar/Dev/Nova/api/Program.cs)
2. [`Extensions/BuilderExtensions.cs`](/mnt/d/Quasar/Dev/Nova/api/Extensions/BuilderExtensions.cs)
3. [`Extensions/RoutesExtensions.cs`](/mnt/d/Quasar/Dev/Nova/api/Extensions/RoutesExtensions.cs)
4. [`Database/AppDbContext.cs`](/mnt/d/Quasar/Dev/Nova/api/Database/AppDbContext.cs)
5. rotas de negócio em [`Routes/Operations`](/mnt/d/Quasar/Dev/Nova/api/Routes/Operations)
6. serviços em [`Services`](/mnt/d/Quasar/Dev/Nova/api/Services)

Essa sequência já dá uma visão clara de bootstrap, infraestrutura, modelo de dados e fluxo funcional da aplicação.
