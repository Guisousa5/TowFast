# TowFast Web

SPA operacional em Angular 22 e NG-ZORRO para clientes, administradores, operadores,
prestadores e motoristas da TowFast.

## Requisitos

- Node.js 24+
- pnpm 11+
- API TowFast disponível em `http://127.0.0.1:5211` para desenvolvimento

## Executar

```bash
pnpm install --frozen-lockfile
pnpm start
```

A aplicação abre em `http://127.0.0.1:4200`. O proxy local encaminha `/api` para a
API .NET sem exigir alteração de CORS no backend.

## Configuração de runtime

O arquivo `public/config.json` é lido antes do bootstrap. Nenhuma connection string,
chave JWT ou credencial Supabase pertence a esse arquivo.

| Campo | Uso |
| --- | --- |
| `apiBaseUrl` | Origem pública da API. Vazio usa a mesma origem/proxy. |
| `googleMapsApiKey` | Chave de navegador da Maps JavaScript API. |
| `googleMapsMapId` | Map ID opcional para estilo do mapa. |
| `trackingIntervalMs` | Intervalo de leitura/publicação; padrão `10000`. |

A chave Google Maps aparece no navegador por definição. Restrinja-a no Google Cloud
por HTTP referrer e às APIs Maps JavaScript, Places e Geocoding. Não habilite Routes
neste MVP. O NG-ZORRO usa licença MIT e não requer chave de licença.

## Segurança da sessão

- Access token: apenas em memória.
- Refresh token: `sessionStorage`, conforme decisão da spec.
- Renovação: serializada para impedir rotações concorrentes.
- Logout: revoga o refresh token e limpa a sessão local.
- Erros: exibem o `correlationId` retornado pela API sem registrar tokens ou dados pessoais.

## Qualidade

```bash
pnpm run test:ci
pnpm run build
pnpm run check
```

O cliente em `src/app/api/generated` foi gerado mecanicamente de
`openapi/towfast-v1.json`. Ao mudar o contrato, atualize o snapshot e gere novamente
antes de adaptar a facade.

## Container

```bash
docker build -t towfast-web .
docker run --rm -p 8080:80 \
  -e API_BASE_URL=https://api.exemplo.com \
  -e GOOGLE_MAPS_API_KEY=chave-de-navegador \
  -e GOOGLE_MAPS_MAP_ID=map-id \
  -e TRACKING_INTERVAL_MS=10000 \
  towfast-web
```

O Nginx aplica fallback da SPA, `config.json` sem cache, CSP mínima para Google Maps,
proteção contra framing, `nosniff`, referrer restrito e permissão de geolocalização
somente para a própria origem. Em produção, sirva exclusivamente por HTTPS e aplique
HSTS no proxy ou load balancer que termina TLS.
