# Spec: Plataforma Web TowFast

## Status

Rascunho para aprovacao.

## Contexto e problema

A TowFast possui uma API REST para conectar clientes que precisam de assistencia
veicular a prestadores e motoristas parceiros. Falta uma interface web unica que
permita executar os fluxos do cliente, da operacao, do prestador e do motorista
sem recorrer ao Swagger ou a chamadas manuais.

O produto nao deve parecer um template administrativo convencional. A experiencia
precisa ser reconhecivel como TowFast, priorizar leitura rapida de estado, mapa,
tempo e proxima acao, e continuar eficiente em operacoes repetitivas.

## Objetivo

Entregar uma SPA responsiva em Angular que consuma todos os endpoints publicos da
TowFast API e ofereca experiencias orientadas por papel para:

- autocadastro, login e renovacao de sessao;
- manutencao de clientes, veiculos, locais, prestadores, motoristas e frota;
- abertura, consulta, filtragem e acompanhamento de solicitacoes;
- fila elegivel, aceite e execucao do atendimento pelo motorista;
- rastreamento da ultima posicao durante o atendimento;
- administracao dos tipos de servico e habilitacoes de cada prestador.

## Direcao de produto e design

### Conceito visual: Signal Grid

A interface deve combinar a clareza de uma central de mobilidade com a precisao de
um painel de despacho, sem copiar dashboards bancarios, templates SaaS ou layouts
com dezenas de cards flutuantes.

- A informacao principal deve aparecer em faixas e paineis sem moldura, com cards
  apenas para itens repetidos, modais e ferramentas realmente delimitadas.
- O elemento visual recorrente sera uma linha de progresso operacional que conecta
  horario, status, motorista, veiculo e local como um sinal continuo.
- Mapas e timelines devem ocupar a superficie disponivel, nunca aparecer como uma
  miniatura decorativa.
- A composicao desktop pode ser assimetrica: trilho lateral estreito, area de
  trabalho ampla e painel contextual recolhivel.
- No mobile, a navegacao deve usar barra inferior por icones e folhas inferiores
  para detalhes, preservando mapa, fila e acao principal.
- Cantos entre 4 e 8 px, bordas discretas, sombras raras e nenhum efeito de orb,
  vidro, bokeh ou degradê decorativo.

### Paleta e tipografia

- Fundo claro: branco frio `#F7F9FA`; superficie elevada `#FFFFFF`.
- Texto principal: grafite `#171A1D`; texto secundario `#58616A`.
- Acao primaria: ciano eletrico `#007F8B`; foco `#005E67`.
- Sinal de atencao: amarelo seguranca `#F2C230`.
- Sucesso: verde `#18865B`; perigo: coral `#D94B45`.
- Modo escuro opcional deve usar preto mineral, sem tornar azul ou roxo a paleta
  dominante.
- Tipografia de interface: familia sans-serif legivel e auto-hospedada; numeros,
  identificadores e horarios podem usar uma familia mono apenas como apoio.
- Nao usar tamanho de fonte proporcional a largura da viewport nem letter-spacing
  negativo.

### NG-ZORRO

- NG-ZORRO sera a biblioteca principal de componentes, sob licenca MIT.
- O tema TowFast deve combinar os componentes Ant Design com tokens proprios;
  sobrescritas globais devem permanecer pequenas, documentadas e estaveis.
- Usar Ant Design Icons e registrar apenas os icones utilizados pela aplicacao.
- Componentes previstos: Button, InputText, Password, Select, MultiSelect,
  DatePicker, InputMask, InputNumber, ToggleSwitch, Checkbox, Table, Paginator,
  Drawer, Dialog, ConfirmDialog, Toast, Message, Tag, Timeline, Tabs, Skeleton,
  Menu, Tooltip e ProgressSpinner.
- Botoes de ferramentas devem priorizar icones familiares com tooltip; texto deve
  ser reservado para comandos claros como `Aceitar chamado` e `Concluir`.

## Plataforma e arquitetura frontend

- Angular 22, TypeScript estrito e componentes standalone.
- NG-ZORRO 22, compativel com Angular 22, e Ant Design Icons.
- Angular Router com lazy loading por area funcional.
- Angular Signals para estado local e compartilhado; RxJS para HTTP, polling,
  geolocalizacao e cancelamento de fluxos assincronos.
- Reactive Forms tipados para todos os formularios.
- Cliente HTTP tipado gerado ou validado a partir do OpenAPI da TowFast API.
- Interceptors funcionais para Bearer token, correlation ID, refresh controlado e
  normalizacao de `application/problem+json`.
- Guards por autenticacao e papel, sem considerar o frontend uma fronteira de
  seguranca; a API continua sendo a autoridade final.
- Google Maps JavaScript API carregada sob demanda por
  `@googlemaps/js-api-loader`, encapsulada por um adaptador da aplicacao.
- Estrutura sugerida:

```text
src/app/
  core/          autenticacao, http, guards, configuracao e shell
  shared/        componentes visuais, formatadores e validadores
  features/
    auth/
    client/
    operation/
    provider/
    driver/
    requests/
    tracking/
  api/           contratos e cliente OpenAPI
  theme/         tokens TowFast e integracao NG-ZORRO
```

## Fora de escopo

- Alterar regras de negocio ou autorizacao da API.
- Criar novos endpoints nao existentes no backend.
- Editar ou excluir locais salvos; a API atual permite apenas listar e criar.
- Criar administradores ou operadores pela interface.
- Recuperar, redefinir ou trocar senha pela interface.
- Aplicativo nativo, notificacoes push, chat ou chamadas de voz.
- Pagamentos, cobrancas, repasses, precificacao ou notas fiscais.
- Calculo de rota, transito, pedagio ou previsao automatica de chegada.
- Historico visual da rota; somente a ultima posicao sera exibida.
- Funcionamento offline completo.
- Site institucional ou landing page de marketing.

## Atores

- Visitante: acessa login e autocadastro de cliente.
- Cliente: mantem os proprios dados, veiculos e locais; abre, acompanha e cancela
  solicitacoes permitidas.
- Operador: administra cadastros e acompanha solicitacoes, sem escolher o motorista
  no fluxo normal.
- Administrador: possui as funcoes operacionais e administra estados de catalogo,
  prestadores e demais cadastros expostos pela API.
- Prestador: mantem motoristas, frota e tipos de servico do proprio prestador.
- Motorista: consulta fila elegivel, aceita um atendimento, altera seus estados e
  publica localizacao.

## Arquitetura de informacao

### Area publica

- `/login`: autenticacao.
- `/cadastro`: autocadastro do cliente.

### Cliente

- `/app/inicio`: solicitacao ativa, proxima acao e historico recente.
- `/app/solicitacoes`: lista filtravel e detalhe.
- `/app/solicitacoes/nova`: abertura guiada em etapas.
- `/app/veiculos`: veiculos do cliente.
- `/app/locais`: locais salvos.
- `/app/perfil`: dados cadastrais.

### Operacao e administrador

- `/ops/painel`: faixas por status, volume e chamados que exigem atencao.
- `/ops/solicitacoes`: tabela densa, filtros persistentes e detalhe contextual.
- `/ops/clientes`: clientes, veiculos e locais.
- `/ops/prestadores`: prestadores, raio, motoristas, frota e servicos.
- `/ops/catalogo`: tipos de servico e estado ativo.

### Prestador

- `/partner/resumo`: atendimento ativo e disponibilidade operacional.
- `/partner/motoristas`: cadastro e manutencao de motoristas.
- `/partner/frota`: cadastro e manutencao da frota.
- `/partner/servicos`: habilitacao dos tipos de servico.

### Motorista

- `/driver/fila`: solicitacoes elegiveis em ordem de abertura.
- `/driver/atendimento`: atendimento ativo, mapa, estados e tracking.
- `/driver/historico`: solicitacoes atribuidas ao motorista.

## Google Maps e visualizacao dos atendimentos

- Google Maps sera o provedor oficial de mapas da plataforma.
- A biblioteca `maps` deve ser carregada somente nas telas que exibem mapa, por
  importacao dinamica, para nao aumentar o bundle e o custo das demais rotas.
- A chave deve vir de configuracao de runtime como `GOOGLE_MAPS_API_KEY`; ela nao
  deve ser escrita em componentes, arquivos TypeScript versionados ou logs.
- Como chaves da Maps JavaScript API ficam visiveis no navegador por natureza, a
  seguranca deve ser feita no Google Cloud com restricao por HTTP referrer aos
  dominios autorizados e restricao de API somente aos servicos utilizados.
- Um `GOOGLE_MAPS_MAP_ID` opcional deve permitir estilo visual proprio alinhado ao
  tema TowFast sem codificar o estilo na feature.
- A solicitacao selecionada deve mostrar marcador de origem, marcador de destino
  quando houver e marcador diferenciado para a ultima posicao do motorista.
- O marcador do motorista deve atualizar sem recriar o mapa, preservar o zoom do
  usuario e indicar visualmente quando `isStale=true`.
- Cliente, operacao e prestador so devem visualizar o mapa quando a API autorizar a
  consulta daquela solicitacao.
- O modo motorista deve mostrar sua posicao atual e os pontos da solicitacao sem
  enviar coordenadas fora de `EnRoute` ou `InProgress`.
- A API TowFast nao mantem historico de rota; portanto o mapa nao deve inventar nem
  persistir trilha percorrida.
- A integracao usara Maps JavaScript API com as bibliotecas Places e Geocoding para
  busca, selecao no mapa e preenchimento de endereco. Routes API nao faz parte do
  MVP e o mapa nao calculara rota viaria.
- A operacao podera abrir o mapa da solicitacao selecionada. Um mapa global com
  todos os motoristas nao faz parte do MVP, pois a API atual nao fornece feed
  agregado de posicoes.

## Fluxo principal do cliente

1. O visitante realiza autocadastro ou login.
2. A aplicacao identifica o papel retornado no token/resposta e abre a area correta.
3. O cliente cadastra ou seleciona um veiculo.
4. Escolhe o tipo de servico.
5. Seleciona um local salvo ou informa origem; reboque exige destino.
6. Revisa um resumo compacto e cria a solicitacao.
7. A tela ativa mostra status, timeline e, quando permitido, ultima posicao.
8. O cliente pode cancelar antes do atendimento conforme a regra da API.
9. Ao concluir, a tela apresenta itens, observacoes e historico imutavel.

## Fluxo principal do motorista

1. O motorista autentica e acessa a fila elegivel.
2. A fila exibe solicitacoes mais antigas primeiro e deixa claro quando o prestador
   esta indisponivel por possuir atendimento ativo.
3. O motorista abre o resumo, escolhe um veiculo ativo e aceita.
4. A aplicacao passa ao modo de atendimento dedicado.
5. Em `Accepted`, o motorista inicia deslocamento.
6. Em `EnRoute`, a aplicacao solicita permissao de geolocalizacao e publica pontos
   no intervalo configurado enquanto a pagina estiver ativa.
7. O motorista inicia o atendimento, registra itens e conclui; ou libera o aceite
   antes do deslocamento quando permitido.
8. Erros de versao ou aceite concorrente atualizam os dados e explicam o conflito
   sem repetir automaticamente comandos destrutivos.

## Fluxos alternativos e erros

- Credenciais invalidas: manter o e-mail, limpar a senha e exibir mensagem inline.
- Sessao expirada: tentar uma unica rotacao do refresh token; em falha, limpar a
  sessao e voltar ao login com mensagem de expiracao.
- `401`: nunca repetir em loop; encerrar sessao se o refresh nao recuperar.
- `403`: exibir estado sem permissao, sem revelar detalhes recebidos parcialmente.
- `404`: exibir recurso inexistente e permitir voltar para a lista.
- `409`: apresentar conflito contextual e acao para recarregar a versao atual.
- `429`: bloquear temporariamente a acao e informar quando tentar novamente.
- `application/problem+json`: mapear `title`, `detail`, `status` e `correlationId`.
- API indisponivel: preservar filtros e formularios nao sensiveis, permitir retry.
- GPS negado ou indisponivel: manter atendimento funcional, explicar que o cliente
  vera a ultima posicao como desatualizada e oferecer nova tentativa.
- Ponto fora de ordem: aceitar a resposta da API sem mover o mapa para tras.
- Prestador ocupado: exibir fila vazia com atendimento atual, sem falsa mensagem de
  ausencia de demanda.
- Conflito de aceite: fechar confirmacao, atualizar fila e informar que outro
  motorista assumiu ou que a capacidade do prestador mudou.
- Campos condicionais: ocultar destino em bateria/pneu e exigir destino em reboque.

## Requisitos funcionais

### Identidade e sessao

- `REQ-001`: A aplicacao deve permitir autocadastro de cliente.
- `REQ-002`: A aplicacao deve autenticar todos os papeis e redirecionar para a area
  autorizada.
- `REQ-003`: A aplicacao deve renovar access tokens de forma serializada, evitando
  varias rotacoes concorrentes do mesmo refresh token.
- `REQ-004`: A aplicacao deve executar logout, revogar o refresh token e limpar todo
  estado sensivel local.
- `REQ-005`: Rotas e itens de navegacao devem respeitar o papel e os IDs de escopo
  retornados pela API.

### Clientes, veiculos e locais

- `REQ-006`: Operacao deve listar, criar, atualizar, ativar e inativar clientes.
- `REQ-007`: Cliente deve visualizar e atualizar somente o proprio perfil.
- `REQ-008`: Cliente e operacao devem listar, cadastrar e ativar/inativar veiculos
  conforme permissao.
- `REQ-009`: Cliente e operacao devem listar e cadastrar locais salvos.
- `REQ-010`: Formularios devem aplicar mascara e validacao de CPF/CNPJ, telefone,
  placa, CEP, UF, coordenadas e senha antes do envio, sem substituir a validacao da
  API.

### Prestadores e catalogo

- `REQ-011`: Operacao deve listar, criar, atualizar, ativar e inativar prestadores.
- `REQ-012`: O formulario do prestador deve permitir definir centro de atendimento
  no mapa e raio em quilometros.
- `REQ-013`: Prestador autorizado e operacao devem listar e cadastrar motoristas.
- `REQ-014`: Prestador autorizado e operacao devem atualizar e inativar motoristas.
- `REQ-015`: Prestador autorizado e operacao devem listar e cadastrar veiculos de
  atendimento.
- `REQ-016`: Prestador autorizado e operacao devem atualizar e inativar veiculos de
  atendimento.
- `REQ-017`: Prestador autorizado e operacao devem consultar e substituir o conjunto
  de tipos de servico habilitados.
- `REQ-018`: Administrador e operador devem ativar ou inativar tipos do catalogo.

### Solicitacoes

- `REQ-019`: Cliente e operacao devem abrir solicitacoes usando local inline ou
  local salvo.
- `REQ-020`: A listagem deve suportar pagina, tamanho, status, periodo, servico,
  cliente, prestador e motorista, respeitando o escopo do usuario.
- `REQ-021`: O detalhe deve exibir identificador publico, cliente permitido,
  veiculo, servico, locais, vinculos, datas, itens e timeline de status.
- `REQ-022`: Cliente deve cancelar solicitacoes permitidas com motivo e versao.
- `REQ-023`: Operacao deve executar transicoes permitidas pela API sem despachar
  manualmente um motorista.
- `REQ-024`: A interface deve enviar sempre a versao mais recente nos comandos e
  atualizar o detalhe apos cada mutacao.

### Motorista e tracking

- `REQ-025`: Motorista deve consultar fila paginada elegivel, ordenada pela API.
- `REQ-026`: O resumo da fila nao deve exibir contato ou documento do cliente.
- `REQ-027`: Motorista deve aceitar uma solicitacao escolhendo veiculo ativo.
- `REQ-028`: Motorista deve liberar um aceite permitido informando motivo.
- `REQ-029`: Motorista e operacao devem executar `EnRoute`, `InProgress` e
  `Completed` conforme permissoes e estado.
- `REQ-030`: A conclusao deve permitir zero ou mais itens com nome, quantidade,
  unidade, descricao e observacao.
- `REQ-031`: O modo motorista deve publicar geolocalizacao apenas em `EnRoute` e
  `InProgress`, com `sourceEventId` unico por captura.
- `REQ-032`: A aplicacao deve consultar somente a ultima posicao e mostrar instante,
  precisao e estado atualizado/desatualizado.
- `REQ-033`: Polling e publicacao devem ser interrompidos ao sair da tela, perder a
  sessao ou alcançar estado terminal.

### Experiencia transversal

- `REQ-034`: Todas as mutacoes devem indicar estado carregando e impedir duplo
  envio.
- `REQ-035`: Listas devem preservar filtros na URL e permitir compartilhamento da
  visao entre operadores autorizados.
- `REQ-036`: Tabelas desktop devem oferecer colunas configuradas para leitura
  operacional; mobile deve usar linhas compactas ou lista, nunca tabela espremida.
- `REQ-037`: Datas devem ser exibidas no fuso local do navegador e enviadas em ISO
  8601 com offset/UTC conforme contrato.
- `REQ-038`: Enums da API devem possuir rotulos em portugues sem alterar seus valores
  no transporte.
- `REQ-039`: A aplicacao deve propagar ou gerar `X-Correlation-ID` e mostra-lo em
  erros tecnicos copiaveis.
- `REQ-040`: Skeletons devem preservar dimensoes finais e evitar saltos de layout.
- `REQ-041`: Telas de detalhe e atendimento devem usar Google Maps para exibir
  origem, destino aplicavel e ultima posicao autorizada do motorista.
- `REQ-042`: O carregamento do Google Maps deve ser lazy, tolerar erro de quota,
  chave ou rede e manter os dados textuais do atendimento utilizaveis sem o mapa.
- `REQ-043`: A plataforma deve oferecer temas claro e escuro desde o MVP, preservar
  a escolha do usuario e respeitar `prefers-color-scheme` na primeira visita.
- `REQ-044`: Origem, destino, local salvo e centro do prestador devem ser escolhidos
  por busca, clique no Google Maps ou localizacao atual, sem digitacao manual de
  latitude e longitude.
- `REQ-045`: Reboque deve sempre enviar `destination` ou `destinationLocationId` e
  impedir o envio enquanto o destino estiver ausente.
- `REQ-046`: Administrador e operador devem conseguir abrir a manutencao completa
  de clientes e prestadores listados, incluindo seus recursos associados conforme
  as autorizacoes existentes na API.
- `REQ-047`: O snapshot OpenAPI e os contratos gerados devem ser comparados com o
  documento servido pela API e regenerados quando houver divergencia.
- `REQ-048`: Polling e publicacao de localizacao devem executar no maximo uma
  requisicao por vez, respeitar intervalo minimo seguro, silenciar erros esperados
  de chamadas em background e interromper o GPS ao fechar o detalhe ou sair de um
  estado rastreavel, sem bloquear a interface ou repetir notificacoes.
- REQ-049: Apos a transicao bem-sucedida para EnRoute, o frontend do motorista
  deve iniciar o GPS e publicar latitude e longitude pela API .NET, cuja persistencia
  permanece no PostgreSQL do Supabase.

## Matriz de endpoints consumidos

| Endpoint | Uso na plataforma |
|---|---|
| `POST /api/v1/auth/register` | Autocadastro do cliente |
| `POST /api/v1/auth/login` | Login de todos os papeis |
| `POST /api/v1/auth/refresh` | Renovacao serializada da sessao |
| `POST /api/v1/auth/logout` | Encerramento e revogacao da sessao |
| `GET /api/v1/clients` | Proprio cliente ou lista da operacao |
| `POST /api/v1/clients` | Cadastro de cliente pela operacao |
| `PATCH /api/v1/clients/{id}` | Perfil e estado do cliente |
| `GET/POST /api/v1/clients/{id}/vehicles` | Listagem e cadastro de veiculos |
| `PATCH /api/v1/clients/{id}/vehicles/{vehicleId}/active` | Estado do veiculo |
| `GET/POST /api/v1/clients/{id}/locations` | Locais salvos |
| `GET/POST /api/v1/providers` | Listagem e cadastro de prestadores |
| `PATCH /api/v1/providers/{id}` | Dados do prestador |
| `PATCH /api/v1/providers/{id}/active` | Estado do prestador |
| `GET/POST /api/v1/providers/{id}/drivers` | Motoristas do prestador |
| `PATCH /api/v1/providers/{id}/drivers/{driverId}` | Manutencao do motorista |
| `GET/POST /api/v1/providers/{id}/vehicles` | Frota do prestador |
| `PATCH /api/v1/providers/{id}/vehicles/{vehicleId}` | Manutencao da frota |
| `GET/PUT /api/v1/providers/{id}/service-types` | Servicos habilitados |
| `GET /api/v1/service-types` | Catalogo ativo |
| `PATCH /api/v1/service-types/{id}/active` | Estado do tipo de servico |
| `POST /api/v1/service-requests` | Abertura da solicitacao |
| `GET /api/v1/service-requests` | Listagem paginada e filtrada |
| `GET /api/v1/service-requests/available` | Fila do motorista |
| `GET /api/v1/service-requests/{publicId}` | Detalhe completo |
| `POST /api/v1/service-requests/{id}/accept` | Aceite pelo motorista |
| `POST /api/v1/service-requests/{id}/release` | Liberacao pelo motorista |
| `POST /api/v1/service-requests/{id}/en-route` | Inicio do deslocamento |
| `POST /api/v1/service-requests/{id}/start` | Inicio do atendimento |
| `POST /api/v1/service-requests/{id}/complete` | Conclusao e itens |
| `POST /api/v1/service-requests/{id}/cancel` | Cancelamento permitido |
| `POST /api/v1/service-requests/{id}/tracking/positions` | Posicao do motorista |
| `GET /api/v1/service-requests/{id}/tracking/latest` | Ultima posicao autorizada |
| `GET /health/live` | Diagnostico de processo |
| `GET /health` | Diagnostico administrativo de readiness |

## Requisitos nao funcionais

- Seguranca: access token em memoria e refresh token somente em `sessionStorage`;
  aplicar CSP restritiva e nunca registrar tokens ou dados pessoais em logs do
  navegador.
- Seguranca: sanitizar conteudo textual, nao usar HTML vindo da API e evitar
  dependencias sem manutencao.
- Desempenho: primeiro carregamento autenticado com JavaScript inicial comprimido
  de ate 350 KB, excluindo mapa carregado sob demanda.
- Desempenho: Largest Contentful Paint de ate 2,5 s e Interaction to Next Paint de
  ate 200 ms no percentil 75 em dispositivos e rede de referencia definidos.
- Desempenho: listas devem usar paginacao da API; nenhuma tela deve carregar todo o
  historico operacional para filtrar no navegador.
- Acessibilidade: conformidade WCAG 2.2 AA, foco visivel, navegacao por teclado,
  rotulos associados, contraste e anuncios acessiveis para toasts importantes.
- Responsividade: suportar de 360 px a 1920 px sem sobreposicao, corte de texto ou
  controles inacessiveis.
- Compatibilidade: navegadores cobertos pelo Baseline da versao Angular escolhida.
- Observabilidade: erros HTTP devem incluir rota logica, status, duracao e
  correlation ID, sem token, documento, telefone ou coordenada completa.
- Testabilidade: regras de apresentacao e guards com testes unitarios; fluxos
  criticos com Playwright; contratos verificados contra OpenAPI no CI.
- Qualidade: TypeScript strict, lint sem erros, formatacao automatica e budgets de
  bundle no `angular.json`.
- Privacidade: mapa e tracking somente durante solicitacao autorizada; destruir
  watchers de geolocalizacao imediatamente ao encerrar o fluxo.
- Custos: carregar Maps, Places e Geocoding apenas nos formularios e telas que os
  utilizam; nao habilitar calculo de rota neste MVP.

## Criterios de aceite

### AC-001 - Autocadastro e sessao

- Dado um visitante com CPF/CNPJ e dados validos
- Quando concluir o cadastro
- Entao deve entrar como cliente e acessar apenas seu proprio espaco.

### AC-002 - Renovacao concorrente

- Dadas varias requisicoes que recebam `401` pelo mesmo access token expirado
- Quando a aplicacao renovar a sessao
- Entao deve enviar uma unica chamada de refresh e repetir cada leitura no maximo
  uma vez com o novo token.

### AC-003 - Navegacao por papel

- Dado um usuario autenticado
- Quando o shell for carregado
- Entao deve mostrar somente rotas e comandos adequados ao seu papel e escopo.

### AC-004 - Abertura de reboque

- Dado um cliente com veiculo ativo, origem e destino validos
- Quando confirmar um reboque
- Entao a solicitacao deve aparecer como `Pending` com identificador publico e
  timeline inicial.

### AC-005 - Servico sem destino

- Dado troca de bateria ou pneu
- Quando o formulario for exibido
- Entao nao deve solicitar amperagem, aro, medida do pneu ou destino.

### AC-006 - Fila do motorista

- Dado um motorista ativo e prestador livre
- Quando abrir a fila
- Entao deve ver apenas solicitacoes elegiveis, mais antigas primeiro, sem contato
  ou documento do cliente.

### AC-007 - Prestador ocupado

- Dado que o prestador possui atendimento ativo
- Quando qualquer motorista desse prestador abrir a fila
- Entao a tela deve indicar indisponibilidade e nao oferecer novo aceite.

### AC-008 - Aceite concorrente

- Dado um resumo aberto por dois motoristas
- Quando o segundo aceite receber `409`
- Entao a interface deve atualizar a fila e explicar o conflito sem sobrescrever o
  primeiro aceite.

### AC-009 - Execucao do atendimento

- Dado um atendimento aceito
- Quando o motorista avancar pelos estados permitidos
- Entao a interface deve habilitar somente a proxima acao valida e atualizar a
  versao apos cada resposta.

### AC-010 - Rastreamento ativo

- Dado um atendimento em `EnRoute` ou `InProgress` e permissao de localizacao
- Quando uma nova captura ocorrer
- Entao a aplicacao deve enviar evento unico e o cliente deve visualizar somente a
  ultima posicao, com horario e indicador de atualidade.

### AC-011 - Encerramento do tracking

- Dado atendimento concluido, cancelado, logout ou saida da tela operacional
- Quando o estado for observado
- Entao watchers, timers e polling devem ser encerrados imediatamente.

### AC-012 - Detalhe e privacidade

- Dado um usuario autorizado abrindo uma solicitacao
- Quando o detalhe for carregado
- Entao dados e contatos devem seguir exatamente a resposta filtrada da API, sem
  chamadas cruzadas que ampliem o acesso.

### AC-013 - Administracao do prestador

- Dado operador, administrador ou prestador no proprio escopo
- Quando mantiver motorista, frota ou servicos
- Entao a tela deve refletir inclusoes, atualizacoes e inativacoes sem recarregar a
  aplicacao inteira.

### AC-014 - Layout responsivo

- Dadas larguras de 360, 768, 1280 e 1920 px
- Quando as telas principais forem capturadas
- Entao nao deve haver texto cortado, controles sobrepostos, scroll horizontal
  acidental ou mapa sem dimensao estavel.

### AC-015 - Acessibilidade

- Dado um usuario operando somente por teclado e leitor de tela
- Quando executar login, abrir solicitacao, aceitar e concluir atendimento
- Entao foco, rotulos, ordem de leitura e mensagens devem permitir concluir os
  fluxos sem depender somente de cor, hover ou gesto.

### AC-016 - Cobertura da API

- Dado o documento OpenAPI atual do backend
- Quando a verificacao de contrato executar no CI
- Entao todo endpoint da matriz deve possuir cliente tipado e ao menos uma tela,
  acao ou diagnostico que o consuma.

### AC-017 - Visualizacao no Google Maps

- Dada uma solicitacao autorizada com coordenadas de origem, destino e tracking
- Quando cliente, operacao, prestador ou motorista abrir o detalhe permitido
- Entao o Google Maps deve enquadrar os marcadores disponiveis, distinguir a ultima
  posicao e mostrar seu estado atualizado ou desatualizado.

### AC-018 - Falha do Google Maps

- Dada uma falha de chave, quota ou rede do Google Maps
- Quando o mapa nao puder ser inicializado
- Entao status, enderecos, timeline e comandos do atendimento devem continuar
  disponiveis, com erro contextual e sem tela em branco.

### AC-019 - Temas claro e escuro

- Dado um usuario em qualquer area autenticada
- Quando alternar o tema
- Entao todas as superficies NG-ZORRO, mapas, estados e focos devem manter contraste
  WCAG AA e preservar a escolha nas proximas navegacoes.

### AC-020 - Endereco assistido pelo mapa

- Dado um formulario de origem, destino, local salvo ou centro do prestador
- Quando o usuario buscar, clicar no mapa ou usar sua localizacao atual
- Entao endereco e coordenadas devem ser preenchidos pela integracao Google sem
  expor campos manuais de latitude e longitude.

### AC-021 - Contrato de reboque

- Dado o tipo de servico `Tow`
- Quando o cliente confirmar a solicitacao
- Entao o payload deve conter `destination` ou `destinationLocationId`, independente
  da ordem em que o catalogo foi carregado.

### AC-022 - Rastreamento sem sobrecarga

- Dado um atendimento `EnRoute` ou `InProgress` com compartilhamento ativo
- Quando o navegador emitir varias amostras, uma requisicao demorar ou a API
  devolver erro
- Entao deve existir no maximo um envio e uma consulta em andamento, o erro deve
  aparecer uma unica vez e o compartilhamento deve parar sem travar a interface.

### AC-023 - Inicio automatico do GPS

- Dado um chamado aceito pelo motorista
- Quando ele clicar em Estou a caminho e a API confirmar EnRoute
- Entao o frontend inicia watchPosition e envia latitude e longitude ao endpoint
  de tracking da API, que persiste a ultima posicao no Supabase.

## Dados e contratos

- A URL base deve vir de configuracao de build/runtime, nunca ficar codificada em
  componentes.
- Estados recebidos: `Pending`, `Accepted`, `EnRoute`, `InProgress`, `Completed` e
  `Cancelled`; a interface exibira traducoes sem modificar o payload.
- Papeis recebidos: `Admin`, `Operator`, `Client`, `Provider` e `Driver`.
- Todas as mutacoes de solicitacao usam `version` para concorrencia otimista.
- `publicId` e usado em URLs amigaveis de detalhe; `id` GUID e usado nos comandos.
- Datas sao tratadas como instantes e formatadas para o fuso do navegador.
- O frontend nao acessa Supabase diretamente e nao conhece credenciais do banco.
- O frontend nao calcula elegibilidade, raio, permissao ou transicao valida como
  fonte de verdade; usa esses dados apenas para orientar a experiencia.

## Estrategia de testes

- Unitarios: formatadores, validadores, guards, store de sessao, serializacao do
  refresh e maquina de apresentacao das acoes por status.
- Componentes: formularios condicionais, tabelas responsivas, timeline, estados de
  loading/vazio/erro e permissoes visuais.
- Contrato: gerar/validar cliente a partir de `/swagger/v1/swagger.json` e falhar o
  CI diante de alteracao incompativel.
- E2E Playwright: autocadastro, login, cadastro de veiculo/local, abertura, fila,
  aceite, tracking simulado, conclusao, cancelamento, `401`, `403`, `409` e `429`.
- Visual: screenshots desktop/mobile em tema claro e escuro para telas principais.
- Acessibilidade: axe-core no CI e percurso manual por teclado antes da entrega.

## Suposicoes

- Angular 22 e NG-ZORRO 22 serao usados por sua compatibilidade oficial na data
  desta spec.
- A mesma aplicacao hospedara as experiencias dos cinco papeis com bundles lazy.
- O backend permanecera como unica fonte de autorizacao e regras de negocio.
- O tracking do MVP continuara por REST/polling, sem conexao persistente.
- O usuario pode possuir um unico papel por credencial, conforme resposta atual.
- Coordenadas de origem, destino e tracking serao renderizadas pelo Google Maps,
  cuja chave e Map ID serao configurados por ambiente.

## Questoes abertas

- Nenhuma questao aberta para o escopo atual.

## Decisoes confirmadas

- A integracao Google usara Maps JavaScript API, Places e Geocoding com uma chave
  de navegador restrita por dominio e somente a essas APIs.
- O refresh token permanecera em `sessionStorage` no MVP.
- Os temas claro e escuro fazem parte do MVP.
- Locais salvos poderao apenas ser listados e criados.
- A API nao sera alterada; gestao de administradores/operadores e recuperacao ou
  troca de senha ficam fora do escopo.
