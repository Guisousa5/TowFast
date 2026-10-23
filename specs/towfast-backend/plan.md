# Plan: Backend TowFast - Assistencia 24 horas

## Referencia

- Spec: `specs/towfast-backend/spec.md`
- Requisitos cobertos: `REQ-001` a `REQ-056`
- Criterios cobertos: `AC-001` a `AC-027`
- Estado do repositorio: greenfield para o backend; ainda nao existe solution .NET.

## Abordagem

Construir uma ASP.NET Core 10 Web API em .NET 10 (`net10.0`), modular e implantavel,
organizada por arquitetura hexagonal. O dominio concentra estados, invariantes e
regras puras; a aplicacao orquestra casos de uso e define portas; a infraestrutura
implementa PostgreSQL, JWT, hash de senha e relogio; a API expoe os adapters HTTP e
faz a composicao por injecao de dependencia.

A solution sera dividida nos projetos abaixo:

```text
TowFast.sln
src/
  TowFast.Domain/
  TowFast.Application/
  TowFast.Infrastructure/
  TowFast.Api/
tests/
  TowFast.Domain.Tests/
  TowFast.Application.Tests/
  TowFast.IntegrationTests/
  TowFast.ArchitectureTests/
```

O SDK 10 sera fixado por `global.json` com roll-forward apenas para patches
compativeis. Todos os projetos usarao `TargetFramework` `net10.0`; pacotes de EF Core,
ASP.NET Core e Npgsql devem ser selecionados em versoes compativeis com .NET 10. Nao
havera multi-targeting no MVP.

Dependencias permitidas:

```text
Infrastructure -> Application -> Domain
Api -> Application
Api -> Infrastructure (somente bootstrap)
```

- `Domain` nao referencia frameworks, banco ou HTTP.
- `Application` referencia apenas `Domain` e contratos essenciais.
- `Infrastructure` implementa as portas da aplicacao com EF Core/Npgsql e servicos
  tecnicos.
- `Api` referencia `Application` e usa `Infrastructure` apenas no bootstrap.
- A entrega sera uma aplicacao modular, sem microsservicos, mensageria, CQRS ou
  cache distribuido no MVP.

## Componentes afetados

| Camada | Componente | Mudanca |
|---|---|---|
| Domain | Identidade | Papeis, estado ativo e vinculos com cliente, prestador e motorista |
| Domain | Cliente | Cliente CPF/CNPJ, veiculos e regras de unicidade conceitual |
| Domain | Rede parceira | Prestador, raio, servicos, motoristas e frota |
| Domain | Solicitacao | Agregado, estados, transicoes, aceite, liberacao e cancelamento |
| Domain | Rastreamento | Posicao atual, validacao geografica e ordem de captura |
| Application | Autenticacao | Registro, login, refresh rotativo e logout |
| Application | Cadastros | Casos de uso para clientes, locais, prestadores, motoristas e veiculos |
| Application | Solicitacoes | Criacao, fila, aceite, transicoes, consulta e historico |
| Application | Tracking | Publicacao e consulta da ultima posicao |
| Infrastructure | Persistencia | `TowFastDbContext`, mappings, repositories e migrations PostgreSQL |
| Infrastructure | Seguranca | Emissao JWT, hash de senha, refresh token e usuario corrente |
| Infrastructure | Geolocalizacao | Filtro de cobertura por distancia no adapter PostgreSQL |
| Api | Endpoints | Controllers REST em `/api/v1`, DTOs e autorizacao por politica |
| Api | Erros | `ProblemDetails`, validacao e mapeamento de excecoes |
| Api | Operacao | OpenAPI, Swagger UI, health checks, logs e correlation ID |
| Bootstrap | Composicao | Configuracoes tipadas e registros de dependencia |

## Portas e adapters

### Portas de entrada

- `IRegisterClientUseCase`, `ILoginUseCase`, `IRefreshTokenUseCase` e
  `ILogoutUseCase`.
- Casos de uso de manutencao de cliente, veiculo, local, prestador, motorista,
  veiculo de atendimento e tipos de servico.
- `ICreateServiceRequestUseCase`, `IListAvailableRequestsUseCase`,
  `IAcceptServiceRequestUseCase`, `IReleaseServiceRequestUseCase`,
  `ITransitionServiceRequestUseCase`, `ICancelServiceRequestUseCase` e
  `ICompleteServiceRequestUseCase`.
- `IPublishDriverLocationUseCase` e `IGetLatestDriverLocationUseCase`.

As portas de entrada podem ser representadas por handlers ou services de aplicacao.
Nao criar uma interface por operacao quando uma classe coesa puder atender um grupo
pequeno de casos de uso sem misturar responsabilidades.

### Portas de saida

- `IUserRepository` e `IRefreshTokenRepository`.
- `IClientRepository`, `IClientVehicleRepository` e `ILocationRepository`.
- `IProviderRepository` e `IServiceTypeRepository`.
- `IServiceRequestRepository` para agregado, fila, vinculos e historico.
- `ILocationTrackingRepository` para upsert e leitura da ultima posicao.
- `IPasswordHasher`, `ITokenIssuer`, `IUnitOfWork`, `IClock` e
  `ICurrentUserContext`.

### Adapters de entrada

- Controllers ASP.NET Core com rotas versionadas e DTOs imutaveis.
- Autenticacao Bearer e policies por papel e propriedade do recurso.
- Middleware ou handler global para `ProblemDetails` e correlation ID.
- OpenAPI como contrato de integracao e Swagger UI em `/swagger` com autenticacao
  JWT Bearer para exploracao e teste manual dos endpoints.

### Adapters de saida

- EF Core com provider Npgsql para PostgreSQL hospedado no Supabase.
- `PasswordHasher` baseado no mecanismo seguro disponibilizado pelo ASP.NET Core;
  o dominio conhece apenas `IPasswordHasher`.
- Emissor JWT assinado com chave externa ao repositorio.
- Calculo de distancia no adapter PostgreSQL por expressao Haversine parametrizada.
  PostGIS fica como evolucao caso volume e indices geoespaciais se tornem necessarios.

## Modelo e regras

### Agregados e tipos principais

- `User`: credencial, papel, vinculo de escopo e estado ativo.
- `Client`: tipo e numero de documento, nome, contato e estado.
- `ClientVehicle`: placa normalizada e nao reutilizavel, fabricante e modelo.
- `Location`: endereco e coordenadas; a solicitacao usa snapshot imutavel.
- `Provider`: identificacao, centro geografico, raio e estado.
- `Driver`: pertence a exatamente um prestador.
- `ProviderVehicle`: pertence a exatamente um prestador.
- `ServiceType`: `TOW`, `BATTERY_REPLACEMENT` ou `TIRE_CHANGE`.
- `ServiceRequest`: raiz do agregado operacional.
- `ServiceRequestAssignment`: historico de aceite e liberacao.
- `ServiceRequestStatusHistory`: trilha imutavel de estados.
- `ServiceRequestTracking`: ultima posicao conhecida por solicitacao.

### Value objects

- `CpfCnpj`, `EmailAddress`, `PhoneNumber`, `LicensePlate` e `PublicRequestId`.
- `GeoCoordinate` valida latitude e longitude.
- `ServiceRadius` valida valor maior que zero.
- `CapturedPosition` contem evento, coordenada, instante e dados opcionais.

A normalizacao e validacao sintatica acontecem na criacao do value object. A
unicidade continua protegida por restricoes no PostgreSQL.

### Estados e invariantes

- Transicoes permitidas:
  `PENDING -> ACCEPTED -> EN_ROUTE -> IN_PROGRESS -> COMPLETED`.
- `PENDING -> CANCELLED`, estados ativos para `CANCELLED` e
  `ACCEPTED -> PENDING` por liberacao sao caminhos alternativos.
- Reboque exige destino; bateria e pneu nao exigem campos tecnicos adicionais.
- O motorista, o veiculo de atendimento e o prestador devem estar ativos e
  pertencer ao mesmo escopo no aceite.
- O prestador deve oferecer o servico e a origem deve estar dentro de seu raio.
- Um prestador pode ter no maximo uma solicitacao em `ACCEPTED`, `EN_ROUTE` ou
  `IN_PROGRESS`, compartilhada por todos os seus motoristas.
- A fila contem apenas `PENDING`, e ordenada por `created_at ASC, public_id ASC` e
  fica vazia quando o prestador esta ocupado.
- Somente o motorista atribuido publica posicao em `EN_ROUTE` ou `IN_PROGRESS`.
- Uma posicao duplicada ou mais antiga nao substitui a ultima posicao persistida.
- Conclusao e cancelamento encerram o compartilhamento de novas localizacoes.
- Cada transicao grava historico na mesma transacao da alteracao do agregado.

### Concorrencia do aceite

O aceite sera executado em uma unica transacao:

1. Validar usuario, motorista, prestador, veiculo e solicitacao.
2. Confirmar que a solicitacao permanece `PENDING` pela coluna `version`.
3. Confirmar servico e distancia de cobertura.
4. Definir prestador, motorista, veiculo, `ACCEPTED` e datas.
5. Inserir atribuicao e historico.
6. Persistir e confirmar a transacao.

Um indice unico parcial por `provider_id` nos estados ativos sera a protecao final
contra dois aceites simultaneos do mesmo prestador. Violacao da versao da
solicitacao ou desse indice deve ser traduzida para `409 Conflict`, sem expor SQL.

## API e contratos

### Convencoes

- Base `/api/v1`, JSON camelCase e datas ISO 8601 em UTC.
- IDs internos UUID; solicitacoes sao expostas preferencialmente por `publicId`.
- Listagens recebem `page`, `pageSize` limitado, filtros permitidos e ordenacao
  deterministica.
- Erros seguem `application/problem+json`, com `type`, `title`, `status`, `detail`,
  `instance`, `correlationId` e `errors` para campos invalidos.
- `400` para entrada invalida, `401` sem autenticacao, `403` sem permissao, `404`
  sem recurso acessivel e `409` para unicidade, estado ou concorrencia.
- DTOs HTTP nao sao entidades do EF nem objetos do dominio.

### Autenticacao

| Rota | Request principal | Resultado |
|---|---|---|
| `POST /api/v1/auth/register` | nome, CPF/CNPJ, email, telefone e senha | `201`, cliente e tokens |
| `POST /api/v1/auth/login` | email e senha | `200`, access token e refresh token |
| `POST /api/v1/auth/refresh` | refresh token | `200`, par rotacionado |
| `POST /api/v1/auth/logout` | refresh token | `204`, token revogado |

O access token inclui `sub`, papel e identificadores de escopo necessarios. A API
confirma que o usuario permanece ativo durante a validacao do token. Refresh tokens
sao armazenados somente por hash, possuem expiracao, rotacao e deteccao de reuso.

### Cadastros

- `GET/POST/PATCH /api/v1/clients` conforme o papel; o cliente acessa apenas a si.
- `/clients/{id}/vehicles` gerencia veiculos e impede placa repetida.
- `/clients/{id}/locations` gerencia locais reutilizaveis.
- `/providers` mantem parceiro, centro e raio de atendimento.
- `/providers/{id}/drivers` e `/providers/{id}/vehicles` respeitam o prestador.
- `/providers/{id}/service-types` mantem a habilitacao dos servicos.
- `GET /api/v1/service-types` retorna somente o catalogo ativo.

Criacoes retornam `201 Created` com `Location`; atualizacoes retornam `200` com o
recurso ou `204` quando nao houver representacao util. Inativacao usa `PATCH` e nao
apaga registros com historico.

### Solicitacoes

| Rota | Contrato essencial | Resultado esperado |
|---|---|---|
| `POST /service-requests` | veiculo, servico, origem, destino opcional e notas | `201`, `PENDING` |
| `GET /service-requests` | filtros e paginacao | pagina conforme escopo |
| `GET /service-requests/available` | paginacao | fila elegivel ou vazia |
| `GET /service-requests/{publicId}` | identificador publico | detalhe autorizado |
| `POST /service-requests/{id}/accept` | `providerVehicleId` e versao esperada | `200`, `ACCEPTED` |
| `POST /service-requests/{id}/release` | motivo e versao | `200`, `PENDING` |
| `POST /service-requests/{id}/en-route` | versao | `200`, `EN_ROUTE` |
| `POST /service-requests/{id}/start` | versao | `200`, `IN_PROGRESS` |
| `POST /service-requests/{id}/complete` | notas, itens e versao | `200`, `COMPLETED` |
| `POST /service-requests/{id}/cancel` | motivo e versao | `200`, `CANCELLED` |

Antes do aceite, o DTO da fila inclui servico, modelo do veiculo e origem, mas nao
inclui telefone, email, documento ou outros dados pessoais do cliente.

### Rastreamento

| Rota | Contrato essencial | Resultado esperado |
|---|---|---|
| `POST /service-requests/{id}/tracking/positions` | evento, coordenadas, `capturedAt` e dados opcionais | `202` ou `200` idempotente |
| `GET /service-requests/{id}/tracking/latest` | solicitacao autorizada | coordenadas, datas e `isStale` |

O POST usa upsert atomico condicionado a `capturedAt` mais recente. O GET nao expoe
historico e calcula `isStale` com limite configuravel, inicialmente 30 segundos.

## Interface web

Uma interface web completa esta fora do MVP. Swagger UI deve ser configurado em
`/swagger`, habilitado por padrao em desenvolvimento e homologacao e controlado por
configuracao em producao. A interface deve carregar o documento OpenAPI da versao
`v1`, exibir o esquema Bearer e permitir informar um JWT para chamar endpoints
protegidos. O futuro frontend HTML/CSS/JS ou aplicativo movel consumira a mesma API
e nao acessara o Supabase diretamente.

## Persistencia e migracao

### Estrutura

Implementar as tabelas definidas na spec:

- `users`, `refresh_tokens` e `clients`.
- `client_vehicles` e `locations`.
- `providers`, `drivers`, `provider_vehicles`, `service_types` e
  `provider_service_types`.
- `service_requests`, `service_request_items`, `service_request_status_history` e
  `service_request_assignments`.
- `service_request_tracking` com uma linha por solicitacao.

### Restricoes e indices

- Unicos normalizados para email, documento, CNH e placas.
- Check constraints para coordenadas, raio, quantidades e valores enumerados.
- FKs com exclusao restrita onde existir historico.
- Indices para filtros de solicitacao por status, abertura, cliente, prestador,
  motorista, servico e periodo.
- Indice para a fila iniciando por `status, created_at, public_id`.
- Indice unico parcial em `service_requests(provider_id)` onde status esteja em
  `ACCEPTED`, `EN_ROUTE` ou `IN_PROGRESS`.
- PK de `service_request_tracking` em `service_request_id` e unicidade do evento
  atual.
- `version` configurada como concurrency token e incrementada em alteracoes.

### Migrations e dados iniciais

1. Criar schema e extensoes estritamente necessarias.
2. Criar identidade e cadastros base.
3. Criar rede de atendimento e catalogo.
4. Criar solicitacoes, historicos e restricoes de concorrencia.
5. Criar rastreamento atual.
6. Inserir de forma idempotente os tres tipos de servico.

O primeiro administrador sera criado por processo controlado de bootstrap usando
segredos do ambiente, nunca por endpoint publico ou credencial versionada.

Cada migration deve ter `Down` viavel durante desenvolvimento. Em producao, rollback
de aplicacao deve manter compatibilidade com a migration anterior; migracoes
destrutivas exigem estrategia expandir-migrar-contrair e backup do Supabase.

## Seguranca e observabilidade

### Seguranca

- Chave JWT, emissor, audiencia, duracoes e conexao ficam em configuracao externa.
- Senhas e refresh tokens nunca sao registrados nem persistidos em texto puro.
- Aplicar rate limiting especialmente em registro, login e refresh.
- Validar tamanho, formato e limites de todos os DTOs na borda HTTP.
- Policies verificam papel e propriedade; filtros enviados pelo cliente nunca
  substituem o escopo derivado das claims.
- Nao retornar existencia de recurso de outro cliente ou prestador.
- Logs omitem senha, tokens, CPF/CNPJ, telefone e coordenadas completas.
- A API usa uma credencial PostgreSQL de menor privilegio; nenhuma chave Supabase
  destinada a backend e exposta aos consumidores.
- TLS e obrigatorio fora do ambiente local.

### Observabilidade

- Correlation ID aceito ou gerado por requisicao e devolvido na resposta.
- Logs estruturados registram endpoint, resultado, duracao, papel e IDs tecnicos.
- Metricas: latencia, taxa de erros, logins falhos, conflitos de aceite, tamanho da
  fila e posicoes rejeitadas/desatualizadas.
- Health checks separados para processo e PostgreSQL, sem detalhes sensiveis.
- A exposicao do Swagger UI em producao depende de configuracao explicita; essa
  configuracao nao pode desabilitar a geracao do documento usado na validacao.
- Auditoria funcional fica no historico de status e atribuicoes; logs nao substituem
  registros de dominio.

## Estrategia de testes

### Dominio

- Value objects: CPF/CNPJ, email, placa, coordenada e raio.
- Todas as transicoes validas e invalidas de `ServiceRequest`.
- Destino obrigatorio apenas para reboque.
- Regras de liberacao, cancelamento e conclusao.
- Posicao mais antiga ou duplicada nao substitui a atual.

### Aplicacao

- Autocadastro, login, refresh rotativo, logout e usuario inativo.
- Autorizacao e isolamento por cliente/prestador com portas simuladas.
- Fila por servico, raio, ordem de abertura e prestador ocupado.
- Aceite, liberacao, avancos, cancelamento e conclusao com transacao simulada.
- Publicacao autorizada e calculo de `isStale` usando `IClock` falso.

### Integracao

- Subir PostgreSQL descartavel compativel com a versao do Supabase.
- Aplicar migrations desde banco vazio e validar dados iniciais.
- Testar constraints, FKs, normalizacao e exclusao logica.
- Disparar aceites concorrentes para a mesma solicitacao.
- Disparar aceites concorrentes de solicitacoes diferentes pelo mesmo prestador;
  apenas um deve persistir.
- Validar query Haversine nos limites dentro, sobre e fora do raio.
- Validar upsert concorrente da ultima posicao.
- Testar endpoints com `WebApplicationFactory` e JWT real de teste.

### Arquitetura

- `Domain` nao depende de EF Core, ASP.NET Core ou Infrastructure.
- `Application` nao depende de adapters.
- Controllers nao acessam `DbContext` diretamente.
- DTOs de API e entidades de persistencia nao vazam para o dominio.

### Ponta a ponta

- Cobrir os fluxos de `AC-001` a `AC-027` em cenarios automatizados prioritarios.
- Manter uma colecao HTTP ou arquivo `.http` para smoke test local, sem segredos.
- Validar documento OpenAPI, carregamento de `/swagger`, esquema JWT Bearer, health
  check e fluxo completo de uma solicitacao.

## Sequencia de entrega

1. Criar solution, projetos, referencias, analyzers, nulabilidade, configuracao e
   testes de arquitetura.
2. Implementar tipos compartilhados, erros de dominio, relogio e persistencia base.
3. Implementar identidade, migration inicial, JWT, autocadastro e sessao.
4. Implementar clientes, veiculos, locais e respectivas autorizacoes.
5. Implementar prestadores, motoristas, frota, servicos, centro e raio.
6. Implementar agregado da solicitacao, estados, snapshots e historicos.
7. Implementar abertura, consulta geral e fila elegivel ordenada.
8. Implementar aceite e liberacao atomicos com concorrencia por solicitacao e por
   prestador.
9. Implementar deslocamento, inicio, cancelamento e conclusao com itens.
10. Implementar publicacao e consulta da ultima posicao.
11. Completar OpenAPI, Swagger UI em `/swagger`, esquema JWT Bearer,
    `ProblemDetails`, rate limiting, logs, metricas e health.
12. Executar testes completos, revisar seguranca, validar migrations no Supabase de
    homologacao e documentar configuracao de execucao.

Cada incremento deve terminar com migrations aplicaveis, testes verdes e contratos
OpenAPI atualizados antes do proximo incremento.

## Riscos e mitigacoes

| Risco | Impacto | Mitigacao |
|---|---|---|
| Dois aceites pelo mesmo prestador | Viola capacidade operacional | Transacao, indice unico parcial e teste concorrente |
| Dois motoristas aceitam a mesma solicitacao | Atendimento duplicado | Concurrency token, update condicional e `409` |
| JWT ou refresh token comprometido | Acesso indevido | Chave externa, expiracao curta, hash, rotacao e revogacao |
| Vazamento de dados na fila | Exposicao de cliente | DTO dedicado e testes de contrato/autorizacao |
| Calculo de raio incorreto | Fila inelegivel | Haversine no adapter e testes de fronteira |
| Coordenadas ausentes na origem | Impossibilidade de elegibilidade | Validacao antes de criar a solicitacao |
| Evento GPS fora de ordem | Posicao regressiva | Upsert condicionado a `captured_at` |
| Acoplamento ao Supabase | Dificulta evolucao | Usar PostgreSQL por Npgsql atras de portas |
| Migration incompativel | Indisponibilidade ou perda de dados | Homologacao, backup e mudancas expandir-migrar-contrair |
| Consultas lentas com crescimento | Atraso na fila e operacao | Indices, paginacao, metricas e plano futuro para PostGIS |
| Inativacao nao refletida no JWT | Usuario bloqueado continua ativo | Validacao de usuario ativo e access token curto |

## Decisoes e alternativas

- Decisao: .NET 10 e ASP.NET Core 10 com `TargetFramework` `net10.0` em toda a solution.
  Alternativa descartada: multi-targeting ou outras versoes, para manter uma
  plataforma unica e previsivel no MVP.
- Decisao: monolito modular com arquitetura hexagonal.
  Alternativa descartada: microsservicos, por custo e complexidade sem necessidade
  no MVP.
- Decisao: EF Core/Npgsql e migrations versionadas.
  Alternativa descartada: acesso direto ao Supabase por SDK no dominio, por criar
  acoplamento de infraestrutura.
- Decisao: JWT proprio com refresh token rotativo armazenado por hash.
  Alternativa descartada: Supabase Auth, conforme decisao da spec.
- Decisao: indice unico parcial como garantia de um atendimento ativo por prestador.
  Alternativa descartada: apenas consulta previa, vulneravel a corrida concorrente.
- Decisao: Haversine no adapter PostgreSQL para o volume inicial.
  Alternativa futura: PostGIS quando houver necessidade comprovada de escala e
  indices geoespaciais.
- Decisao: polling REST configuravel e somente a ultima posicao.
  Alternativas futuras: SSE, WebSocket ou Supabase Realtime atras das mesmas portas.
- Decisao: sem interface web operacional neste ciclo.
  Alternativa futura: frontend HTML/CSS/JavaScript consumindo os contratos REST.

## Correcao das transicoes persistidas

- Configurar como `ValueGeneratedNever` as chaves GUID das entidades cujo identificador
  e criado no dominio. Isso impede que dependentes novos encontrados nas colecoes do
  agregado sejam classificados pelo EF Core como registros existentes.
- Cobrir pelo menos `ServiceRequestStatusHistory`, `ServiceRequestAssignment` e
  `ServiceRequestItem`, preservando `Version` como token de concorrencia da raiz.
- Traduzir `DbUpdateConcurrencyException` para conflito de versao e violacao de indice
  de capacidade para conflito de capacidade, sem capturar genericamente todo erro de update.
- Adicionar regressao de persistencia que salva, limpa o tracking, recarrega o agregado
  e executa aceite e cancelamento.
