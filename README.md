# TowFast API

Backend REST para uma operacao de assistencia 24 horas. Clientes abrem solicitacoes de reboque, troca de bateria ou troca de pneu; motoristas parceiros visualizam a fila elegivel e aceitam o atendimento diretamente. Cada prestador pode manter somente um atendimento em andamento.

## Stack e arquitetura

- .NET 10 / ASP.NET Core Web API
- PostgreSQL compativel com Supabase via Entity Framework Core e Npgsql
- Arquitetura hexagonal: `Domain` e `Application` no centro; `Infrastructure` e `Api` como adaptadores
- JWT emitido pela propria API, com refresh token rotativo armazenado somente por hash
- Swagger UI em `/swagger`
- Rastreamento com somente a ultima posicao persistida

## Configuracao

Use variaveis de ambiente ou User Secrets. Nunca versione os valores reais.

```powershell
$env:ConnectionStrings__TowFast = "Host=db.PROJECT.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=SECRET;SSL Mode=Require;Trust Server Certificate=true"
$env:Jwt__SigningKey = "uma-chave-aleatoria-com-pelo-menos-32-bytes"
$env:Database__AutoMigrate = "true"
$env:BootstrapAdmin__Email = "admin@towfast.local"
$env:BootstrapAdmin__Password = "UmaSenhaForte1"
```

`Database__AutoMigrate=true` aplica migrations e cria o primeiro administrador de forma idempotente. Em producao, execute migrations em uma etapa controlada e mantenha `AutoMigrate=false` durante o funcionamento normal.

Configuracoes de tracking:

- `Tracking__PublishIntervalSeconds`: recomendacao do intervalo do cliente, padrao 10 segundos.
- `Tracking__QueryIntervalSeconds`: recomendacao do polling da ultima posicao, padrao 10 segundos.
- `Tracking__StaleAfterSeconds`: tempo para marcar a posicao como desatualizada, padrao 30 segundos.
- `Tracking__MaxSampleAgeSeconds`: idade maxima aceita de uma amostra, padrao 3600 segundos.
- `Tracking__FutureToleranceSeconds`: tolerancia para relogio adiantado, padrao 60 segundos.
- `Pagination__MaxPageSize`: limite maximo das paginas operacionais, padrao 100.

## Executar

```powershell
dotnet tool restore
dotnet restore TowFast.slnx
dotnet tool run dotnet-ef database update --project src/TowFast.Infrastructure --startup-project src/TowFast.Api
dotnet run --project src/TowFast.Api
```

Abra `http://localhost:5211/swagger`. Use `POST /api/v1/auth/login` e informe o access token no botao **Authorize** como Bearer token.

## Validacao

```powershell
dotnet build TowFast.slnx --configuration Release
dotnet test TowFast.slnx --configuration Release
```

Endpoints de saude:

- `GET /health/live`: processo da API.
- GET /health: conexao com PostgreSQL.

## Fluxo principal

1. O cliente se cadastra em `POST /api/v1/auth/register`.
2. Cadastra veiculo e, opcionalmente, locais reutilizaveis.
3. Abre uma solicitacao em `POST /api/v1/service-requests`.
4. O motorista consulta `GET /api/v1/service-requests/available`, em ordem de abertura e dentro do raio do prestador.
5. O motorista aceita a solicitacao. O banco impede tanto aceite duplicado quanto dois atendimentos ativos pelo mesmo prestador.
6. O atendimento percorre `Accepted -> EnRoute -> InProgress -> Completed`, ou os fluxos permitidos de liberacao/cancelamento.
7. Durante EnRoute ou InProgress, o motorista publica posicoes. Cliente, prestador vinculado, motorista e operacao consultam somente a ultima posicao autorizada.

Exemplos de requisicoes estao em [http/TowFast.Api.http](http/TowFast.Api.http).

## Decisoes de privacidade

A fila do motorista expoe somente identificador publico, servico, modelo do veiculo e origem operacional. Senhas, tokens, CPF/CNPJ e coordenadas completas nao devem ser registrados em logs. O sistema nao mantem historico de rota e nao implementa cobranca por cancelamento.
