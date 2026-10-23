# Tasks: Plataforma Web TowFast

## Referencias

- Spec: `specs/towfast-web/spec.md`
- Plan: `specs/towfast-web/plan.md`
- Requisitos: `REQ-001` a `REQ-043`
- Criterios de aceite: `AC-001` a `AC-019`

## Fundacao

- [ ] `TASK-001` - Criar workspace Angular standalone e strict
  - Requisitos: `REQ-034`, `REQ-040`
  - Arquivos provaveis: `web/towfast-web/angular.json`, `web/towfast-web/package.json`, `web/towfast-web/src/`
  - Implementacao: criar Angular 22 sem SSR, TypeScript strict, aliases, estilos globais, testes e lockfile.
  - Verificacao: `npm ci && npm run build`
  - Dependencias: nenhuma

- [x] `TASK-002` - Instalar e configurar NG-ZORRO e icones
  - Requisitos: `REQ-036`, `REQ-043`
  - Arquivos provaveis: `web/towfast-web/src/app/app.config.ts`, `web/towfast-web/package.json`
  - Implementacao: instalar NG-ZORRO 22, locale pt-BR, Ant Design Icons registrados e imports standalone.
  - Verificacao: pagina de smoke renderiza Button, Input, Modal e Message sem warnings.
  - Dependencias: `TASK-001`

- [ ] `TASK-003` - Configurar lint, formatacao e budgets
  - Requisitos: `REQ-034`, `REQ-036`, `REQ-040`
  - Arquivos provaveis: `web/towfast-web/eslint.config.*`, `.prettierrc`, `angular.json`, `package.json`
  - Implementacao: regras strict, scripts de check e budgets de bundle conforme plan.
  - Verificacao: `npm run lint && npm run format:check && npm run build:production`
  - Dependencias: `TASK-001`

- [ ] `TASK-004` - Implementar configuracao runtime antes do bootstrap
  - Requisitos: `REQ-031`, `REQ-032`, `REQ-041`, `REQ-042`
  - Arquivos provaveis: `public/config/`, `scripts/generate-runtime-config.mjs`, `src/app/core/config/`, `src/main.ts`
  - Implementacao: carregar e validar API URL, chave/Map ID Google, intervalos e ambiente antes de iniciar Angular.
  - Verificacao: testes rejeitam config ausente/invalida e build unica inicia com dois arquivos de ambiente.
  - Dependencias: `TASK-001`

- [ ] `TASK-005` - Definir estrutura lazy e aliases do projeto
  - Requisitos: `REQ-002`, `REQ-005`
  - Arquivos provaveis: `src/app/app.routes.ts`, `src/app/core/`, `src/app/features/`, `tsconfig.json`
  - Implementacao: criar boundaries core/shared/api/features e rotas lazy vazias para os quatro shells.
  - Verificacao: teste de rotas confirma chunks lazy e ausencia de ciclos de dependencia.
  - Dependencias: `TASK-001`

- [ ] `TASK-006` - Configurar runner unitario e Playwright
  - Requisitos: `REQ-034`, `REQ-036`, `REQ-040`
  - Arquivos provaveis: `package.json`, `playwright.config.ts`, `e2e/`, configuracao de testes Angular
  - Implementacao: scripts unit, component, e2e e screenshots com servidor local deterministico.
  - Verificacao: testes vazios de smoke passam em Chromium desktop e mobile.
  - Dependencias: `TASK-001`

## Contrato e HTTP

- [ ] `TASK-007` - Versionar snapshot OpenAPI da TowFast API
  - Requisitos: `REQ-016`, `REQ-019`, `REQ-020`, `REQ-033`
  - Arquivos provaveis: `web/towfast-web/openapi/towfast-v1.json`, `scripts/sync-openapi.mjs`
  - Implementacao: exportar OpenAPI, validar JSON, versao v1 e esquema Bearer.
  - Verificacao: `npm run api:sync && npm run api:validate`
  - Dependencias: `TASK-001`

- [ ] `TASK-008` - Gerar cliente TypeScript Angular pelo OpenAPI
  - Requisitos: `REQ-001` a `REQ-033`
  - Arquivos provaveis: `src/app/api/generated/`, `package.json`, configuracao do gerador
  - Implementacao: fixar gerador compativel, gerar models/services e impedir edicao manual.
  - Verificacao: `npm run api:generate` e segunda execucao nao produz diff.
  - Dependencias: `TASK-007`

- [ ] `TASK-009` - Criar facades de API por dominio
  - Requisitos: `REQ-001` a `REQ-033`
  - Arquivos provaveis: `src/app/api/api-facade/`
  - Implementacao: AuthApi, ClientsApi, ProvidersApi, ServiceTypesApi, RequestsApi, TrackingApi e HealthApi.
  - Verificacao: testes exercitam todos os 34 grupos de endpoints da matriz da spec.
  - Dependencias: `TASK-008`

- [ ] `TASK-010` - Normalizar ProblemDetails em AppError
  - Requisitos: `REQ-034`, `REQ-039`
  - Arquivos provaveis: `src/app/core/errors/`, `src/app/core/http/`
  - Implementacao: mapear status, title, detail, correlationId, erros de campo e retryable.
  - Verificacao: testes cobrem `400`, `401`, `403`, `404`, `409`, `429`, `5xx` e falha de rede.
  - Dependencias: `TASK-009`

- [ ] `TASK-011` - Implementar interceptor de correlation ID
  - Requisitos: `REQ-039`
  - Arquivos provaveis: `src/app/core/http/correlation-id.interceptor.ts`, `src/app/core/observability/`
  - Implementacao: gerar UUID por requisicao, propagar header e capturar ID retornado sem dados pessoais.
  - Verificacao: teste HTTP confirma headers distintos e AppError com ID copiavel.
  - Dependencias: `TASK-009`

- [ ] `TASK-012` - Criar mock server aderente ao OpenAPI
  - Requisitos: `REQ-001` a `REQ-043`
  - Arquivos provaveis: `web/towfast-web/mocks/`, `package.json`
  - Implementacao: cenarios felizes e erros deterministas por papel para desenvolvimento e E2E.
  - Verificacao: smoke chama login, lista e ProblemDetails sem backend real.
  - Dependencias: `TASK-007`

## Sessao e autorizacao

- [ ] `TASK-013` - Implementar SessionStore com Signals
  - Requisitos: `REQ-002`, `REQ-004`, `REQ-005`
  - Arquivos provaveis: `src/app/core/auth/session.store.ts`, `session-storage.port.ts`
  - Implementacao: estados da sessao, access token em memoria, refresh/expiracao em sessionStorage e usuario atual.
  - Verificacao: testes cobrem hidratar, autenticar, atualizar token e limpar todos os dados.
  - Dependencias: `TASK-009`

- [ ] `TASK-014` - Implementar Bearer interceptor
  - Requisitos: `REQ-002`, `REQ-005`
  - Arquivos provaveis: `src/app/core/http/auth.interceptor.ts`
  - Implementacao: anexar access token apenas a URLs da TowFast API e nunca aos hosts Google.
  - Verificacao: teste confirma header na API, ausencia em Maps e ausencia sem sessao.
  - Dependencias: `TASK-013`

- [ ] `TASK-015` - Implementar refresh token single-flight
  - Requisitos: `REQ-003`, `REQ-004`
  - Arquivos provaveis: `src/app/core/auth/refresh.service.ts`, `src/app/core/http/refresh.interceptor.ts`
  - Implementacao: serializar refresh, aguardar chamada compartilhada, retry unico e logout em falha.
  - Verificacao: teste concorrente dispara varios `401` e observa um unico refresh, cobrindo `AC-002`.
  - Dependencias: `TASK-013`, `TASK-014`

- [ ] `TASK-016` - Implementar restauracao e logout resiliente
  - Requisitos: `REQ-003`, `REQ-004`
  - Arquivos provaveis: `src/app/core/auth/session-bootstrap.service.ts`, `logout.service.ts`
  - Implementacao: renovar ao recarregar, bloquear shell ate resolver e limpar local mesmo com falha de logout.
  - Verificacao: testes cobrem refresh valido, expirado, reutilizado e API indisponivel.
  - Dependencias: `TASK-015`

- [ ] `TASK-017` - Implementar guards de autenticacao, papel e escopo
  - Requisitos: `REQ-002`, `REQ-005`
  - Arquivos provaveis: `src/app/core/guards/`, `src/app/app.routes.ts`
  - Implementacao: authGuard, anonymousGuard, roleGuard e scopeGuard com retorno seguro.
  - Verificacao: matriz de testes cobre cinco papeis e impede open redirect.
  - Dependencias: `TASK-013`, `TASK-016`

- [ ] `TASK-018` - Derivar menus e redirecionamento por papel
  - Requisitos: `REQ-002`, `REQ-005`
  - Arquivos provaveis: `src/app/core/layout/navigation.config.ts`, `role-home.ts`
  - Implementacao: gerar navegacao a partir de configuracao tipada compartilhada com as rotas.
  - Verificacao: `AC-003` passa para Admin, Operator, Client, Provider e Driver.
  - Dependencias: `TASK-017`

## Design system e shells

- [x] `TASK-019` - Criar tokens TowFast para tema claro
  - Requisitos: `REQ-036`, `REQ-040`, `REQ-043`
  - Arquivos provaveis: `src/app/theme/tokens.ts`, `towfast-preset.ts`, `src/styles/`
  - Implementacao: paleta Signal Grid, tipografia, foco, estados, densidade, raios e superficies via design tokens.
  - Verificacao: pagina catalogo visual passa contraste AA e nao usa overrides globais `.p-*` frágeis.
  - Dependencias: `TASK-002`

- [ ] `TASK-020` - Implementar tema escuro e ThemeService
  - Requisitos: `REQ-043`
  - Arquivos provaveis: `src/app/theme/theme.service.ts`, `towfast-preset.ts`
  - Implementacao: respeitar prefers-color-scheme, alternar por icone e persistir somente preferencia.
  - Verificacao: teste e screenshot confirmam persistencia e `AC-019`.
  - Dependencias: `TASK-019`

- [ ] `TASK-021` - Criar AppShell responsivo
  - Requisitos: `REQ-005`, `REQ-036`, `REQ-040`
  - Arquivos provaveis: `src/app/core/layout/app-shell/`, estilos de layout
  - Implementacao: trilho/topbar desktop, barra inferior mobile, area de trabalho e painel contextual.
  - Verificacao: screenshots 360/768/1280/1920 sem overlap ou scroll horizontal.
  - Dependencias: `TASK-018`, `TASK-019`

- [ ] `TASK-022` - Criar primitivas de pagina e estados
  - Requisitos: `REQ-034`, `REQ-036`, `REQ-039`, `REQ-040`
  - Arquivos provaveis: `src/app/shared/components/`
  - Implementacao: PageHeader, DataState, StatusSignal, FilterBar, EntityDrawer e confirmacoes.
  - Verificacao: testes de componentes cobrem loading, vazio, erro, permissao e retry sem salto de layout.
  - Dependencias: `TASK-019`

- [ ] `TASK-023` - Criar timeline e rotulos operacionais
  - Requisitos: `REQ-021`, `REQ-029`, `REQ-037`, `REQ-038`
  - Arquivos provaveis: `src/app/shared/components/request-timeline/`, `src/app/shared/pipes/`
  - Implementacao: mapear enums para portugues, datas locais, icones e historico sem depender apenas de cor.
  - Verificacao: testes cobrem seis estados, datas e leitores de tela.
  - Dependencias: `TASK-019`, `TASK-009`

- [ ] `TASK-024` - Implementar padroes responsivos de lista e tabela
  - Requisitos: `REQ-020`, `REQ-035`, `REQ-036`
  - Arquivos provaveis: `src/app/shared/components/responsive-data-view/`
  - Implementacao: tabela/paginacao NG-ZORRO desktop e linhas estruturadas mobile com filtros na URL.
  - Verificacao: pagina/filtros sobrevivem reload e mobile nao cria tabela espremida.
  - Dependencias: `TASK-022`

- [ ] `TASK-025` - Criar validadores e controles de formulario
  - Requisitos: `REQ-010`, `REQ-030`, `REQ-034`, `REQ-037`
  - Arquivos provaveis: `src/app/shared/forms/`
  - Implementacao: CPF/CNPJ, telefone, placa, coordenadas, UF, CNH, senha e quantidade positiva com normalizacao.
  - Verificacao: testes cobrem casos validos/invalidos e payload sem mascara.
  - Dependencias: `TASK-001`

## Autenticacao publica

- [ ] `TASK-026` - Implementar tela de login
  - Requisitos: `REQ-002`, `REQ-003`, `REQ-005`
  - Arquivos provaveis: `src/app/features/auth/login/`
  - Implementacao: formulario acessivel, estados de erro/rate limit e redirecionamento por papel.
  - Verificacao: component test e E2E validam login dos cinco papeis.
  - Dependencias: `TASK-016`, `TASK-018`, `TASK-021`, `TASK-025`

- [ ] `TASK-027` - Implementar autocadastro do cliente
  - Requisitos: `REQ-001`, `REQ-010`
  - Arquivos provaveis: `src/app/features/auth/register/`
  - Implementacao: CPF/CNPJ, contato, senha/confirmacao, envio normalizado e entrada autenticada.
  - Verificacao: E2E cobre `AC-001`, duplicidade e ProblemDetails de validacao.
  - Dependencias: `TASK-016`, `TASK-021`, `TASK-025`

- [ ] `TASK-028` - Implementar comando de logout e expiracao visual
  - Requisitos: `REQ-004`
  - Arquivos provaveis: `src/app/core/layout/user-menu/`, `src/app/features/auth/`
  - Implementacao: logout no shell, limpeza imediata e mensagem de sessao expirada.
  - Verificacao: E2E confirma revogacao, sessionStorage vazio e retorno ao login.
  - Dependencias: `TASK-016`, `TASK-021`

## Area do cliente

- [ ] `TASK-029` - Implementar inicio e perfil do cliente
  - Requisitos: `REQ-007`, `REQ-020`, `REQ-021`
  - Arquivos provaveis: `src/app/features/client/home/`, `profile/`
  - Implementacao: carregar proprio cliente, solicitacao ativa/resumo e atualizar dados permitidos.
  - Verificacao: testes garantem uso do clientId da sessao e ausencia de IDs editaveis.
  - Dependencias: `TASK-024`, `TASK-026`

- [ ] `TASK-030` - Implementar veiculos do cliente
  - Requisitos: `REQ-008`, `REQ-010`
  - Arquivos provaveis: `src/app/features/client/vehicles/`
  - Implementacao: listar, cadastrar e ativar/inativar veiculos com placa globalmente unica tratada por `409`.
  - Verificacao: component/E2E cobrem inclusao, duplicidade e mudanca de estado.
  - Dependencias: `TASK-025`, `TASK-029`

- [ ] `TASK-031` - Implementar locais salvos do cliente
  - Requisitos: `REQ-009`, `REQ-010`
  - Arquivos provaveis: `src/app/features/client/locations/`
  - Implementacao: listar e criar locais; nao renderizar editar ou excluir.
  - Verificacao: teste confirma payload normalizado e ausencia dos comandos fora de escopo.
  - Dependencias: `TASK-025`, `TASK-029`

- [ ] `TASK-032` - Implementar abertura guiada de solicitacao
  - Requisitos: `REQ-019`, `REQ-024`, `REQ-034`
  - Arquivos provaveis: `src/app/features/requests/create/`
  - Implementacao: stepper Servico/Veiculo/Local/Revisao, local salvo/inline e condicional de destino.
  - Verificacao: E2E cobre `AC-004`, `AC-005`, reboque sem destino e duplo envio.
  - Dependencias: `TASK-030`, `TASK-031`

## Solicitacoes compartilhadas

- [ ] `TASK-033` - Implementar store e listagem filtrada de solicitacoes
  - Requisitos: `REQ-020`, `REQ-024`, `REQ-035`, `REQ-036`
  - Arquivos provaveis: `src/app/features/requests/data-access/`, `list/`
  - Implementacao: filtros completos, pagina limite, ordenacao da API e query string.
  - Verificacao: testes cobrem cada filtro, reload, paginacao e escopo por papel.
  - Dependencias: `TASK-024`, `TASK-009`

- [ ] `TASK-034` - Implementar detalhe e timeline da solicitacao
  - Requisitos: `REQ-021`, `REQ-024`, `REQ-032`, `REQ-038`
  - Arquivos provaveis: `src/app/features/requests/detail/`
  - Implementacao: cliente permitido, veiculo, servico, locais, vinculos, datas, itens, history e versao.
  - Verificacao: `AC-012` confirma privacidade e nenhuma chamada cruzada amplia dados.
  - Dependencias: `TASK-023`, `TASK-033`

- [ ] `TASK-035` - Implementar cancelamento e conflitos de versao
  - Requisitos: `REQ-022`, `REQ-024`, `REQ-034`
  - Arquivos provaveis: `src/app/features/requests/actions/`
  - Implementacao: dialogo com motivo/version, permissoes visuais, refresh em `409` e feedback.
  - Verificacao: testes cobrem cliente permitido, operacao, conflito e comando indisponivel.
  - Dependencias: `TASK-034`, `TASK-022`

## Operacao e administrador

- [ ] `TASK-036` - Implementar painel operacional
  - Requisitos: `REQ-020`, `REQ-021`, `REQ-035`
  - Arquivos provaveis: `src/app/features/operation/dashboard/`
  - Implementacao: faixas por status e lista recente sem inventar agregados globais.
  - Verificacao: testes confirmam que numeros derivam somente da resposta carregada.
  - Dependencias: `TASK-033`, `TASK-021`

- [ ] `TASK-037` - Implementar manutencao operacional de clientes
  - Requisitos: `REQ-006`, `REQ-008`, `REQ-009`, `REQ-010`
  - Arquivos provaveis: `src/app/features/operation/clients/`
  - Implementacao: listar/criar/editar/estado do cliente e navegar a veiculos/locais permitidos.
  - Verificacao: E2E Admin/Operator cobre CRUD logico, duplicidades e escopo.
  - Dependencias: `TASK-024`, `TASK-025`, `TASK-026`

- [ ] `TASK-038` - Implementar manutencao operacional de prestadores
  - Requisitos: `REQ-011`, `REQ-012`
  - Arquivos provaveis: `src/app/features/operation/providers/`
  - Implementacao: listar/criar/editar/estado, coordenadas e raio, com abas de recursos.
  - Verificacao: testes cobrem coordenadas, raio invalido, estado e autorizacao.
  - Dependencias: `TASK-024`, `TASK-025`, `TASK-026`

- [ ] `TASK-039` - Implementar catalogo de tipos de servico
  - Requisitos: `REQ-018`
  - Arquivos provaveis: `src/app/features/operation/service-types/`
  - Implementacao: listar catalogo e ativar/inativar com confirmacao para Admin/Operator.
  - Verificacao: testes cobrem estado, erro e ocultacao para outros papeis.
  - Dependencias: `TASK-022`, `TASK-026`

- [ ] `TASK-040` - Implementar transicoes operacionais da solicitacao
  - Requisitos: `REQ-023`, `REQ-024`, `REQ-029`, `REQ-030`
  - Arquivos provaveis: `src/app/features/operation/requests/`, `src/app/features/requests/actions/`
  - Implementacao: en-route, start, complete e cancel conforme estado, sem despacho manual.
  - Verificacao: testes garantem proxima acao valida, itens e versao atualizada.
  - Dependencias: `TASK-034`, `TASK-035`

## Prestador

- [ ] `TASK-041` - Implementar motoristas do prestador
  - Requisitos: `REQ-013`, `REQ-014`
  - Arquivos provaveis: `src/app/features/provider/drivers/`
  - Implementacao: listar, cadastrar, editar e inativar usando providerId da sessao/operacao.
  - Verificacao: E2E cobre proprio escopo, CNH/documento duplicado e estado.
  - Dependencias: `TASK-024`, `TASK-025`, `TASK-026`

- [ ] `TASK-042` - Implementar frota do prestador
  - Requisitos: `REQ-015`, `REQ-016`
  - Arquivos provaveis: `src/app/features/provider/fleet/`
  - Implementacao: listar, cadastrar, editar e inativar veiculos de atendimento.
  - Verificacao: testes cobrem placa duplicada, capacidade e escopo.
  - Dependencias: `TASK-024`, `TASK-025`, `TASK-026`

- [ ] `TASK-043` - Implementar habilitacao de servicos do prestador
  - Requisitos: `REQ-017`
  - Arquivos provaveis: `src/app/features/provider/services/`
  - Implementacao: listar catalogo/habilitados, MultiSelect e PUT explicito do conjunto.
  - Verificacao: `AC-013` cobre salvar, remover e refletir resposta sem reload total.
  - Dependencias: `TASK-039`, `TASK-026`

- [ ] `TASK-044` - Implementar resumo operacional do prestador
  - Requisitos: `REQ-017`, `REQ-020`, `REQ-021`
  - Arquivos provaveis: `src/app/features/provider/home/`
  - Implementacao: atendimento ativo, disponibilidade e atalhos, sem KPI nao suportado.
  - Verificacao: testes cobrem livre, ocupado e sem dados.
  - Dependencias: `TASK-033`, `TASK-043`

## Motorista

- [ ] `TASK-045` - Implementar fila elegivel do motorista
  - Requisitos: `REQ-025`, `REQ-026`, `REQ-033`, `REQ-036`
  - Arquivos provaveis: `src/app/features/driver/queue/`
  - Implementacao: fila paginada, idade, servico, modelo, origem, refresh e estado prestador ocupado.
  - Verificacao: `AC-006`/`AC-007` confirmam ordenacao, privacidade e fila vazia correta.
  - Dependencias: `TASK-024`, `TASK-026`

- [ ] `TASK-046` - Implementar aceite com selecao de veiculo
  - Requisitos: `REQ-027`, `REQ-034`
  - Arquivos provaveis: `src/app/features/driver/queue/accept-dialog/`
  - Implementacao: carregar frota ativa, enviar providerVehicleId/version e bloquear duplo envio.
  - Verificacao: `AC-008` cobre sucesso e `409` concorrente atualizando a fila.
  - Dependencias: `TASK-042`, `TASK-045`

- [ ] `TASK-047` - Implementar tela dedicada de atendimento
  - Requisitos: `REQ-028`, `REQ-029`, `REQ-030`, `REQ-034`
  - Arquivos provaveis: `src/app/features/driver/active-service/`
  - Implementacao: proxima acao unica, release com motivo, estados e conclusao com itens.
  - Verificacao: `AC-009` e testes cobrem fluxo completo, saltos invalidos e versao.
  - Dependencias: `TASK-034`, `TASK-046`

- [ ] `TASK-048` - Implementar historico do motorista
  - Requisitos: `REQ-020`, `REQ-021`, `REQ-035`
  - Arquivos provaveis: `src/app/features/driver/history/`
  - Implementacao: reutilizar listagem filtrada por driverId da sessao e detalhe autorizado.
  - Verificacao: teste confirma filtro imutavel de escopo e paginacao.
  - Dependencias: `TASK-033`, `TASK-047`

## Google Maps e tracking

- [ ] `TASK-049` - Implementar loader seguro do Google Maps
  - Requisitos: `REQ-041`, `REQ-042`
  - Arquivos provaveis: `src/app/shared/maps/google-maps-loader.service.ts`, `map.port.ts`
  - Implementacao: configurar loader uma vez, importar `maps`, `places` e `geocoding` sob demanda, com timeout e erro tipado.
  - Verificacao: teste confirma lazy load, uma inicializacao e nenhuma biblioteca Routes.
  - Dependencias: `TASK-004`

- [ ] `TASK-050` - Implementar MapSurface e marcadores TowFast
  - Requisitos: `REQ-032`, `REQ-041`, `REQ-042`, `REQ-043`
  - Arquivos provaveis: `src/app/shared/maps/map-surface/`
  - Implementacao: origem, destino, motorista/stale, bounds inicial, tema e fallback textual.
  - Verificacao: component test e screenshots cobrem `AC-017`, `AC-018`, claro/escuro e dimensao nao nula.
  - Dependencias: `TASK-020`, `TASK-049`

- [ ] `TASK-051` - Integrar mapa ao prestador e detalhes de solicitacao
  - Requisitos: `REQ-012`, `REQ-021`, `REQ-032`, `REQ-041`
  - Arquivos provaveis: `src/app/features/operation/providers/`, `src/app/features/requests/detail/`
  - Implementacao: centro/raio por busca, pin ou geolocalizacao e markers da solicitacao sem calculo de rota.
  - Verificacao: testes cobrem selecao de endereco no mapa, raio e detalhe autorizado.
  - Dependencias: `TASK-034`, `TASK-038`, `TASK-050`

- [ ] `TASK-052` - Implementar polling da ultima posicao
  - Requisitos: `REQ-032`, `REQ-033`, `REQ-041`
  - Arquivos provaveis: `src/app/features/tracking/latest-position.store.ts`
  - Implementacao: polling configuravel, pausa em aba oculta, cancelamento e monotonicidade de capturedAt.
  - Verificacao: fake timers cobrem update, stale, `404`, `409`, troca de request e teardown.
  - Dependencias: `TASK-034`, `TASK-050`

- [ ] `TASK-053` - Implementar captura e publicacao GPS do motorista
  - Requisitos: `REQ-031`, `REQ-033`, `REQ-034`
  - Arquivos provaveis: `src/app/features/tracking/geolocation.port.ts`, `driver-location-publisher.service.ts`
  - Implementacao: watch unico, amostragem, UUID, ultima captura em memoria e encerramento completo.
  - Verificacao: `AC-010`/`AC-011` cobrem permissao, envio, rede, estado terminal, logout e destroy.
  - Dependencias: `TASK-047`, `TASK-052`

- [ ] `TASK-054` - Integrar mapa e tracking ao modo motorista
  - Requisitos: `REQ-029`, `REQ-031`, `REQ-032`, `REQ-041`
  - Arquivos provaveis: `src/app/features/driver/active-service/`, `src/app/features/tracking/`
  - Implementacao: exibir posicao atual/origem/destino e controlar publicacao pelos estados.
  - Verificacao: E2E com geolocation mock percorre EnRoute, InProgress e Completed sem envio posterior.
  - Dependencias: `TASK-050`, `TASK-053`

## Qualidade e entrega

- [ ] `TASK-055` - Aplicar acessibilidade WCAG 2.2 AA
  - Requisitos: `REQ-034`, `REQ-036`, `REQ-040`, `REQ-043`
  - Arquivos provaveis: toda UI, `e2e/accessibility.spec.ts`
  - Implementacao: foco, teclado, labels, live regions, contraste, reduced motion e nao depender de cor.
  - Verificacao: axe-core e percurso teclado cobrem login, abertura, aceite e conclusao conforme `AC-015`.
  - Dependencias: `TASK-027`, `TASK-032`, `TASK-047`, `TASK-050`

- [ ] `TASK-056` - Executar matriz visual e responsiva
  - Requisitos: `REQ-036`, `REQ-040`, `REQ-043`
  - Arquivos provaveis: `e2e/visual/`, snapshots Playwright
  - Implementacao: capturas claro/escuro em 360, 768, 1280 e 1920 para telas principais.
  - Verificacao: `AC-014`/`AC-019`, canvas do mapa nao vazio e nenhum overlap/corte/scroll acidental.
  - Dependencias: `TASK-055`, `TASK-054`

- [ ] `TASK-057` - Completar E2E das jornadas e erros
  - Requisitos: `REQ-001` a `REQ-043`
  - Arquivos provaveis: `e2e/auth/`, `e2e/client/`, `e2e/operation/`, `e2e/provider/`, `e2e/driver/`
  - Implementacao: automatizar `AC-001` a `AC-013`, `AC-016` a `AC-018` e falhas `401/403/409/429`.
  - Verificacao: `npm run e2e` sem retries ocultando flakiness.
  - Dependencias: `TASK-037`, `TASK-040`, `TASK-044`, `TASK-054`

- [ ] `TASK-058` - Configurar headers e politica de seguranca
  - Requisitos: `REQ-003`, `REQ-004`, `REQ-039`, `REQ-041`, `REQ-042`
  - Arquivos provaveis: configuracao de hosting, `docs/web-deployment.md`
  - Implementacao: CSP Google minima, HSTS, nosniff, referrer e permissions policy geolocation self.
  - Verificacao: smoke de producao confirma Maps/API e scanner nao encontra header bloqueante ou permissivo demais.
  - Dependencias: `TASK-049`, `TASK-053`

- [ ] `TASK-059` - Criar build e container/hosting de producao
  - Requisitos: `REQ-034`, `REQ-040`, `REQ-042`
  - Arquivos provaveis: `web/towfast-web/Dockerfile`, config do servidor estatico, scripts runtime, docs
  - Implementacao: build imutavel, fallback SPA, config no-store, assets hashed e sem source maps publicos.
  - Verificacao: container inicia com duas configs, deep link funciona e budgets passam.
  - Dependencias: `TASK-003`, `TASK-004`, `TASK-058`

- [ ] `TASK-060` - Configurar CI completo do frontend
  - Requisitos: `REQ-001` a `REQ-043`
  - Arquivos provaveis: pipeline CI, `package.json`, scripts
  - Implementacao: install lockfile, OpenAPI diff, lint, testes, build, Playwright, axe e artifacts visuais.
  - Verificacao: pipeline limpo falha deliberadamente em drift de contrato, lint, budget e regressao E2E.
  - Dependencias: `TASK-057`, `TASK-059`

- [ ] `TASK-061` - Documentar execucao, configuracao e operacao
  - Requisitos: `REQ-031`, `REQ-032`, `REQ-039`, `REQ-041`, `REQ-043`
  - Arquivos provaveis: `web/towfast-web/README.md`, `docs/web-deployment.md`
  - Implementacao: setup, runtime config, OpenAPI, Google key/referrer, temas, testes, deploy e diagnostico.
  - Verificacao: ambiente limpo executa app e testes seguindo apenas a documentacao.
  - Dependencias: `TASK-059`, `TASK-060`

- [ ] `TASK-062` - Executar revisao final de arquitetura, seguranca e UX
  - Requisitos: `REQ-001` a `REQ-043`
  - Arquivos provaveis: todo `web/towfast-web/`, spec, plan e tasks
  - Implementacao: revisar boundaries, todos endpoints, papeis, tokens, XSS/CSP, Maps, tracking e responsividade.
  - Verificacao: `npm run check && npm run e2e`; nenhum achado bloqueante e matriz `AC-001` a `AC-019` preenchida.
  - Dependencias: `TASK-055` a `TASK-061`

## Validacao final

- [ ] Todos os requisitos `REQ-001` a `REQ-043` possuem implementacao rastreavel.
- [ ] Todos os criterios `AC-001` a `AC-019` foram verificados.
- [ ] Todos os endpoints da matriz possuem facade tipada e uso observavel.
- [ ] Access token permanece em memoria e refresh token somente em `sessionStorage`.
- [ ] Refresh concorrente executa uma unica rotacao.
- [ ] Nenhum token, documento, contato ou coordenada completa aparece em logs.
- [ ] Chave Google esta restrita por dominio e Maps, Places e Geocoding APIs.
- [ ] Routes nao e carregado; Places e Geocoding sao importados somente nos seletores de endereco.
- [ ] Tracking para em estado terminal, logout e destruicao da tela.
- [ ] Temas claro e escuro atendem contraste WCAG AA.
- [ ] Layouts 360/768/1280/1920 nao possuem overlap, corte ou scroll acidental.
- [ ] Google Maps renderiza com dimensao estavel e possui fallback funcional.

## Reconciliacao de contratos e fluxos

- [x] `TASK-063` - Sincronizar e verificar contratos OpenAPI
  - Requisitos: `REQ-047`
  - Implementacao: adicionar comandos de sincronizacao/geracao e verificacao dos 30 endpoints servidos pela API.
  - Verificacao: cliente gerado compila e comparacao semantica nao aponta rotas divergentes.

- [x] `TASK-064` - Garantir destino no payload de reboque
  - Requisitos: `REQ-019`, `REQ-045`
  - Implementacao: decidir obrigatoriedade pelo codigo `Tow`, validar destino e montar o comando tipado sem depender de estado tardio.
  - Verificacao: teste cobre destino inline e local salvo.

- [x] `TASK-065` - Criar seletor reutilizavel de endereco Google
  - Requisitos: `REQ-012`, `REQ-044`
  - Implementacao: busca Places, clique no mapa, geocodificacao reversa e localizacao atual, emitindo `LocationInput`.
  - Verificacao: componentes de solicitacao, local salvo e prestador nao exibem latitude/longitude manuais.

- [x] `TASK-066` - Completar manutencao operacional de clientes e prestadores
  - Requisitos: `REQ-006`, `REQ-011`, `REQ-013`, `REQ-015`, `REQ-017`, `REQ-046`
  - Implementacao: expor criacao/edicao de prestador e links de gestao para recursos associados usando escopo selecionado.
  - Verificacao: Admin/Operator acessam cliente/prestador selecionado; Provider permanece limitado ao proprio ID.
- [x] `TASK-067` - Estabilizar polling e publicacao da localizacao
  - Requisitos: `REQ-032`, `REQ-033`, `REQ-048`
  - Implementacao: tornar erros de background silenciosos, impedir requisicoes
    concorrentes, aplicar intervalo minimo e encerrar o GPS ao fechar ou mudar de estado.
  - Verificacao: testes cobrem estados rastreaveis e normalizacao do intervalo;
    build confirma teardown e tratamento local sem tempestade de notificacoes.
- [x] TASK-068 - Iniciar GPS ao entrar em EnRoute
  - Requisitos: REQ-031, REQ-049
  - Implementacao: apos a API confirmar EnRoute, atualizar o detalhe e iniciar
    watchPosition, enviando lat/lng pelo endpoint .NET persistido no Supabase.
  - Verificacao: testes e build Angular passam; backend continua usando
    service_request_tracking no PostgreSQL sem nova dependencia de banco.
- [ ] Build de producao respeita budgets e nao publica source maps sensiveis.
- [ ] Testes unitarios, componentes, contrato, Playwright e axe estao passando.
- [ ] Code Reviewer nao deixou achados bloqueantes.
- [ ] Riscos residuais e verificacoes nao executadas foram registrados.
