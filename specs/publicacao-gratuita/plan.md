# Plan: Publicacao gratuita do TowFast

## Referencia

- Spec: `specs/publicacao-gratuita/spec.md`
- Requisitos cobertos: `REQ-001` a `REQ-007`

## Abordagem

Publicar a SPA Angular pelo workflow oficial de GitHub Pages, compilar com o
subcaminho do repositorio e gerar `config.json` em tempo de build usando variaveis
publicas do repositorio. Hospedar a API existente como container ASP.NET Core no
Render e usar somente variaveis do ambiente para banco, JWT e bootstrap do primeiro
administrador. CORS na API aceita localhost e a origem do Pages.

## Componentes afetados

| Camada | Componente | Mudanca |
|---|---|---|
| Adapter | Angular runtime config | Resolver `config.json` em relacao a base do Pages |
| Adapter | ASP.NET Core CORS | Configurar origens para desenvolvimento e Pages |
| Bootstrap | API container | Adicionar Dockerfile e health check |
| Infraestrutura | GitHub Actions | Build Angular e deploy automatico do Pages |
| Infraestrutura | Render Blueprint | Declarar API free e variaveis de ambiente |
| Documentacao | README e spec | Passos de setup, Supabase, migrations e limitacoes |

## Portas e adapters

- Porta de entrada: endpoints HTTP existentes da API.
- Porta de saida: persistencia PostgreSQL existente.
- Adapter de entrada: SPA Angular no GitHub Pages.
- Adapter de saida: PostgreSQL Supabase e runtime Docker do Render.

## Modelo e regras

Nenhuma regra de dominio muda. CORS limita a origem browser; autenticacao JWT e
autorizacao existentes continuam na API.

## API e contratos

Nenhuma rota ou payload muda. A API escuta em HTTP na porta 8080 no container, com
TLS terminado pelo provedor e `/health/live` como health check.

## Interface web

Definir base href por build para `/TowFast/`; carregar configuracao de runtime
relativa ao `baseURI`; gerar `404.html` a partir de `index.html` para suportar
deep links no Pages.

## Persistencia e migracao

Nao ha alteracao de schema nesta funcionalidade. Na primeira inicializacao, usar
`Database__AutoMigrate=true` para aplicar migrations pendentes e criar o admin
bootstrap com credenciais fornecidas pelo operador. Apos confirmar a migration,
definir a variavel como `false` no Render para as inicializacoes seguintes.

## Seguranca e observabilidade

- Connection string Supabase, segredo JWT e senha de bootstrap permanecem no Render.
- URL da API e chave Maps sao configuracoes publicas de build, nunca secrets de banco.
- CORS usa a origem exata `https://guisousa5.github.io` e localhost.
- `/health/live` valida somente processo; `/health` consulta disponibilidade Postgres.

## Estrategia de testes

- Build: `pnpm exec ng build --base-href /TowFast/`.
- Backend: `dotnet build TowFast.slnx --configuration Release`.
- Deploy config: conferir que o output inclui `404.html` e runtime `config.json`.
- Fluxo externo: apos configurar provedores, abrir o Pages, registrar/login e
  confirmar persistencia no Supabase.

## Sequencia de entrega

1. Adaptar base href e resolucao de config para subcaminho.
2. Configurar CORS e container da API.
3. Criar blueprints e workflow Pages.
4. Documentar configuracao de contas e segredos.
5. Buildar e revisar; publicar ao conectar contas e definir variaveis externas.

## Riscos e mitigacoes

| Risco | Impacto | Mitigacao |
|---|---|---|
| Repo privado sem plano GitHub Pages pago | Workflow nao publica | Confirmar visibilidade antes do Pages |
| Free web service dorme | Primeira chamada pode demorar | Documentar cold start e avaliar plano pago depois |
| Connection string incompatível com IPv4 | API nao conecta ao Supabase | Preferir Shared Pooler Session em IPv4 |
| URL da API ausente no build | SPA fica sem backend | Falhar build sem `API_BASE_URL` |
| Migration em schema ainda nao aplicado | Chamadas falham | Habilitar migration inicial, conferir health e desligar AutoMigrate |

## Decisoes e alternativas

- Decisao: Pages para SPA, Render free para API e Supabase existente para dados.
- Alternativa: migrar API para Edge Functions; descartada pois exigiria reescrita
  das regras, contratos, autenticação e integração EF Core.
- Decisao: Angular continua estatico; nenhum segredo de servidor passa pelo browser.
