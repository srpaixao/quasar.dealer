# Gerador do Manual

Os arquivos Markdown operacionais em `web/docs` são a fonte oficial do manual.
O portal da aplicação utiliza versões HTML geradas e armazenadas em
`Simplify.Quasar/App_Data/Manual`.

## Atualização

No diretório `Simplify.Quasar/tools/manual`, executar:

```powershell
npm ci
npm run review:accents
npm run build
```

Depois da geração:

1. validar o portal em `/Manual`;
2. compilar o projeto com `MvcBuildViews=true`;
3. versionar os Markdown, os HTML gerados e as imagens atualizadas.

Somente os documentos operacionais definidos em `build.mjs` são publicados.
Arquitetura, integrações, SQL e guias de desenvolvimento ficam fora do portal.

Última revisão dos Markdown operacionais: `31/08/2026`.
