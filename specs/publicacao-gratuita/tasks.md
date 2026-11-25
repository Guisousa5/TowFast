# Tasks: Publicacao gratuita do TowFast

## Referencias

- Spec: `specs/publicacao-gratuita/spec.md`
- Plan: `specs/publicacao-gratuita/plan.md`

## Tarefas

- [x] `TASK-001` - Adaptar a SPA ao subcaminho do GitHub Pages
  - Requisitos: `REQ-002`
  - Arquivos provaveis: `web/towfast-web/src/index.html`, `web/towfast-web/src/app/core/runtime-config.service.ts`
  - Implementacao: carregar runtime config em relacao ao elemento base e permitir base href definida no build.
  - Verificacao: `pnpm exec ng build --base-href /TowFast/`; inspecionar config e deep-link fallback.
  - Dependencias: nenhuma

- [x] `TASK-002` - Preparar CORS e container da API
  - Requisitos: `REQ-003`, `REQ-004`, `REQ-005`
  - Arquivos provaveis: `src/TowFast.Api/Program.cs`, `src/TowFast.Api/Dockerfile`, `render.yaml`
  - Implementacao: habilitar origem Pages, configurar container Linux e Blueprint gratuito sem incluir credenciais.
  - Verificacao: build .NET e revisao de env vars sem secrets versionados.
  - Dependencias: `TASK-001`

- [x] `TASK-003` - Automatizar publicacao no Pages
  - Requisitos: `REQ-001`, `REQ-002`, `REQ-006`
  - Arquivos provaveis: `.github/workflows/deploy-pages.yml`, `web/towfast-web/public/config.json`
  - Implementacao: instalar Node/pnpm, compilar Angular, gerar config, copiar 404 e publicar artifact do Pages.
  - Verificacao: workflow valido, URL da API obrigatoria e artifact estatico correto.
  - Dependencias: `TASK-001`

- [x] `TASK-004` - Documentar o setup externo e revisar deploy
  - Requisitos: `REQ-005`, `REQ-007`
  - Arquivos provaveis: `README.md`, spec/plan/tasks de publicacao
  - Implementacao: registrar GitHub Pages, Render, Supabase session pooler, migrations e variaveis.
  - Verificacao: revisar requisitos e executar builds locais.
  - Dependencias: `TASK-002`, `TASK-003`

## Validacao final

- [ ] AC-001 e AC-002 foram verificados nos provedores apos configurar contas e variaveis.
- [x] Build .NET e testes automatizados passaram.
- [x] Build Angular e testes de CI passaram.
- [x] Revisao local nao encontrou achados bloqueantes.
- [x] Riscos e passos dependentes das contas foram registrados.
