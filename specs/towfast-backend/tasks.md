# Tasks: Backend TowFast - Assistencia 24 horas

## Referencias

- Spec: `specs/towfast-backend/spec.md`
- Plan: `specs/towfast-backend/plan.md`
- Requisitos: `REQ-001` a `REQ-056`
- Criterios de aceite: `AC-001` a `AC-027`

## Tarefas

### Fundacao

- [x] `TASK-001` - Criar a solution e os projetos da arquitetura hexagonal
  - Requisitos: `REQ-040`, `REQ-042`
  - Arquivos provaveis: `global.json`, `TowFast.sln`, `Directory.Build.props`, `src/TowFast.Domain/`, `src/TowFast.Application/`, `src/TowFast.Infrastructure/`, `src/TowFast.Api/`, `tests/`
  - Implementacao: fixar o SDK .NET 10, configurar todos os projetos com `TargetFramework` `net10.0`, criar referencias permitidas, nulabilidade, analyzers e configuracao comum sem dependencias invertidas.
  - Verificacao: `dotnet --version` retorna SDK 10.x e `dotnet build TowFast.sln --configuration Release` compila somente projetos `net10.0`.
  - Dependencias: nenhuma

- [x] `TASK-002` - Proteger as fronteiras com testes de arquitetura
  - Requisitos: `REQ-040`, `REQ-041`, `REQ-042`
  - Arquivos provaveis: `tests/TowFast.ArchitectureTests/`
  - Implementacao: testar que Domain nao referencia frameworks, Application nao referencia adapters e controllers nao usam `DbContext`.
  - Verificacao: `dotnet test tests/TowFast.ArchitectureTests`
  - Dependencias: `TASK-001`

- [x] `TASK-003` - Implementar primitivas, erros e value objects compartilhados
  - Requisitos: `REQ-006`, `REQ-008`, `REQ-012`, `REQ-036`, `REQ-037`
  - Arquivos provaveis: `src/TowFast.Domain/Common/`, `src/TowFast.Domain/ValueObjects/`, `tests/TowFast.Domain.Tests/ValueObjects/`
  - Implementacao: criar resultado/erro de dominio, CPF/CNPJ, email, telefone, placa, coordenada, raio e identificador publico com normalizacao e validacao.
  - Verificacao: `dotnet test tests/TowFast.Domain.Tests --filter ValueObjects`
  - Dependencias: `TASK-001`

- [x] `TASK-004` - Configurar persistencia PostgreSQL e unidade de trabalho
  - Requisitos: `REQ-039`, `REQ-041`, `REQ-042`
  - Arquivos provaveis: `src/TowFast.Infrastructure/Persistence/TowFastDbContext.cs`, `src/TowFast.Application/Abstractions/Persistence/`, `src/TowFast.Infrastructure/DependencyInjection.cs`
  - Implementacao: configurar EF Core/Npgsql, convencoes UTC, snake_case, transacoes, `IUnitOfWork` e conexao externa.
  - Verificacao: `dotnet test tests/TowFast.IntegrationTests --filter PersistenceBootstrap`
  - Dependencias: `TASK-001`, `TASK-003`

- [x] `TASK-005` - Padronizar relogio, usuario corrente e cancelamento de I/O
  - Requisitos: `REQ-034`, `REQ-042`
  - Arquivos provaveis: `src/TowFast.Application/Abstractions/`, `src/TowFast.Infrastructure/Time/`, `src/TowFast.Api/Security/`
  - Implementacao: criar `IClock`, `ICurrentUserContext`, implementacoes nos adapters e propagar `CancellationToken`.
  - Verificacao: testes unitarios comprovam tempo deterministico e escopo derivado das claims.
  - Dependencias: `TASK-001`

### Identidade e acesso

- [x] `TASK-006` - Modelar usuarios, papeis e refresh tokens
  - Requisitos: `REQ-001`, `REQ-002`, `REQ-003`
  - Arquivos provaveis: `src/TowFast.Domain/Identity/`, `src/TowFast.Application/Identity/`, `src/TowFast.Infrastructure/Persistence/Configurations/`
  - Implementacao: criar usuario, papeis, vinculos de escopo, estado ativo, refresh token, repositories e mappings.
  - Verificacao: testes de dominio validam um unico papel e vinculos obrigatorios por papel.
  - Dependencias: `TASK-003`, `TASK-004`

- [x] `TASK-007` - Criar migration de identidade e restricoes de credenciais
  - Requisitos: `REQ-001`, `REQ-002`, `REQ-039`
  - Arquivos provaveis: `src/TowFast.Infrastructure/Persistence/Migrations/`
  - Implementacao: criar `users` e `refresh_tokens`, indices unicos de email e hash de token, FKs e campos de expiracao/revogacao.
  - Verificacao: aplicar e reverter a migration em PostgreSQL descartavel.
  - Dependencias: `TASK-006`

- [x] `TASK-008` - Implementar hash de senha e emissao de JWT
  - Requisitos: `REQ-001`, `REQ-002`
  - Arquivos provaveis: `src/TowFast.Application/Abstractions/Security/`, `src/TowFast.Infrastructure/Security/`, `tests/TowFast.Application.Tests/Identity/`
  - Implementacao: implementar `IPasswordHasher` e `ITokenIssuer`, access token curto, claims de escopo e configuracoes tipadas.
  - Verificacao: testes validam senha correta/incorreta, assinatura, emissor, audiencia, expiracao e claims.
  - Dependencias: `TASK-005`, `TASK-006`

- [x] `TASK-009` - Implementar autocadastro transacional do cliente
  - Requisitos: `REQ-001`, `REQ-005`, `REQ-006`
  - Arquivos provaveis: `src/TowFast.Application/Identity/RegisterClient/`, `src/TowFast.Api/Controllers/AuthController.cs`, `tests/TowFast.Application.Tests/Identity/`
  - Implementacao: criar cliente e usuario CLIENT na mesma transacao, validar CPF/CNPJ, email unico e retornar tokens.
  - Verificacao: testes cobrem `AC-001`, `AC-002` e conflito sem persistencia parcial.
  - Dependencias: `TASK-007`, `TASK-008`

- [x] `TASK-010` - Implementar login e bloqueio de usuario inativo
  - Requisitos: `REQ-001`, `REQ-002`
  - Arquivos provaveis: `src/TowFast.Application/Identity/Login/`, `src/TowFast.Api/Controllers/AuthController.cs`
  - Implementacao: validar credenciais sem revelar qual campo falhou, verificar estado ativo e emitir access/refresh tokens.
  - Verificacao: testes cobrem login valido, senha invalida, usuario ausente e inativo conforme `AC-025`.
  - Dependencias: `TASK-008`, `TASK-009`

- [x] `TASK-011` - Implementar rotacao, revogacao e deteccao de reuso do refresh token
  - Requisitos: `REQ-001`
  - Arquivos provaveis: `src/TowFast.Application/Identity/RefreshToken/`, `src/TowFast.Application/Identity/Logout/`, `src/TowFast.Infrastructure/Security/`
  - Implementacao: armazenar somente hash, rotacionar atomicamente, revogar no logout e invalidar cadeia em tentativa de reuso.
  - Verificacao: testes de integracao concorrentes confirmam que um refresh token so pode ser rotacionado uma vez.
  - Dependencias: `TASK-007`, `TASK-008`

- [x] `TASK-012` - Configurar autenticacao Bearer e policies de autorizacao
  - Requisitos: `REQ-002`, `REQ-003`, `REQ-004`
  - Arquivos provaveis: `src/TowFast.Api/Program.cs`, `src/TowFast.Api/Security/`, `tests/TowFast.IntegrationTests/Security/`
  - Implementacao: configurar JWT Bearer, policies por papel/propriedade e validacao de usuario ativo.
  - Verificacao: testes HTTP cobrem `401`, `403`, isolamento entre clientes e escopo do prestador conforme `AC-012`.
  - Dependencias: `TASK-010`, `TASK-011`

### Clientes e rede parceira

- [x] `TASK-013` - Implementar cadastro e manutencao de clientes
  - Requisitos: `REQ-005`, `REQ-006`, `REQ-039`
  - Arquivos provaveis: `src/TowFast.Domain/Clients/`, `src/TowFast.Application/Clients/`, `src/TowFast.Api/Controllers/ClientsController.cs`
  - Implementacao: criar, consultar, atualizar, listar e inativar clientes com DTOs separados e autorizacao por escopo.
  - Verificacao: testes HTTP cobrem operacao, proprio cliente, duplicidade e exclusao logica.
  - Dependencias: `TASK-009`, `TASK-012`

- [x] `TASK-014` - Implementar veiculos de clientes e placa nao reutilizavel
  - Requisitos: `REQ-007`, `REQ-008`, `REQ-039`
  - Arquivos provaveis: `src/TowFast.Domain/Clients/ClientVehicle.cs`, `src/TowFast.Application/ClientVehicles/`, `src/TowFast.Api/Controllers/ClientVehiclesController.cs`
  - Implementacao: criar, consultar, atualizar e inativar veiculos, mantendo unicidade global da placa.
  - Verificacao: testes confirmam varios veiculos por cliente e `409` para placa ja usada, inclusive inativa.
  - Dependencias: `TASK-013`

- [x] `TASK-015` - Implementar locais salvos e snapshots imutaveis
  - Requisitos: `REQ-009`, `REQ-010`
  - Arquivos provaveis: `src/TowFast.Domain/Locations/`, `src/TowFast.Application/Locations/`, `src/TowFast.Infrastructure/Persistence/Configurations/LocationConfiguration.cs`
  - Implementacao: gerenciar locais do cliente e fornecer criacao de snapshot com coordenadas obrigatorias para origem de solicitacao.
  - Verificacao: teste cobre `AC-011`: alterar local salvo nao altera solicitacao existente.
  - Dependencias: `TASK-013`

- [x] `TASK-016` - Implementar prestadores com centro e raio de atendimento
  - Requisitos: `REQ-011`, `REQ-012`, `REQ-039`
  - Arquivos provaveis: `src/TowFast.Domain/Providers/Provider.cs`, `src/TowFast.Application/Providers/`, `src/TowFast.Api/Controllers/ProvidersController.cs`
  - Implementacao: criar, consultar, atualizar e inativar prestadores, validando centro geografico e raio positivo.
  - Verificacao: testes HTTP e de dominio cobrem dados validos, limites e autorizacao.
  - Dependencias: `TASK-012`, `TASK-013`

- [x] `TASK-017` - Implementar motoristas vinculados a um unico prestador
  - Requisitos: `REQ-014`, `REQ-015`, `REQ-017`
  - Arquivos provaveis: `src/TowFast.Domain/Providers/Driver.cs`, `src/TowFast.Application/Drivers/`, `src/TowFast.Api/Controllers/DriversController.cs`
  - Implementacao: criar e manter motorista, credencial DRIVER, CNH unica e FK obrigatoria para um prestador.
  - Verificacao: testes impedem motorista sem prestador, em dois prestadores ou com CNH duplicada.
  - Dependencias: `TASK-016`, `TASK-012`

- [x] `TASK-018` - Implementar frota de atendimento do prestador
  - Requisitos: `REQ-016`, `REQ-017`
  - Arquivos provaveis: `src/TowFast.Domain/Providers/ProviderVehicle.cs`, `src/TowFast.Application/ProviderVehicles/`, `src/TowFast.Api/Controllers/ProviderVehiclesController.cs`
  - Implementacao: criar, consultar, atualizar e inativar veiculos de atendimento com placa unica e capacidade opcional.
  - Verificacao: testes validam propriedade pelo prestador e bloqueio de veiculo inativo.
  - Dependencias: `TASK-016`

- [x] `TASK-019` - Implementar catalogo e habilitacao de servicos por prestador
  - Requisitos: `REQ-013`, `REQ-018`, `REQ-019`, `REQ-021`
  - Arquivos provaveis: `src/TowFast.Domain/Services/`, `src/TowFast.Application/ServiceTypes/`, `src/TowFast.Api/Controllers/ServiceTypesController.cs`
  - Implementacao: semear os tres servicos, listar ativos e manter relacionamento prestador-servico sem apagar historico.
  - Verificacao: migration e testes confirmam seed idempotente e bloqueio de servico inativo.
  - Dependencias: `TASK-016`

- [x] `TASK-020` - Criar migration consolidada dos cadastros
  - Requisitos: `REQ-005` a `REQ-019`, `REQ-039`
  - Arquivos provaveis: `src/TowFast.Infrastructure/Persistence/Migrations/`, `tests/TowFast.IntegrationTests/Persistence/`
  - Implementacao: criar tabelas, FKs, checks e indices unicos de clientes, veiculos, locais, prestadores, motoristas, frota e servicos.
  - Verificacao: banco vazio migra e reverte; constraints sao exercitadas em PostgreSQL real.
  - Dependencias: `TASK-013` a `TASK-019`

### Solicitacoes e operacao

- [x] `TASK-021` - Implementar agregado e maquina de estados da solicitacao
  - Requisitos: `REQ-022`, `REQ-023`, `REQ-024`, `REQ-028`, `REQ-030`, `REQ-034`
  - Arquivos provaveis: `src/TowFast.Domain/ServiceRequests/`, `tests/TowFast.Domain.Tests/ServiceRequests/`
  - Implementacao: criar agregado, enum de estados, datas, invariantes, transicoes e eventos de historico.
  - Verificacao: testes cobrem todas as transicoes validas e invalidas, `AC-005`, `AC-009` e `AC-010`.
  - Dependencias: `TASK-003`, `TASK-015`, `TASK-019`

- [x] `TASK-022` - Mapear solicitacoes, itens, atribuicoes e historico
  - Requisitos: `REQ-020`, `REQ-022`, `REQ-030`, `REQ-031`, `REQ-034`, `REQ-035`
  - Arquivos provaveis: `src/TowFast.Infrastructure/Persistence/Configurations/ServiceRequests/`, `src/TowFast.Infrastructure/Persistence/Migrations/`
  - Implementacao: criar tabelas, FKs restritas, indices de consulta, versionamento otimista e indice unico parcial por prestador ativo.
  - Verificacao: migration aplica e constraints rejeitam estado invalido e dois atendimentos ativos do mesmo prestador.
  - Dependencias: `TASK-020`, `TASK-021`

- [x] `TASK-023` - Implementar abertura de solicitacao
  - Requisitos: `REQ-022`, `REQ-023`, `REQ-024`, `REQ-030`
  - Arquivos provaveis: `src/TowFast.Application/ServiceRequests/Create/`, `src/TowFast.Api/Controllers/ServiceRequestsController.cs`
  - Implementacao: validar propriedade do veiculo, servico ativo, snapshots, coordenadas, destino e gravar `PENDING` com historico.
  - Verificacao: testes HTTP cobrem `AC-004` e `AC-005` sem persistencia parcial.
  - Dependencias: `TASK-021`, `TASK-022`

- [x] `TASK-024` - Implementar detalhe e listagem filtrada de solicitacoes
  - Requisitos: `REQ-032`, `REQ-033`, `REQ-036`, `REQ-038`
  - Arquivos provaveis: `src/TowFast.Application/ServiceRequests/Queries/`, `src/TowFast.Api/Contracts/ServiceRequests/`
  - Implementacao: criar detalhe autorizado e pagina por status, periodo, servico, cliente, prestador e motorista.
  - Verificacao: testes cobrem filtros, limite de pagina, ordenacao deterministica e isolamento conforme `AC-012`, `AC-013`.
  - Dependencias: `TASK-023`, `TASK-012`

- [x] `TASK-025` - Implementar fila elegivel por servico, raio e horario
  - Requisitos: `REQ-004`, `REQ-017`, `REQ-025`, `REQ-038`
  - Arquivos provaveis: `src/TowFast.Application/ServiceRequests/ListAvailable/`, `src/TowFast.Infrastructure/Persistence/Repositories/ServiceRequestRepository.cs`
  - Implementacao: filtrar `PENDING`, prestador/servico/estado, calcular Haversine, ordenar por abertura e omitir dados pessoais.
  - Verificacao: testes cobrem `AC-006`, `AC-007`, `AC-023`, `AC-024` e fila vazia para prestador ocupado.
  - Dependencias: `TASK-022`, `TASK-024`

- [x] `TASK-026` - Implementar aceite atomico da solicitacao
  - Requisitos: `REQ-017`, `REQ-026`, `REQ-027`, `REQ-030`, `REQ-035`
  - Arquivos provaveis: `src/TowFast.Application/ServiceRequests/Accept/`, `src/TowFast.Infrastructure/Persistence/Repositories/ServiceRequestRepository.cs`
  - Implementacao: validar elegibilidade, versao, veiculo, raio e capacidade; persistir aceite, atribuicao e historico na mesma transacao.
  - Verificacao: testes de aplicacao cobrem aceite valido e cada motivo de `403`/`409`.
  - Dependencias: `TASK-025`

- [ ] `TASK-027` - Testar concorrencia do aceite por solicitacao e prestador
  - Requisitos: `REQ-027`, `REQ-035`
  - Arquivos provaveis: `tests/TowFast.IntegrationTests/ServiceRequests/AcceptConcurrencyTests.cs`
  - Implementacao: executar requisicoes paralelas para a mesma solicitacao e para solicitacoes diferentes pelo mesmo prestador.
  - Verificacao: somente um aceite persiste e os demais retornam `409`, cobrindo `AC-022`, `AC-026` e `AC-027`.
  - Dependencias: `TASK-026`

- [x] `TASK-028` - Implementar liberacao do atendimento aceito
  - Requisitos: `REQ-028`, `REQ-030`, `REQ-034`, `REQ-035`
  - Arquivos provaveis: `src/TowFast.Application/ServiceRequests/Release/`
  - Implementacao: permitir ao motorista atribuido liberar apenas em `ACCEPTED`, exigir motivo, encerrar atribuicao e retornar a `PENDING`.
  - Verificacao: teste cobre `AC-008` e comprova liberacao da capacidade do prestador.
  - Dependencias: `TASK-026`

- [x] `TASK-029` - Implementar deslocamento e inicio do atendimento
  - Requisitos: `REQ-028`, `REQ-030`, `REQ-034`, `REQ-035`
  - Arquivos provaveis: `src/TowFast.Application/ServiceRequests/Transitions/`, `src/TowFast.Api/Controllers/ServiceRequestsController.cs`
  - Implementacao: expor transicoes `ACCEPTED -> EN_ROUTE -> IN_PROGRESS` com autorizacao, versao e historico atomico.
  - Verificacao: testes cobrem ordem correta, repeticao e motorista nao atribuido.
  - Dependencias: `TASK-026`

- [x] `TASK-030` - Implementar cancelamento sem efeito financeiro
  - Requisitos: `REQ-028`, `REQ-029`, `REQ-030`, `REQ-034`, `REQ-035`
  - Arquivos provaveis: `src/TowFast.Application/ServiceRequests/Cancel/`
  - Implementacao: aplicar permissoes por estado, exigir motivo, registrar `CANCELLED` e liberar capacidade sem criar cobranca.
  - Verificacao: testes cobrem cliente antes do atendimento, operacao durante atendimento e papel nao autorizado.
  - Dependencias: `TASK-029`

- [x] `TASK-031` - Implementar conclusao com itens utilizados
  - Requisitos: `REQ-020`, `REQ-028`, `REQ-030`, `REQ-031`, `REQ-034`, `REQ-035`
  - Arquivos provaveis: `src/TowFast.Application/ServiceRequests/Complete/`
  - Implementacao: concluir apenas `IN_PROGRESS`, validar itens e gravar resultado, itens e historico atomicamente.
  - Verificacao: teste cobre `AC-014` e confirma liberacao da capacidade do prestador.
  - Dependencias: `TASK-029`

### Rastreamento

- [x] `TASK-032` - Modelar e persistir somente a ultima posicao
  - Requisitos: `REQ-046`, `REQ-052`, `REQ-054`
  - Arquivos provaveis: `src/TowFast.Domain/Tracking/`, `src/TowFast.Infrastructure/Persistence/Configurations/ServiceRequestTrackingConfiguration.cs`, `src/TowFast.Infrastructure/Persistence/Migrations/`
  - Implementacao: criar posicao atual por solicitacao e upsert atomico condicionado a evento valido e `captured_at` mais recente.
  - Verificacao: testes PostgreSQL confirmam uma linha, idempotencia e protecao contra evento fora de ordem.
  - Dependencias: `TASK-022`

- [x] `TASK-033` - Implementar publicacao autorizada da posicao
  - Requisitos: `REQ-045`, `REQ-046`, `REQ-047`, `REQ-050`, `REQ-051`, `REQ-054`, `REQ-055`
  - Arquivos provaveis: `src/TowFast.Application/Tracking/Publish/`, `src/TowFast.Api/Controllers/TrackingController.cs`
  - Implementacao: validar motorista atribuido, estado, coordenadas, instante e evento; aceitar somente em `EN_ROUTE`/`IN_PROGRESS`.
  - Verificacao: testes cobrem `AC-016`, `AC-019`, `AC-020` e `AC-021`.
  - Dependencias: `TASK-029`, `TASK-032`

- [x] `TASK-034` - Implementar consulta autorizada da ultima posicao
  - Requisitos: `REQ-048`, `REQ-049`, `REQ-053`, `REQ-055`
  - Arquivos provaveis: `src/TowFast.Application/Tracking/GetLatest/`, `src/TowFast.Api/Controllers/TrackingController.cs`
  - Implementacao: retornar somente a ultima posicao, datas e `isStale`, respeitando cliente, operacao, prestador e motorista atribuidos.
  - Verificacao: testes com relogio falso cobrem `AC-017`, `AC-018`, ausencia de historico e acesso cruzado.
  - Dependencias: `TASK-032`, `TASK-033`

- [x] `TASK-035` - Preservar portas para futura entrega em tempo real
  - Requisitos: `REQ-056`
  - Arquivos provaveis: `src/TowFast.Application/Tracking/`, `tests/TowFast.ArchitectureTests/`
  - Implementacao: garantir que casos de uso nao dependem de polling, HTTP, SSE, WebSocket ou Supabase Realtime.
  - Verificacao: testes de arquitetura passam e nenhuma referencia de transporte existe em Domain/Application.
  - Dependencias: `TASK-034`

### API e operacao

- [x] `TASK-036` - Padronizar DTOs, validacao, paginacao e ProblemDetails
  - Requisitos: `REQ-036`, `REQ-037`, `REQ-038`
  - Arquivos provaveis: `src/TowFast.Api/Contracts/`, `src/TowFast.Api/Middleware/`, `src/TowFast.Api/Errors/`
  - Implementacao: configurar JSON camelCase, UTC, limites, validacao de requests, pagina padrao e erros com correlation ID.
  - Verificacao: testes de contrato cobrem `400`, `401`, `403`, `404`, `409` e corpo `application/problem+json`.
  - Dependencias: `TASK-012`, `TASK-024`

- [x] `TASK-037` - Configurar OpenAPI e Swagger UI com JWT Bearer
  - Requisitos: `REQ-043`
  - Arquivos provaveis: `src/TowFast.Api/Program.cs`, `src/TowFast.Api/OpenApi/`, `src/TowFast.Api/appsettings*.json`
  - Implementacao: publicar OpenAPI v1, Swagger UI em `/swagger`, esquema Authorize Bearer e chave de configuracao para producao.
  - Verificacao: teste HTTP cobre `AC-015`: `/swagger`, documento OpenAPI e security scheme carregam corretamente.
  - Dependencias: `TASK-012`, `TASK-036`

- [x] `TASK-038` - Implementar health checks
  - Requisitos: `REQ-044`
  - Arquivos provaveis: `src/TowFast.Api/Program.cs`, `src/TowFast.Infrastructure/Health/`
  - Implementacao: expor liveness do processo e readiness do PostgreSQL sem dados sensiveis.
  - Verificacao: testes cobrem resposta saudavel e falha controlada do banco conforme `AC-015`.
  - Dependencias: `TASK-004`, `TASK-036`

- [ ] `TASK-039` - Adicionar correlation ID, logs estruturados e metricas essenciais
  - Requisitos: `REQ-037`, `REQ-044`
  - Arquivos provaveis: `src/TowFast.Api/Observability/`, `src/TowFast.Api/Program.cs`
  - Implementacao: registrar duracao, resultado e IDs tecnicos; medir conflitos, fila e rastreamento sem dados pessoais.
  - Verificacao: teste captura logs/metricas e confirma ausencia de senha, token, documento e coordenada completa.
  - Dependencias: `TASK-036`, `TASK-038`

- [x] `TASK-040` - Aplicar rate limiting e endurecer configuracoes de seguranca
  - Requisitos: `REQ-001`, `REQ-002`, `REQ-037`
  - Arquivos provaveis: `src/TowFast.Api/Security/`, `src/TowFast.Api/Program.cs`, `src/TowFast.Api/appsettings.json`
  - Implementacao: limitar register/login/refresh, validar opcoes no startup, exigir TLS fora do local e evitar segredos versionados.
  - Verificacao: testes confirmam `429`, falha de startup com JWT invalido e ausencia de segredos no repositorio.
  - Dependencias: `TASK-011`, `TASK-036`

### Integracao e entrega

- [ ] `TASK-041` - Automatizar PostgreSQL descartavel para testes de integracao
  - Requisitos: `REQ-039`, `REQ-041`
  - Arquivos provaveis: `tests/TowFast.IntegrationTests/Fixtures/`, `tests/TowFast.IntegrationTests/TowFastWebApplicationFactory.cs`
  - Implementacao: subir banco compativel, aplicar migrations e isolar dados entre testes sem depender do Supabase remoto.
  - Verificacao: `dotnet test tests/TowFast.IntegrationTests --filter DatabaseFixture`
  - Dependencias: `TASK-004`, `TASK-022`, `TASK-032`

- [x] `TASK-042` - Criar bootstrap seguro do primeiro administrador
  - Requisitos: `REQ-001`, `REQ-002`
  - Arquivos provaveis: `src/TowFast.Api/Bootstrap/`, `src/TowFast.Infrastructure/Identity/`
  - Implementacao: criar administrador idempotente somente por processo controlado e segredos do ambiente, sem endpoint publico.
  - Verificacao: duas execucoes criam um unico administrador e nenhuma credencial aparece em log.
  - Dependencias: `TASK-007`, `TASK-008`, `TASK-041`

- [ ] `TASK-043` - Criar smoke tests do fluxo principal
  - Requisitos: `REQ-001` a `REQ-056`
  - Arquivos provaveis: `tests/TowFast.IntegrationTests/EndToEnd/`, `http/TowFast.Api.http`
  - Implementacao: automatizar cadastro, login, cadastros, solicitacao, fila, aceite, deslocamento, tracking, inicio e conclusao.
  - Verificacao: executar fluxo completo e confirmar contratos, estados, historico e autorizacao.
  - Dependencias: `TASK-013` a `TASK-042`

- [ ] `TASK-044` - Validar todos os criterios de aceite automatizaveis
  - Requisitos: `REQ-001` a `REQ-056`
  - Arquivos provaveis: `tests/TowFast.Domain.Tests/`, `tests/TowFast.Application.Tests/`, `tests/TowFast.IntegrationTests/`
  - Implementacao: mapear `AC-001` a `AC-027` para testes nomeados e preencher lacunas de cobertura.
  - Verificacao: relatorio mostra ao menos um teste ou evidencia objetiva para cada criterio.
  - Dependencias: `TASK-043`

- [x] `TASK-045` - Validar migrations e configuracao no Supabase de homologacao
  - Requisitos: `REQ-039`, `REQ-043`, `REQ-044`
  - Arquivos provaveis: `README.md`, `docs/deployment.md`, configuracao externa de homologacao
  - Implementacao: aplicar migrations, executar health, abrir Swagger UI e rodar smoke tests sem registrar segredos.
  - Verificacao: evidencias de migration, `/health`, `/swagger` e fluxo principal em homologacao.
  - Dependencias: `TASK-037`, `TASK-038`, `TASK-043`

- [x] `TASK-046` - Documentar execucao, configuracao e operacao do backend
  - Requisitos: `REQ-043`, `REQ-044`, `REQ-055`
  - Arquivos provaveis: `README.md`, `docs/configuration.md`, `docs/operations.md`
  - Implementacao: documentar setup local, variaveis, migrations, Swagger, bootstrap admin, tracking e diagnostico sem valores secretos.
  - Verificacao: um ambiente limpo consegue executar build, testes, migration e API seguindo apenas a documentacao.
  - Dependencias: `TASK-045`

- [ ] `TASK-047` - Executar revisao final de arquitetura, seguranca e codigo
  - Requisitos: `REQ-001` a `REQ-056`
  - Arquivos provaveis: todo o backend e testes
  - Implementacao: revisar limites hexagonais, autorizacao, concorrencia, migrations, exposicao de dados, OpenAPI e riscos residuais.
  - Verificacao: `dotnet build TowFast.sln --configuration Release` e `dotnet test TowFast.sln --configuration Release`; nenhum achado bloqueante aberto.
  - Dependencias: `TASK-044`, `TASK-046`

## Validacao final

- [ ] Todos os requisitos `REQ-001` a `REQ-056` possuem implementacao e teste rastreavel.
- [ ] Todos os criterios `AC-001` a `AC-027` foram verificados.
- [x] Build Release e testes automatizados relevantes estao passando.
- [x] Migrations foram validadas desde um banco vazio e em homologacao.
- [x] Swagger UI funciona em `/swagger` com autenticacao JWT Bearer.
- [x] Health checks nao expoem dados sensiveis.
- [ ] Aceites concorrentes nao duplicam solicitacao nem capacidade do prestador.
- [x] O cliente recebe somente a ultima posicao autorizada, sem historico de rota.
- [ ] Code Reviewer nao deixou achados bloqueantes.
- [ ] Riscos residuais e verificacoes nao executadas foram registrados.

## Correcao de concorrencia e dependentes do agregado

- [x] `TASK-048` - Corrigir persistencia das transicoes de atendimento
  - Requisitos: `REQ-057`
  - Arquivos provaveis: `TowFastDbContext.cs`, `ServiceRequestService.cs`, testes de persistencia e middleware.
  - Implementacao: marcar GUIDs gerados no dominio como nao gerados pelo banco, diferenciar concorrencia de outras falhas e atualizar a lista no frontend apos conflitos.
  - Verificacao: `AC-028`, testes de aceite/cancelamento recarregados, suite .NET e frontend verdes.
  - Dependencias: `TASK-021`, `TASK-022`, `TASK-032`
