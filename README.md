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

1. A administracao configura os precos em `PATCH /api/v1/service-types/{id}/pricing` ou na aba **Servicos** da interface.
2. O cliente se cadastra em `POST /api/v1/auth/register` e cadastra veiculo e locais reutilizaveis.
3. O cliente consulta `POST /api/v1/service-requests/quote`, visualiza o preco e confirma em `POST /api/v1/service-requests` com `expectedPrice`.
4. A API recalcula e grava o preco do chamado. O motorista consulta a fila em ordem de abertura, com o valor exibido.
5. O motorista aceita a solicitacao. O banco impede tanto aceite duplicado quanto dois atendimentos ativos pelo mesmo prestador.
6. O atendimento percorre `Accepted -> EnRoute -> InProgress -> Completed`, ou os fluxos permitidos de liberacao/cancelamento.
7. Durante EnRoute ou InProgress, o motorista publica posicoes. Cliente, prestador vinculado, motorista e operacao consultam somente a ultima posicao autorizada.

## Precos

- Troca de bateria e troca de pneu: preco fixo configurado por servico.
- Reboque: tarifa base mais valor por km multiplicado pela distancia aproximada em linha reta entre origem e destino. A distancia usa Haversine e e mostrada como estimativa; o calculo nao usa trajeto pelas ruas.
- Tarifas iniciais: reboque R$ 100,00 de base mais R$ 3,00/km; troca de bateria R$ 150,00; troca de pneu R$ 80,00. O administrador pode alterar esses valores na aba **Servicos**.
- O preco confirmado fica gravado no chamado e permanece igual quando a tarifa do catalogo muda. Chamados anteriores a esta funcionalidade exibem **Preco nao registrado**.

Exemplos de requisicoes estao em [http/TowFast.Api.http](http/TowFast.Api.http).

## Publicar gratuitamente

A arquitetura publicada usa GitHub Pages para a SPA, Render para a API .NET e o
PostgreSQL do Supabase ja configurado para o projeto. O frontend nao se conecta ao
banco diretamente.

### API no Render

1. Importe este repositorio como um Blueprint no [Render](https://render.com/).
   O `render.yaml` cria o servico `towfast-api` no plano Free e usa o Dockerfile
   da raiz.
2. Quando o Blueprint solicitar valores, informe a connection string do Supabase,
   email do primeiro administrador e uma senha forte. O segredo JWT e gerado pelo
   Render automaticamente.
3. No Supabase, abra **Connect** e copie a conexao direta se a rede do servico
   aceitar IPv6. Para IPv4, escolha **Session pooler**, porta `5432`; o usuario
   desse modo inclui o project ref (`postgres.<project-ref>`). Mantenha SSL
   habilitado. Nao use o pooler em modo Transaction com Entity Framework.
4. Na primeira inicializacao, `Database__AutoMigrate=true` aplica migrations
   pendentes e cria o administrador inicial. Confira `https://<api>/health` e, com
   a base inicializada, altere `Database__AutoMigrate` para `false` no Render.
5. Copie a URL HTTPS criada pelo Render para usa-la na configuracao do Pages.

### Frontend no GitHub Pages

1. No repositorio, abra **Settings > Pages** e escolha **GitHub Actions** como
   fonte de publicacao. No plano GitHub Free, Pages requer repositorio publico;
   mantenha privado apenas se a conta tiver um plano que suporte Pages privado.
2. Em **Settings > Secrets and variables > Actions > Variables**, crie
   `API_BASE_URL` com a URL HTTPS da API, sem barra no final. Opcionalmente, crie
   `GOOGLE_MAPS_API_KEY` e `GOOGLE_MAPS_MAP_ID` para habilitar mapas. A chave Maps
   e publica no navegador e deve ser limitada ao referrer
   `https://guisousa5.github.io/TowFast/*` e somente as APIs Maps, Places e
   Geocoding usadas pelo frontend.
3. Execute **Actions > Deploy GitHub Pages > Run workflow**. Pushes futuros em
   `main` que alterarem o frontend publicam automaticamente em
   `https://guisousa5.github.io/TowFast/`.

O plano gratuito do Render suspende a API depois de 15 minutos sem trafego. A
primeira requisicao seguinte pode levar cerca de um minuto para acordar. E adequado
para demonstracao e validacao, nao para disponibilidade continua.

## Decisoes de privacidade

A fila do motorista expoe identificador publico, servico, modelo do veiculo, origem operacional e preco do atendimento. Senhas, tokens, CPF/CNPJ e coordenadas completas nao devem ser registrados em logs. O sistema nao mantem historico de rota e nao implementa cobranca por cancelamento.
