# Plan: Plataforma Web TowFast

## Referencia

- Spec: `specs/towfast-web/spec.md`
- Requisitos cobertos: `REQ-001` a `REQ-043`
- Criterios cobertos: `AC-001` a `AC-019`
- API consumida: `TowFast.Api`, contrato OpenAPI v1 em
  `/swagger/v1/swagger.json`
- Estado do repositorio: backend existente; frontend Angular ainda nao criado.

## Abordagem

Construir uma SPA client-side em Angular 22, sem SSR, pois o produto e autenticado,
operacional e nao depende de indexacao. O frontend ficara em `web/towfast-web/`,
separado dos projetos .NET, compartilhando apenas o contrato OpenAPI versionado.

A aplicacao usara componentes standalone, rotas lazy por area de negocio, Angular
Signals para estado de tela/sessao e RxJS para HTTP, refresh serializado, polling e
geolocalizacao. NG-ZORRO fornecera os componentes base, enquanto os tokens TowFast
definirao cores, superficies, densidade, cantos e temas claro/escuro.

O produto sera uma unica aplicacao com quatro shells autenticados:

```text
PublicShell
  /login
  /cadastro

ClientShell
  /app/inicio
  /app/solicitacoes
  /app/veiculos
  /app/locais
  /app/perfil

OperationsShell
  /ops/painel
  /ops/solicitacoes
  /ops/clientes
  /ops/prestadores
  /ops/catalogo

PartnerShell
  /partner/resumo
  /partner/motoristas
  /partner/frota
  /partner/servicos

DriverShell
  /driver/fila
  /driver/atendimento
  /driver/historico
```

`Admin` e `Operator` usam o shell operacional, com comandos adicionais apenas onde
a API autoriza. `Provider`, `Driver` e `Client` usam escopos derivados da resposta
de autenticacao; IDs nunca serao digitados manualmente pelo usuario.

## Estrutura do projeto

```text
web/towfast-web/
  public/
    config/config.template.json
  scripts/
    generate-runtime-config.mjs
    sync-openapi.mjs
  src/
    app/
      api/
        generated/
        api-facade/
      core/
        auth/
        config/
        errors/
        guards/
        http/
        layout/
        observability/
      features/
        auth/
        client/
        operation/
        provider/
        driver/
        requests/
        tracking/
      shared/
        components/
        forms/
        maps/
        pipes/
        utils/
      theme/
        towfast-preset.ts
        tokens.ts
        theme.service.ts
      app.config.ts
      app.routes.ts
    styles/
      reset.css
      foundations.css
      layout.css
    index.html
    main.ts
  e2e/
  openapi/towfast-v1.json
  angular.json
  package.json
  tsconfig.json
```

## Componentes afetados

| Camada | Componente | Mudanca |
|---|---|---|
| Bootstrap | Angular workspace | Criar app standalone strict, scripts, lint, testes e budgets |
| Config | Runtime config | API URL, Google Maps key, Map ID, polling e flags por ambiente |
| API adapter | OpenAPI client | Gerar tipos e services para todos os endpoints v1 |
| Core | Session store | Access token em memoria, refresh em `sessionStorage` e usuario atual |
| Core | HTTP pipeline | Bearer, correlation ID, refresh mutex e ProblemDetails |
| Core | Authorization | Guards e menus por papel/escopo |
| Theme | TowFast tokens | Integracao NG-ZORRO claro/escuro e fundamentos Signal Grid |
| Shared | UI primitives | Page header, status signal, empty/error/loading e confirmacoes |
| Shared | Maps adapter | Loader Google Maps, mapa, markers e estados de falha |
| Feature | Auth | Login, autocadastro, refresh e logout |
| Feature | Client | Perfil, veiculos, locais, abertura e acompanhamento |
| Feature | Operation | Painel, solicitacoes, clientes, prestadores e catalogo |
| Feature | Provider | Motoristas, frota e servicos habilitados |
| Feature | Driver | Fila, aceite, atendimento, itens e geolocalizacao |
| Test | Unit/component | Regras de apresentacao, formularios, sessao e mapas |
| Test | E2E | Jornadas completas por papel e falhas HTTP |
| Delivery | Static hosting | Build imutavel, runtime config, CSP, cache e fallback SPA |

## Portas e adapters

### Portas de entrada

- Rotas Angular e componentes de pagina.
- Formularios reativos tipados.
- Eventos NG-ZORRO de tabela, paginacao, dialogo e comandos.
- Eventos do navegador para tema, conectividade e visibilidade.
- Geolocation API do navegador, somente no modo motorista.

### Portas de saida

- `TowFastApiPort`: facade sobre o cliente OpenAPI.
- `SessionStoragePort`: refresh token e preferencia de sessao.
- `RuntimeConfigPort`: configuracao carregada antes do bootstrap.
- `MapPort`: inicializacao, markers, bounds, tema e destruicao do Google Maps.
- `AddressPicker`: busca Places, clique no mapa, localizacao atual e geocodificacao
  reversa para produzir um `LocationInput` completo.
- `GeolocationPort`: watch, normalizacao e encerramento da captura.
- `TelemetryPort`: eventos tecnicos sem dados pessoais.
- `ClockPort` e `UuidPort`: tempo e `sourceEventId`, substituiveis em testes.

### Adapters

- Angular `HttpClient` para a TowFast API.
- `sessionStorage` para refresh token; access token permanece apenas em signal.
- `@googlemaps/js-api-loader` para carregamento lazy da Maps JavaScript API.
- `navigator.geolocation.watchPosition` para tracking do motorista.
- `crypto.randomUUID()` para correlation IDs e eventos de posicao.
- `matchMedia('(prefers-color-scheme: dark)')` e `localStorage` somente para tema.

## Configuracao de runtime

A aplicacao deve buscar `/config/config.json` antes de executar
`bootstrapApplication`. O arquivo sera criado no deploy a partir de variaveis de
ambiente e nao contera segredo de backend.

```json
{
  "apiBaseUrl": "https://api.towfast.example",
  "googleMapsApiKey": "browser-key-restricted-by-referrer",
  "googleMapsMapId": "optional-map-id",
  "trackingPublishIntervalMs": 10000,
  "trackingQueryIntervalMs": 10000,
  "environmentName": "production"
}
```

- `apiBaseUrl` e obrigatoria e nao deve terminar com `/`.
- `googleMapsApiKey` e publica por natureza no navegador, mas deve usar chave
  exclusiva, restrita por HTTP referrer e somente a Maps JavaScript API.
- `googleMapsMapId` e opcional; sem ele o mapa usa estilo padrao adaptado ao tema.
- Intervalos devem possuir limites defensivos no frontend para evitar polling
  acidentalmente agressivo.
- Producoes diferentes devem reutilizar o mesmo bundle e variar somente o arquivo
  runtime.
- Servidor deve aplicar `Cache-Control: no-store` ao `config.json` e cache imutavel
  aos assets com hash.

## Contrato OpenAPI e API facade

### Sincronizacao

1. Exportar o documento OpenAPI da API em execucao para
   `openapi/towfast-v1.json`.
2. Validar JSON e falhar se o documento nao possuir o security scheme `Bearer`.
3. Gerar cliente TypeScript Angular em `src/app/api/generated/` por ferramenta
   mantida e compativel com Angular 22, fixada no lockfile.
4. Nunca editar arquivos gerados manualmente.
5. Expor uma facade pequena por dominio para esconder detalhes do gerador e tornar
   componentes testaveis.
6. CI deve regenerar e falhar quando houver diff nao versionado no contrato.

### Facades previstas

- `AuthApi`: register, login, refresh e logout.
- `ClientsApi`: clientes, veiculos e locais.
- `ProvidersApi`: prestadores, motoristas, frota e habilitacoes.
- `ServiceTypesApi`: catalogo e estado ativo.
- `RequestsApi`: criar, listar, detalhar e transicionar solicitacoes.
- `TrackingApi`: publicar e consultar ultima posicao.
- `HealthApi`: liveness/readiness apenas para diagnostico autorizado da interface.

As facades nao duplicam regras de negocio. Elas convertem parametros de pagina,
datas e filtros, e traduzem respostas geradas para view models somente quando a
apresentacao exigir.

## Sessao, autenticacao e autorizacao

### Estado de sessao

```text
SessionState
  status: anonymous | authenticating | authenticated | refreshing
  accessToken: memoria
  accessExpiresAt
  refreshToken: sessionStorage
  refreshExpiresAt
  user: id, email, role, clientId, providerId, driverId
```

- Login e registro populam o store e redirecionam conforme papel.
- Recarregar a pagina recupera somente o refresh token, executa refresh e mantem o
  shell bloqueado por skeleton ate determinar a sessao.
- Logout chama a API quando houver refresh token, mas sempre limpa o estado local
  mesmo se a rede falhar.
- Nenhum token deve aparecer em URL, analytics, logs, toast ou `localStorage`.

### Refresh serializado

O interceptor deve implementar single-flight:

1. Uma resposta `401` elegivel inicia refresh quando nao houver outro em curso.
2. Requisicoes seguintes aguardam o mesmo observable compartilhado.
3. A chamada original de leitura pode ser repetida uma unica vez.
4. Mutacoes nao idempotentes so sao repetidas quando nenhuma resposta de negocio
   foi recebida e o erro foi exclusivamente de autenticacao.
5. Falha no refresh limpa a sessao e navega ao login.
6. O endpoint de refresh nunca passa pelo proprio mecanismo de retry.

### Guards e menus

- `authGuard`: requer sessao confirmada.
- `anonymousGuard`: impede login/cadastro quando ja autenticado.
- `roleGuard`: valida papeis aceitos pela rota.
- `scopeGuard`: exige `clientId`, `providerId` ou `driverId` quando necessario.
- Menus derivam da mesma declaracao tipada das rotas para evitar divergencia.
- Guards melhoram UX; respostas `403/404` da API continuam definitivas.

## Tema e design system

### Tokens TowFast

Criar tokens semanticos integrados ao NG-ZORRO, mantendo sobrescritas globais
pequenas e estaveis. Definir escalas para:

- superficies, texto, borda e foco;
- primaria ciano, sucesso verde, atencao amarelo e perigo coral;
- estados de solicitacao com cor, icone e rotulo, nunca somente cor;
- altura de campos, densidade de tabelas e dimensoes estaveis de botoes;
- raios entre 4 e 8 px;
- tema claro e escuro com contraste WCAG AA.

### Primitivas compartilhadas

- `AppShell`: trilho desktop, topbar e navegacao mobile.
- `PageHeader`: titulo compacto, contexto e comandos.
- `StatusSignal`: estado, horario e icone.
- `RequestTimeline`: historico imutavel da solicitacao.
- `DataState`: loading, vazio, erro, sem permissao e retry.
- `FilterBar`: filtros sincronizados com query string.
- `ConfirmActionDialog`: motivo, versao e confirmacao de mutacoes sensiveis.
- `EntityDrawer`: criacao/edicao contextual sem cards aninhados.
- `MapSurface`: mapa full-bleed dentro da area de trabalho, com fallback textual.
- `ThemeToggle`: botao por icone, tooltip e persistencia da preferencia.

### Responsividade

- `>= 1200 px`: trilho lateral, conteudo e painel contextual opcional.
- `768-1199 px`: trilho compacto e detalhe em drawer.
- `< 768 px`: barra inferior, filtros em drawer e comandos fixos seguros.
- Tabelas viram listas de linhas estruturadas no mobile; nao usar scroll horizontal
  como solucao principal.
- Mapas usam `aspect-ratio`/altura minima e nunca dependem do conteudo para ter
  dimensao.

## Features e fluxos

### Autenticacao

- Login com email/senha, feedback inline e rate limit.
- Autocadastro com CPF/CNPJ, telefone, senha e confirmacao.
- Redirecionamento por papel e retorno seguro para rota originalmente solicitada.
- Sem telas de esqueci/trocar senha ou gestao de usuarios administrativos.

### Cliente

- Inicio prioriza solicitacao ativa e botao `Solicitar assistencia`.
- Perfil usa `GET/PATCH /clients` respeitando o proprio `clientId`.
- Veiculos suportam listar, criar e alterar somente estado ativo.
- Locais suportam listar e criar; nao mostrar comandos de editar/excluir.
- Nova solicitacao usa stepper compacto:
  `Servico -> Veiculo -> Local -> Revisao`.
- Reboque habilita destino; bateria/pneu nao exibem campos tecnicos ou destino.
- Detalhe combina status, timeline, dados permitidos e Google Maps quando tracking
  estiver ativo.

### Operacao e administrador

- Painel apresenta faixas por status e lista recente; metricas sao derivadas apenas
  das paginas carregadas e nao devem fingir agregados globais inexistentes.
- Solicitacoes usam filtros na URL, tabela paginada e detalhe em painel contextual.
- Operacao pode transicionar/cancelar conforme API, mas nao escolher motorista.
- Clientes: listar, criar, atualizar, ativar/inativar e abrir recursos relacionados.
- Prestadores: listar, criar, atualizar, centro/raio no mapa, estado e abas para
  motoristas, frota e servicos.
- Catalogo: listar ativos e controlar estado quando autorizado.
- Readiness pode aparecer apenas em tela tecnica para Admin, sem detalhes da string
  de conexao.

### Prestador

- Resumo mostra disponibilidade inferida do atendimento ativo retornado pela API.
- Motoristas e frota usam listas responsivas, drawer de criacao e manutencao.
- Servicos usam selecao multipla com salvamento explicito por `PUT`.
- O providerId vem da sessao; a URL nao concede acesso a outro prestador.

### Motorista

- Fila exibe idade da solicitacao, servico, modelo e origem, sem contato/documento.
- Atualizacao manual e polling moderado devem manter ordenacao recebida da API.
- Aceite abre dialogo de selecao de veiculo ativo e envia `version`.
- Atendimento ativo e uma tela dedicada com mapa, linha de estado e um comando
  principal por vez.
- Liberacao exige motivo e so aparece em `Accepted`.
- `EnRoute`, `Start` e `Complete` usam a versao corrente e atualizam o store com a
  resposta.
- Conclusao possui editor de itens repetiveis com quantidade positiva.

## Google Maps

### Loader e seguranca

- `GoogleMapsLoaderService` chama `setOptions` uma unica vez e importa a biblioteca
  `maps` apenas quando `MapSurface` entra em uso.
- O mesmo loader importa `places` e `geocoding` sob demanda nos formularios de
  endereco, evitando campos manuais de coordenadas.
- Nao carregar Routes ou outras bibliotecas fora do escopo.
- Chave exclusiva do navegador, restrita no Google Cloud a dominios de producao,
  `localhost` de desenvolvimento e Maps JavaScript API.
- Nao enviar chave para a TowFast API nem registra-la em telemetria.

### Renderizacao

- Origem: pin ciano com rotulo acessivel.
- Destino: pin amarelo somente em reboque.
- Motorista: marcador direcional verde; coral/cinza quando `isStale`.
- `fitBounds` apenas na carga inicial ou por acao `Enquadrar`; polling nao deve
  alterar zoom/centro escolhido pelo usuario.
- Mudanca de tema atualiza o Map ID/configuracao suportada sem recriar markers.
- Sem calculo de rota viaria ou historico de trajeto; autocomplete e geocodificacao sao usados apenas na selecao de enderecos.
- Falha do loader mostra endereco e ultima captura em fallback textual.

### Centro e raio do prestador

O formulario do prestador exibira mapa para buscar ou selecionar o centro de atendimento
e configurar o raio. Places, clique no mapa, localizacao atual e geocodificacao reversa
geram as coordenadas sem exigir digitacao manual.

## Tracking e polling

### Publicacao pelo motorista

1. Ativar somente em `EnRoute` ou `InProgress`.
2. Solicitar permissao de GPS por acao explicita do usuario.
3. Manter um unico `watchPosition` ativo.
4. Aplicar amostragem pelo intervalo runtime sem criar fila ilimitada.
5. Criar `sourceEventId` novo para cada captura enviada.
6. Nao reenviar automaticamente `400`, `403` ou `409`.
7. Em perda de rede, manter apenas a captura mais recente em memoria; nao persistir
   historico local.
8. Encerrar watcher em estado terminal, logout, destruicao da pagina ou perda de
   escopo.

### Consulta da ultima posicao

- Polling somente enquanto detalhe visivel, usuario autorizado e estado ativo.
- Pausar com `document.visibilityState !== 'visible'` e retomar com leitura imediata.
- Cancelar requisicao anterior ao trocar de solicitacao.
- Atualizar marker apenas quando `capturedAt` for mais recente.
- `404` sem posicao vira estado vazio; `409` por tracking encerrado para polling e
  mantem a timeline/dados textuais.

## Formularios e validacao

- Criar validadores reutilizaveis para CPF/CNPJ, telefone, placa, coordenadas, UF,
  data de CNH, senha e quantidade positiva.
- Mascaras nunca alteram o valor normalizado enviado a API.
- Erros locais aparecem apos blur/submit; erros da API sao associados ao campo
  quando houver informacao suficiente.
- Formularios mantem rascunho apenas em memoria. Nenhum documento, senha, token ou
  coordenada e persistido em armazenamento local.
- Acoes destrutivas/inativacoes exigem confirmacao clara e apresentam impacto.

## Erros e observabilidade

### Modelo de erro

Normalizar toda falha para:

```text
AppError
  status
  title
  detail
  correlationId
  fieldErrors
  retryable
```

- `400`: campos e resumo do formulario.
- `401`: refresh single-flight ou logout.
- `403`: pagina/comando sem permissao.
- `404`: recurso/posicao ausente.
- `409`: modal de conflito com recarga dos dados.
- `429`: mensagem temporaria, sem retry automatico agressivo.
- `5xx/network`: fallback preservando navegacao e retry manual.

### Correlation ID

- Gerar `X-Correlation-ID` por requisicao quando ausente.
- Capturar o ID retornado pela API/ProblemDetails.
- Exibir em detalhe tecnico copiavel, nao como mensagem primaria.
- Telemetria nao inclui access/refresh token, senha, documento, contato ou
  coordenadas completas.

## Seguranca

- CSP minima: `default-src 'self'`; liberar explicitamente hosts necessarios do
  Google Maps em `script-src`, `img-src`, `style-src`, `font-src` e `connect-src`.
- Nao usar `bypassSecurityTrust*` para conteudo da API.
- `sessionStorage` guarda apenas refresh token e metadados minimos de expiracao.
- Access token em memoria e removido no unload/fim da sessao.
- Formularios de senha desabilitam autocomplete inadequado, sem bloquear gerenciador
  de senhas no login.
- Proteger contra open redirect aceitando return URLs internas conhecidas.
- Desabilitar source maps publicos em producao ou publica-los somente no servico de
  observabilidade.
- A chave Google Maps nao e segredo, mas deve ser restrita; nenhuma chave de servidor
  entra no bundle.

## Persistencia frontend

Nao existe banco local. Persistencia permitida:

- `sessionStorage`: refresh token e expiracao da sessao.
- `localStorage`: somente tema claro/escuro.
- URL: pagina, filtros, ordenacao e IDs publicos permitidos.
- Memoria: access token, dados da API, formularios, GPS e captura pendente.

Service Worker, IndexedDB e cache offline ficam fora do MVP.

## Estrategia de testes

### Unitarios

- Session store, redirecionamento por papel e limpeza de logout.
- Interceptor Bearer, correlation ID e refresh single-flight.
- Guards de autenticacao, papel e escopo.
- Mapeamento de ProblemDetails e enums.
- Validadores e normalizacao de formularios.
- Regras de comandos visiveis por papel/status.
- Tracking: inicio, amostragem, captura mais recente e teardown.
- Theme service e preferencia inicial.

### Componentes

- Login/cadastro, stepper da solicitacao e campos condicionais.
- Tabela/lista responsiva e filtros na URL.
- Timeline, status signal e detalhe da solicitacao.
- Dialogos de aceite, motivo, cancelamento e conclusao com itens.
- Estados loading, vazio, erro, `403`, `409` e `429`.
- MapSurface com adapter falso, markers, stale e fallback.

### Contrato e integracao

- Regenerar cliente pelo OpenAPI e detectar diff.
- Mock server derivado do contrato para respostas felizes e ProblemDetails.
- Testar todos os metodos das seis facades.
- Validar que nenhum endpoint da matriz ficou sem chamada tipada.

### E2E Playwright

- Autocadastro e login de cliente.
- Veiculo, local, abertura de reboque e servicos sem destino.
- Listagem/detalhe/cancelamento do cliente.
- Operacao: clientes, prestadores e catalogo.
- Prestador: motorista, frota e servicos.
- Motorista: fila, aceite, conflito `409`, estados, GPS simulado e conclusao.
- Refresh concorrente, logout e sessao expirada.
- Google Maps carregado e fallback por falha do loader.
- Temas claro/escuro e screenshots em 360, 768, 1280 e 1920 px.
- Axe-core nos fluxos principais.

## Build, CI e entrega

Pipeline minimo:

1. Instalar Node compativel com Angular 22 e dependencias por lockfile.
2. Validar contrato OpenAPI e gerar cliente.
3. Executar format/lint.
4. Executar testes unitarios e de componentes.
5. Compilar producao com budgets.
6. Servir bundle e executar Playwright/axe.
7. Publicar assets estaticos com hashes.
8. Gerar `config.json` por ambiente durante deploy.
9. Aplicar headers CSP, HSTS, `X-Content-Type-Options`, `Referrer-Policy` e
   `Permissions-Policy` permitindo geolocalizacao apenas para a propria origem.
10. Executar smoke test de login, API, Maps e fallback SPA.

Servidor estatico deve redirecionar rotas desconhecidas para `index.html`, exceto
assets e `/config/config.json`. O frontend e a API devem compartilhar origens
permitidas por CORS definidas explicitamente no backend/deploy, sem wildcard em
producao.

## Sequencia de entrega

1. Criar workspace Angular strict, scripts, lint, testes, runtime config e CI base.
2. Sincronizar OpenAPI, gerar cliente e implementar facades para todos endpoints.
3. Implementar sessao, interceptors, refresh single-flight, guards e shells vazios.
4. Criar tokens TowFast, temas claro/escuro e primitivas Signal Grid responsivas.
5. Entregar login, autocadastro, logout e roteamento por papel.
6. Entregar area cliente: perfil, veiculos, locais e abertura de solicitacao.
7. Entregar listagem/detalhe/timeline/cancelamento compartilhados.
8. Entregar operacao: solicitacoes, clientes, prestadores e catalogo.
9. Entregar prestador: motoristas, frota e habilitacao de servicos.
10. Entregar motorista: fila, aceite, liberacao, estados, itens e conclusao.
11. Integrar Google Maps, polling da ultima posicao e publicacao GPS.
12. Reconciliar OpenAPI, completar navegacao operacional para clientes/prestadores
    e substituir coordenadas manuais pelo seletor de endereco.
13. Serializar polling e publicacao GPS, limitar sua frequencia, tratar falhas de
    background sem alertas repetidos e encerrar watches fora do atendimento ativo.
14. Fechar acessibilidade, responsividade, visual regression e budgets.
15. Executar E2E completo com a TowFast API e preparar deploy de producao.

Cada incremento deve terminar com rota navegavel, testes do comportamento entregue
e nenhuma dependencia em tela futura para ser demonstravel.

## Riscos e mitigacoes

| Risco | Impacto | Mitigacao |
|---|---|---|
| NG-ZORRO desalinhado ao Angular 22 | Bloqueio ou incompatibilidade | Fixar NG-ZORRO 22 e validar peer dependencies no bootstrap |
| Refresh em `sessionStorage` exposto a XSS | Sequestro de sessao | CSP forte, sem HTML dinamico, dependencias auditadas e sessao limitada ao tab |
| Varias respostas `401` reutilizam refresh token | Logout/revogacao da cadeia | Single-flight central e teste concorrente obrigatorio |
| OpenAPI muda sem atualizar frontend | Erros de runtime | Snapshot versionado, geracao no CI e facades isolando o gerador |
| Google Maps indisponivel ou sem quota | Tela de atendimento degradada | Loader lazy, fallback textual e nenhum comando dependente do mapa |
| Chave Google usada fora do produto | Custo indevido | Restricao por referrer/API, quota e alertas Google Cloud |
| Polling excessivo | Custo, bateria e carga na API | Intervalos runtime, pause em aba oculta, teardown e uma requisicao por vez |
| Tempestade de erros do GPS | Interface lenta ou bloqueada por notificacoes | Contexto HTTP silencioso para background, erro local unico e parada automatica |
| GPS impreciso/negado | Posicao desatualizada | Exibir accuracy/stale, retry explicito e fluxo operacional independente |
| API nao oferece agregados de dashboard | Numeros enganosos | Exibir apenas dados das paginas carregadas e nao inventar KPIs globais |
| Sem endpoints para editar local/senha/usuarios | Controles sem efeito | Nao renderizar essas acoes e manter explicitamente fora do escopo |
| Layout de mapa instavel | Mapa branco ou sobreposicao | Dimensoes CSS estaveis, ResizeObserver e testes visuais reais |
| Dados pessoais em logs | Incidente de privacidade | Redacao centralizada, allowlist de campos tecnicos e testes de telemetria |

## Decisoes e alternativas

- Decisao: SPA CSR sem SSR.
  Motivo: produto autenticado e operacional; SSR aumentaria complexidade sem ganho
  relevante de SEO.
- Decisao: uma aplicacao com shells lazy por papel.
  Motivo: compartilha contratos e componentes sem entregar bundles desnecessarios
  na primeira navegacao.
- Decisao: Angular Signals e RxJS, sem store global adicional no MVP.
  Motivo: estado e fluxos possuem escopo claro; adicionar NgRx agora aumentaria
  cerimonia sem necessidade comprovada.
- Decisao: NG-ZORRO customizado por tokens TowFast.
  Motivo: preserva acessibilidade e compatibilidade dos componentes sem aparencia de
  tema pronto nem CSS fragil.
- Decisao: cliente OpenAPI gerado atras de facades.
  Motivo: cobertura integral e deteccao de mudancas sem acoplar componentes ao
  gerador.
- Decisao: Google Maps JavaScript API com Places e Geocoding.
  Motivo: atende mapas, busca e conversao de enderecos em coordenadas sem incluir
  calculo de rotas.
- Decisao: access token em memoria e refresh em `sessionStorage`.
  Motivo: decisao confirmada para o MVP e isolamento por aba, aceitando o risco XSS
  mitigado por CSP e disciplina de dependencias.
- Decisao: runtime config externo ao bundle.
  Motivo: promove a mesma build entre ambientes e evita recompilar para trocar API,
  chave Google ou intervalos.
- Decisao: API existente permanece inalterada.
  Motivo: a interface se limita aos contratos disponiveis e nao apresenta acoes sem
  endpoint correspondente.
