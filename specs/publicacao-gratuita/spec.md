# Spec: Publicacao gratuita do TowFast

## Status

Aprovada

## Contexto e problema

O TowFast combina uma SPA Angular, uma API ASP.NET Core e PostgreSQL no Supabase.
O frontend ainda assume origem `/`, depende de `config.json` na origem e a API
aceita somente chamadas de localhost. GitHub Pages serve arquivos estaticos, por
isso o backend precisa de uma hospedagem HTTP separada.

## Objetivo

Disponibilizar a interface publica pelo GitHub Pages e manter os fluxos de
autenticacao, operacao e persistencia funcionando por meio da API .NET hospedada
gratuitamente e conectada ao PostgreSQL existente no Supabase.

## Fora de escopo

- Migrar regras de negocio da API .NET para Supabase Edge Functions.
- Garantir disponibilidade continua ou baixa latencia em camadas gratuitas.
- Publicar credenciais, dados pessoais ou chaves privadas no frontend ou Git.

## Atores

- Visitante: abre o site publico e se cadastra ou entra.
- Operador, administrador, cliente, prestador e motorista: usam os fluxos
  autorizados da SPA, com dados servidos pela API.
- Responsavel pelo deploy: configura os segredos de API, banco e administrador.

## Fluxo principal

1. GitHub Actions compila a SPA com o subcaminho do repositorio e publica no Pages.
2. O navegador carrega a configuracao publica de runtime e chama a API HTTPS.
3. A API valida CORS, autentica as requisicoes e persiste no Supabase.

## Fluxos alternativos e erros

- API ou banco indisponivel: a interface exibe seus estados de erro existentes; a
  camada gratuita pode acordar apos inatividade.
- Variavel de URL da API ausente no workflow: o deploy do Pages falha com instrucao
  para configurar `API_BASE_URL`, evitando publicar a SPA sem backend funcional.
- Supabase ou chave JWT ausentes no servico da API: o servico nao inicia e nao
  aceita requisicoes sem configuracao valida.

## Requisitos funcionais

- `REQ-001`: publicar a SPA do repositorio TowFast no GitHub Pages a partir de `main`.
- `REQ-002`: suportar o subcaminho `/TowFast/`, incluindo assets, configuracao de
  runtime e rotas internas da SPA.
- `REQ-003`: disponibilizar a API .NET por HTTPS em um servico Docker gratuito.
- `REQ-004`: permitir CORS para a origem GitHub Pages e localhost de desenvolvimento.
- `REQ-005`: conectar a API ao PostgreSQL Supabase via configuracao externa segura.
- `REQ-006`: manter URL, chave de navegador Maps e intervalos em runtime config;
  nenhum segredo de banco ou assinatura JWT pode ir para a SPA.
- `REQ-007`: documentar variaveis, migration inicial e limites operacionais do plano
  gratuito.

## Requisitos nao funcionais

- Seguranca: secrets ficam nas configuracoes do provedor; CORS permite somente as
  origens explicitamente configuradas; acesso ao banco usa TLS.
- Desempenho: frontend estatico distribuido pelo Pages; API free pode sofrer cold
  start e limites de recursos.
- Acessibilidade: preservar a UI existente sem mudancas de interacao.
- Observabilidade: endpoint `/health/live` para monitoramento do processo.

## Criterios de aceite

### AC-001 - Publicar a SPA em subcaminho

- Dado o repositorio `TowFast` com Pages configurado para GitHub Actions
- Quando o workflow compilar e publicar `main`
- Entao o site e seus assets carregam sob `/TowFast/` e uma rota interna pode ser
  atualizada sem tela em branco.

### AC-002 - Conversar com a API hospedada

- Dado `API_BASE_URL` configurado e a API online
- Quando um navegador na origem GitHub Pages fizer requisicao permitida
- Entao CORS permite a chamada e o fluxo autenticado utiliza o Supabase.

### AC-003 - Proteger configuracoes

- Dado o build e o servico publicados
- Quando os artefatos estaticos e arquivos Git sao inspecionados
- Entao connection string, senha do administrador e chave JWT nao aparecem neles.

## Dados e contratos

- Pages: `https://guisousa5.github.io/TowFast/`; origem CORS:
  `https://guisousa5.github.io`.
- API: Docker com ASP.NET Core, `/health/live` e connection string
  `ConnectionStrings__TowFast`.
- Supabase: preferir conexao direta se a rede do provedor e IPv6; para IPv4 usar
  Shared Pooler em modo Session, port 5432. Nao usar Transaction Pooler com EF Core.
- JWT: `Jwt__SigningKey` deve ser uma chave aleatoria com pelo menos 32 bytes.

## Suposicoes

- O repositorio remoto e `Guisousa5/TowFast` e o branch padrao e `main`.
- A API sera hospedada como servico Docker gratuito no Render.
- O usuario possui ou criara uma conta Render e conectara o repositorio GitHub.
- O Supabase existente contem ou recebera as migrations da API.

## Questoes abertas

- [ ] O repositorio precisa ser publico para usar GitHub Pages no plano gratuito,
  salvo se a conta tiver um plano GitHub pago com Pages privado. Confirmar antes de
  alterar a visibilidade.
- [ ] O usuario ainda deve cadastrar a connection string Supabase, chave JWT,
  credenciais do administrador, e apos criar a API informar sua URL como variavel
  de Actions `API_BASE_URL`.
