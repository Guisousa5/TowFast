# Spec: Backend TowFast - Assistencia 24 horas

## Status

Implementada e validada em PostgreSQL no Supabase.

## Contexto e problema

A TowFast sera uma plataforma de assistencia veicular 24 horas. O sistema deve
conectar clientes que precisam de atendimento a prestadores parceiros capazes de
executar servicos de reboque, troca de bateria ou troca de pneu.

O primeiro produto sera um backend REST centralizado. Ele devera registrar os
dados operacionais, controlar o ciclo de vida das solicitacoes e disponibilizar
contratos que possam ser consumidos por paineis web e por um aplicativo movel a
ser construido futuramente.

Sem esse backend, a operacao depende de contatos e registros dispersos, com pouca
rastreabilidade sobre cliente, local do atendimento, parceiro, motorista, veiculo,
servico solicitado e andamento da ocorrencia.

## Objetivo

Entregar uma API REST simples, segura e extensivel para que a TowFast possa:

- Manter clientes e seus veiculos.
- Manter prestadores parceiros, motoristas e veiculos de atendimento.
- Manter os tipos de servico oferecidos.
- Registrar locais de origem e destino quando aplicavel.
- Abrir, disponibilizar, aceitar, acompanhar, concluir e cancelar solicitacoes.
- Rastrear em tempo real a localizacao do motorista durante o deslocamento e o
  atendimento, respeitando autorizacao e privacidade.
- Registrar itens e observacoes relacionados ao atendimento.
- Consultar o historico completo de cada solicitacao.
- Persistir os dados em PostgreSQL hospedado no Supabase.
- Reutilizar os mesmos contratos em um futuro aplicativo movel.

## Escopo do MVP

- Backend em .NET 10 (`net10.0`) e ASP.NET Core 10 Web API.
- API REST versionada em `/api/v1`.
- Persistencia PostgreSQL no Supabase.
- Autenticacao JWT implementada pela propria API e autorizacao baseada em papeis.
- CRUD dos cadastros essenciais.
- Fluxo operacional completo de uma solicitacao.
- Fila de solicitacoes abertas para motoristas elegiveis.
- Aceite da solicitacao pelo proprio motorista, sem direcionamento por operador.
- Rastreamento GPS em tempo real operacional por atualizacoes periodicas via REST.
- Consultas paginadas e filtradas.
- Historico auditavel de alteracoes de status.
- Documento OpenAPI e Swagger UI configurado para exploracao da API.

## Fora de escopo

- Aplicativo movel para clientes ou motoristas.
- Interface web completa para a operacao.
- Selecao ou despacho automatico do parceiro por distancia.
- Calculo automatico de rota, pedagio ou tempo estimado.
- Gateway de pagamento, cobranca, repasse e nota fiscal.
- Chat, chamadas de voz ou notificacoes push.
- Integracao com seguradoras, frotas ou outros sistemas externos.
- Precificacao automatica e negociacao de orcamento.
- Multiempresa ou operacao white-label.
  

## Atores

- Cliente: solicita atendimento, informa o veiculo e os locais, consulta suas
  solicitacoes e pode cancelar quando permitido.
- Operador TowFast: administra cadastros, acompanha a fila e intervem em
  solicitacoes quando necessario, sem direcionar o atendimento no fluxo normal.
- Administrador TowFast: possui as permissoes do operador e gerencia usuarios,
  configuracoes e cadastros de referencia.
- Prestador parceiro: empresa ou profissional credenciado que mantem motoristas,
  frota e tipos de servico habilitados para participar da rede TowFast.
- Motorista: pessoa vinculada a um prestador que visualiza solicitacoes abertas
  elegiveis, aceita uma delas, executa o atendimento, atualiza os estados
  operacionais e compartilha sua localizacao durante a solicitacao ativa.
- Sistema cliente futuro: aplicativo ou painel que consumira a API REST sem
  depender de acesso direto ao banco de dados.

## Fluxo principal

1. O cliente realiza seu proprio cadastro ou um operador cadastra o cliente; em
   seguida, o usuario se autentica na API.
2. O cliente seleciona ou cadastra o veiculo que precisa de atendimento.
3. O cliente escolhe reboque, troca de bateria ou troca de pneu.
4. O cliente informa a localizacao do atendimento, dados complementares e, para
   reboque, o destino desejado.
5. A API valida os dados e cria a solicitacao com status `PENDING`.
6. Motoristas ativos cujo prestador nao possua outro atendimento ativo consultam
   uma fila ordenada da solicitacao mais antiga para a mais recente, contendo
   solicitacoes `PENDING` compativeis com os servicos e o raio de atendimento.
7. Um motorista abre o resumo da solicitacao, seleciona um veiculo de atendimento
   ativo de seu prestador e aceita o servico.
8. A API valida a elegibilidade e confirma atomicamente que o prestador continua
   sem outro atendimento ativo; entao vincula prestador, motorista e veiculo e
   altera o status para `ACCEPTED`.
9. O motorista informa o deslocamento pelo estado `EN_ROUTE`, iniciando o
   compartilhamento periodico de sua localizacao com a solicitacao.
10. O cliente e a operacao consultam a ultima posicao, o instante da captura e a
    indicacao de localizacao atualizada ou desatualizada.
11. O motorista inicia o atendimento pelo estado `IN_PROGRESS` e pode continuar
    compartilhando a localizacao enquanto a solicitacao estiver ativa.
12. O motorista ou operador conclui o atendimento como `COMPLETED`, encerrando o
    rastreamento e registrando os itens utilizados e observacoes finais.
13. O cliente e a operacao podem consultar o estado atual e o historico.

## Fluxos alternativos e erros

- Dados obrigatorios ausentes ou invalidos: a API rejeita a operacao com `400` e
  informa os campos invalidos em um corpo padrao de erro.
- Usuario nao autenticado: a API retorna `401`.
- Usuario autenticado sem permissao: a API retorna `403` sem revelar dados de
  outros clientes ou prestadores.
- Cadastro ou solicitacao inexistente: a API retorna `404`.
- Conflito de unicidade ou transicao de estado invalida: a API retorna `409`.
- Prestador inativo ou sem o tipo de servico: seus motoristas nao visualizam nem
  podem aceitar a solicitacao.
- Prestador com solicitacao em `ACCEPTED`, `EN_ROUTE` ou `IN_PROGRESS`: seus
  motoristas nao recebem novas solicitacoes disponiveis e uma tentativa direta de
  aceite retorna `409`.
- Motorista ou veiculo de atendimento inativo: o aceite e rejeitado com `409`.
- Dois motoristas tentam aceitar a mesma solicitacao: o primeiro aceite confirmado
  vence e os demais recebem `409`, sem sobrescrever o atendimento escolhido.
- Dois motoristas do mesmo prestador tentam aceitar solicitacoes diferentes ao
  mesmo tempo: somente um aceite e confirmado; o outro recebe `409`.
- Motorista libera uma solicitacao aceita antes do inicio do atendimento: o vinculo
  atual e encerrado, o motivo fica registrado e a solicitacao retorna a `PENDING`.
- Cliente cancela antes do atendimento: a solicitacao muda para `CANCELLED`, o
  motivo e registrado e nenhuma taxa de cancelamento e gerada.
- Cancelamento depois de `IN_PROGRESS`: somente operador ou administrador pode
  executar a operacao, sempre com justificativa.
- Falha simultanea de atualizacao: a API impede sobrescrita silenciosa e retorna
  conflito para que o consumidor recarregue o estado atual.
- Destino ausente em servico de reboque: a solicitacao nao e criada.
- Destino informado em troca de bateria ou pneu: o campo e ignorado ou rejeitado
  de forma consistente conforme o contrato definitivo da API.
- GPS indisponivel no dispositivo: a solicitacao continua ativa, a ultima posicao
  permanece disponivel como desatualizada e o motorista pode retomar os envios.
- Posicao com coordenadas invalidas, instante futuro ou fora da janela permitida:
  a API rejeita a atualizacao com `400` sem substituir a ultima posicao valida.
- Posicao enviada por motorista nao atribuido: a API retorna `403`.
- Atualizacao recebida fora de `EN_ROUTE` ou `IN_PROGRESS`: a API retorna `409`.
- Atualizacao duplicada: a API trata o identificador do evento de forma idempotente
  e nao cria dois pontos para a mesma captura.

## Estados da solicitacao

| Estado | Descricao | Proximos estados permitidos |
|---|---|---|
| `PENDING` | Disponivel para motoristas elegiveis | `ACCEPTED`, `CANCELLED` |
| `ACCEPTED` | Aceita e vinculada ao motorista | `EN_ROUTE`, `PENDING`, `CANCELLED` |
| `EN_ROUTE` | Motorista em deslocamento | `IN_PROGRESS`, `CANCELLED` |
| `IN_PROGRESS` | Servico em execucao | `COMPLETED`, `CANCELLED` |
| `COMPLETED` | Servico concluido | Estado terminal |
| `CANCELLED` | Solicitacao cancelada | Estado terminal |

Toda transicao deve gerar um registro imutavel no historico com estado anterior,
novo estado, data, usuario responsavel e motivo quando aplicavel.

## Requisitos funcionais

### Identidade e acesso

- `REQ-001`: A API deve implementar autenticacao propria, validar credenciais e
  emitir access token JWT de curta duracao e refresh token revogavel.
- `REQ-002`: A API deve autorizar operacoes pelos papeis `ADMIN`, `OPERATOR`,
  `CLIENT`, `PROVIDER` e `DRIVER`.
- `REQ-003`: Um cliente so pode consultar e alterar dados pertencentes a ele.
- `REQ-004`: Um motorista pode consultar resumos de solicitacoes abertas elegiveis
  com tipo de servico, modelo do veiculo e local de origem; contato e outros dados
  pessoais do cliente ficam restritos a operacao e ao motorista que aceitou.

### Clientes, veiculos e locais

- `REQ-005`: A API deve permitir o autocadastro do cliente e tambem permitir que
  operador ou administrador crie, consulte, atualize, ative e inative clientes.
- `REQ-006`: O cliente deve informar CPF ou CNPJ valido; a API deve impedir
  duplicidade do documento e do e-mail usado como credencial.
- `REQ-007`: A API deve cadastrar um ou mais veiculos para cada cliente.
- `REQ-008`: Cada veiculo do cliente deve possuir placa unica e nao reutilizavel,
  fabricante, modelo, ano opcional, cor opcional e observacoes opcionais.
- `REQ-009`: A API deve cadastrar e reutilizar locais de um cliente, contendo
  endereco estruturado e coordenadas geograficas quando disponiveis.
- `REQ-010`: O local usado em uma solicitacao deve ser preservado como registro
  historico, mesmo que o endereco salvo pelo cliente seja alterado posteriormente.

### Prestadores, motoristas e frota

- `REQ-011`: A API deve criar, consultar, atualizar, ativar e inativar prestadores.
- `REQ-012`: Um prestador deve conter identificacao fiscal, razao social ou nome,
  nome comercial opcional, contatos, coordenadas do centro de atendimento e raio
  maximo de cobertura em quilometros.
- `REQ-013`: A API deve associar ao prestador os tipos de servico que ele executa.
- `REQ-014`: A API deve cadastrar motoristas vinculados a exatamente um prestador.
- `REQ-015`: Cada motorista deve possuir nome, documento, telefone, numero da CNH,
  categoria, validade da CNH e estado ativo ou inativo.
- `REQ-016`: A API deve cadastrar veiculos de atendimento vinculados a um prestador,
  com placa, tipo, descricao e capacidade opcional.
- `REQ-017`: A API nao deve permitir aceite por prestadores, motoristas ou veiculos
  de atendimento inativos, nem por motorista cujo prestador ja possua solicitacao
  em `ACCEPTED`, `EN_ROUTE` ou `IN_PROGRESS`.

### Catalogo e itens

- `REQ-018`: A API deve disponibilizar os tipos iniciais `TOW`,
  `BATTERY_REPLACEMENT` e `TIRE_CHANGE`.
- `REQ-019`: O catalogo deve permitir ativacao e inativacao sem apagar o historico.
- `REQ-020`: Uma solicitacao deve aceitar itens utilizados no atendimento, com
  nome, descricao, quantidade, unidade e observacao opcionais.
- `REQ-021`: Troca de bateria e troca de pneu nao devem exigir campos tecnicos
  especificos; para esses servicos, o modelo do veiculo e suficiente no MVP.

### Solicitacoes e operacao

- `REQ-022`: A API deve criar uma solicitacao vinculada a cliente, veiculo,
  tipo de servico e local de origem.
- `REQ-023`: Uma solicitacao de reboque deve exigir um local de destino.
- `REQ-024`: A solicitacao deve receber identificador publico nao sequencial,
  data de abertura e status inicial `PENDING`.
- `REQ-025`: A API deve listar para cada motorista ativo apenas solicitacoes
  `PENDING` compativeis com os servicos e dentro do raio de atendimento de seu
  prestador, ordenadas obrigatoriamente por data de abertura crescente; se o
  prestador estiver em outro atendimento ativo, a lista disponivel deve ser vazia.
- `REQ-026`: O motorista deve poder aceitar uma solicitacao disponivel selecionando
  um veiculo ativo de seu prestador; o aceite deve vincular automaticamente
  prestador, motorista e veiculo e mudar o status para `ACCEPTED`.
- `REQ-027`: Apenas um motorista pode aceitar cada solicitacao; tentativas
  concorrentes posteriores devem receber `409` sem alterar o primeiro aceite.
- `REQ-028`: A API deve validar todas as transicoes conforme a tabela de estados.
- `REQ-029`: A API deve permitir cancelamento segundo o estado e o papel do usuario,
  registrar o motivo e nao gerar taxa ou efeito financeiro no MVP.
- `REQ-030`: Toda mudanca de status deve ser registrada no historico imutavel.
- `REQ-031`: A conclusao deve aceitar observacao final e itens utilizados.
- `REQ-032`: A API deve permitir consulta da solicitacao por identificador com
  cliente, veiculo, locais, servico, vinculo do motorista, itens e historico.
- `REQ-033`: A API deve listar solicitacoes com paginacao e filtros por status,
  periodo, tipo de servico, cliente, prestador e motorista, conforme permissao.
- `REQ-034`: A API deve registrar datas de aceite, inicio do
  deslocamento, inicio do atendimento, conclusao e cancelamento quando ocorrerem.
- `REQ-035`: Operacoes de aceite, liberacao e transicao de estado devem ser atomicas;
  a confirmacao do aceite deve impedir que o mesmo prestador assuma duas solicitacoes
  ativas, inclusive quando as tentativas forem concorrentes.

### API e manutencao

- `REQ-036`: Todos os endpoints devem usar JSON e contratos independentes das
  entidades de persistencia.
- `REQ-037`: A API deve usar codigos HTTP coerentes e erros no formato
  `application/problem+json`.
- `REQ-038`: Endpoints de listagem devem possuir paginacao com limite maximo
  configuravel e ordenacao deterministica.
- `REQ-039`: A exclusao de cadastros com historico deve ser logica por inativacao.
- `REQ-040`: A implementacao deve separar controllers/endpoints, casos de uso ou
  services, dominio e repositories por meio de interfaces.
- `REQ-041`: Repositories devem encapsular acesso a dados, sem regras de negocio.
- `REQ-042`: Services ou casos de uso devem coordenar regras, transacoes e portas
  sem depender diretamente de ASP.NET Core ou detalhes do Supabase.
- `REQ-043`: A API deve publicar documento OpenAPI atualizado e disponibilizar
  Swagger UI em `/swagger`, com suporte ao envio de token JWT Bearer e habilitacao
  controlada por ambiente ou configuracao.
- `REQ-044`: A API deve possuir endpoint de health check sem dados sensiveis.

### Rastreamento em tempo real

- `REQ-045`: O motorista atribuido deve poder publicar sua posicao enquanto a
  solicitacao estiver em `EN_ROUTE` ou `IN_PROGRESS`.
- `REQ-046`: Cada atualizacao deve conter latitude, longitude, instante de captura,
  identificador idempotente do evento e, quando disponiveis, precisao, velocidade
  e direcao do deslocamento.
- `REQ-047`: A API deve validar limites geograficos, idade da amostra, vinculo do
  motorista e estado da solicitacao antes de persistir uma posicao.
- `REQ-048`: Cliente dono da solicitacao, operador, administrador, prestador e
  motorista atribuidos devem poder consultar exclusivamente a ultima posicao
  autorizada; o cliente nao deve receber historico da rota.
- `REQ-049`: A consulta deve informar `capturedAt`, `receivedAt` e `isStale`; uma
  posicao deve ser considerada desatualizada apos limite configuravel, inicialmente
  definido em 30 segundos.
- `REQ-050`: O rastreamento deve iniciar apenas ao entrar em `EN_ROUTE` e encerrar
  automaticamente em `COMPLETED` ou `CANCELLED`.
- `REQ-051`: A API deve rejeitar novos pontos apos o encerramento e nao deve
  rastrear o motorista quando nao houver solicitacao ativa atribuida a ele.
- `REQ-052`: A API deve manter somente a ultima posicao valida de cada solicitacao,
  substituindo-a quando receber uma amostra mais recente.
- `REQ-053`: O cliente deve acessar somente o rastreamento de suas solicitacoes e
  apenas durante a janela de compartilhamento autorizada.
- `REQ-054`: O envio de pontos deve ser idempotente e tolerar chegada fora de ordem;
  uma amostra antiga ou duplicada nao deve substituir a posicao atual mais recente.
- `REQ-055`: A frequencia de publicacao e consulta deve ser configuravel, sem um
  intervalo obrigatorio e sem exigir conexao persistente no MVP.
- `REQ-056`: A camada de aplicacao deve expor portas independentes do mecanismo de
  entrega, permitindo futura adocao de SSE, WebSocket ou Supabase Realtime.

## Requisitos nao funcionais

- Arquitetura: aplicar arquitetura hexagonal, mantendo dominio e aplicacao
  independentes de ASP.NET Core, Supabase e mecanismo de persistencia.
- Plataforma: todos os projetos do backend devem usar `TargetFramework` `net10.0`
  e dependencias compativeis com .NET 10, sem multi-targeting no MVP.
- Clean Code: nomes explicitos, funcoes pequenas, validacao nas fronteiras, baixa
  duplicacao e comentarios apenas para decisoes que o codigo nao explica.
- Simplicidade: evitar mensageria, cache distribuido, CQRS e outras estruturas sem
  necessidade comprovada no MVP.
- Seguranca: armazenar segredos fora do codigo, usar TLS, validar entradas, aplicar
  menor privilegio e nunca expor a chave `service_role` do Supabase a clientes.
- Privacidade: documentos, contatos e localizacoes sao dados pessoais e devem ser
  exibidos apenas aos papeis autorizados; logs nao devem registrar tokens ou dados
  pessoais completos.
- Privacidade do rastreamento: a localizacao do motorista deve ser vinculada a uma
  solicitacao ativa, nunca publicada como localizacao global do profissional, e
  somente a ultima posicao deve ser mantida.
- Persistencia: usar PostgreSQL do Supabase com migrations versionadas e chaves
  estrangeiras, indices e restricoes de unicidade definidos no schema.
- Concorrencia: solicitacoes devem possuir controle otimista por versao ou campo
  equivalente para evitar transicoes perdidas.
- Desempenho: no volume inicial, consultas unitarias devem responder em ate 500 ms
  no percentil 95 e listagens paginadas em ate 1 segundo, desconsiderando latencia
  externa fora do controle da API.
- Atualidade do rastreamento: em condicoes normais de rede, uma posicao aceita deve
  estar disponivel para consulta em ate 5 segundos no percentil 95.
- Escalabilidade do rastreamento: a ultima posicao deve ser localizada diretamente
  pela solicitacao, sem varrer ou manter historico de rota.
- Disponibilidade: a API deve falhar de forma controlada quando o banco estiver
  indisponivel e retornar erro sem detalhes internos.
- Observabilidade: logs estruturados devem incluir correlation ID, endpoint,
  resultado, duracao e identificadores tecnicos necessarios para diagnostico.
- Testabilidade: regras de dominio e casos de uso devem ser testaveis sem banco;
  repositories e endpoints essenciais devem possuir testes de integracao.
- Compatibilidade: alteracoes incompativeis devem gerar nova versao da API.
- Datas: persistir instantes em UTC e converter somente nas bordas de apresentacao.

## Criterios de aceite

### AC-001 - Cadastrar cliente e veiculo

- Dado um cliente ainda nao cadastrado com CPF ou CNPJ valido
- Quando ele realizar seu autocadastro e adicionar um veiculo
- Entao os registros ficam disponiveis para consulta sem expor entidades internas

### AC-002 - Impedir cliente duplicado

- Dado um cliente existente com determinado documento
- Quando houver nova tentativa de cadastro com o mesmo documento
- Entao a API retorna `409` e nao cria um segundo registro

### AC-003 - Manter rede de atendimento

- Dado um prestador ativo
- Quando um operador cadastrar seus servicos, motorista e veiculo de atendimento
- Entao todos ficam vinculados ao mesmo prestador e o motorista pode consultar e
  aceitar solicitacoes compativeis

### AC-004 - Criar solicitacao de troca de pneu

- Dado um cliente autenticado com veiculo e local validos
- Quando ele solicitar uma troca de pneu
- Entao a API cria uma solicitacao `PENDING` com identificador publico e historico

### AC-005 - Exigir destino no reboque

- Dado um cliente autenticado com origem valida
- Quando ele solicitar reboque sem informar destino
- Entao a API retorna `400` e nao persiste a solicitacao

### AC-006 - Motorista aceitar solicitacao compativel

- Dado uma solicitacao `PENDING` de troca de bateria e um motorista ativo cujo
  prestador oferece esse servico na area solicitada
- Quando o motorista aceitar usando um veiculo de atendimento ativo
- Entao a solicitacao muda para `ACCEPTED`, vincula prestador, motorista e veiculo
  e deixa de aparecer na fila dos demais motoristas

### AC-007 - Bloquear aceite inelegivel

- Dado um motorista inativo, um prestador sem o servico ou um veiculo inativo
- Quando houver tentativa de aceitar a solicitacao
- Entao a API retorna `409` ou `403` e mantem a solicitacao em `PENDING`

### AC-008 - Liberar e redisponibilizar solicitacao

- Dado uma solicitacao `ACCEPTED` que ainda nao iniciou o atendimento
- Quando o motorista libera-la informando o motivo
- Entao o vinculo e encerrado, o motivo e auditado e o status volta a `PENDING`

### AC-009 - Executar fluxo completo

- Dado uma solicitacao aceita por um motorista autorizado
- Quando forem informados deslocamento, inicio e conclusao em ordem
- Entao a API registra cada estado, responsavel e instante ate `COMPLETED`

### AC-010 - Impedir transicao fora de ordem

- Dado uma solicitacao `PENDING`
- Quando alguem tentar conclui-la diretamente
- Entao a API retorna `409` e nao altera o estado nem o historico

### AC-022 - Resolver aceite concorrente

- Dado uma solicitacao `PENDING` exibida simultaneamente a dois motoristas elegiveis
- Quando ambos tentarem aceita-la ao mesmo tempo
- Entao somente um aceite e confirmado e o outro recebe `409`

### AC-011 - Preservar historico de local

- Dado uma solicitacao criada com um endereco salvo pelo cliente
- Quando o cliente alterar posteriormente seu endereco salvo
- Entao a solicitacao continua exibindo o local original do atendimento

### AC-012 - Restringir acesso entre clientes

- Dado dois clientes autenticados com solicitacoes distintas
- Quando um deles tentar consultar a solicitacao do outro
- Entao a API retorna `404` ou `403`, conforme a politica definida, sem expor dados

### AC-013 - Consultar fila operacional

- Dado um operador autenticado e varias solicitacoes
- Quando ele filtrar por status, periodo e tipo de servico
- Entao a API retorna somente os resultados correspondentes, de forma paginada

### AC-014 - Registrar itens da conclusao

- Dado uma solicitacao `IN_PROGRESS`
- Quando o motorista conclui-la informando os itens utilizados
- Entao a solicitacao e os itens sao persistidos atomicamente como `COMPLETED`

### AC-015 - Documentar e monitorar a API

- Dado a aplicacao em execucao
- Quando um desenvolvedor acessar `/swagger`, informar um token JWT pelo esquema
  Bearer e consultar o health check
- Entao encontra a interface Swagger UI funcional, os contratos atuais, endpoints
  protegidos testaveis e o estado de saude sem dados sensiveis

### AC-016 - Publicar localizacao do motorista

- Dado uma solicitacao `EN_ROUTE` e um motorista atribuido autenticado
- Quando o dispositivo enviar uma posicao valida com identificador unico
- Entao a API persiste o ponto e o disponibiliza como ultima posicao da solicitacao

### AC-017 - Visualizar rastreamento atualizado

- Dado uma solicitacao ativa com rastreamento e rede em condicoes normais
- Quando o cliente dono da solicitacao consultar a ultima posicao
- Entao recebe somente as coordenadas mais recentes, instante da captura e
  `isStale: false`, sem receber o historico da rota

### AC-018 - Indicar perda de sinal

- Dado uma solicitacao ativa cuja ultima posicao foi capturada ha mais de 30 segundos
- Quando um usuario autorizado consultar o rastreamento
- Entao a API retorna a ultima posicao conhecida com `isStale: true`

### AC-019 - Bloquear rastreamento indevido

- Dado um motorista nao atribuido ou uma solicitacao fora de `EN_ROUTE` e `IN_PROGRESS`
- Quando houver tentativa de publicar uma posicao
- Entao a API retorna `403` ou `409` e nao persiste o ponto

### AC-020 - Encerrar compartilhamento

- Dado uma solicitacao com rastreamento ativo
- Quando ela mudar para `COMPLETED` ou `CANCELLED`
- Entao novos pontos sao rejeitados e o cliente deixa de receber atualizacoes novas

### AC-021 - Tratar atualizacao duplicada

- Dado um ponto de localizacao ja recebido
- Quando o dispositivo repetir o mesmo identificador de evento
- Entao a API responde de forma idempotente e mantem a mesma ultima posicao

### AC-023 - Ordenar fila por abertura

- Dado mais de uma solicitacao elegivel dentro do raio do prestador
- Quando o motorista consultar a fila de atendimentos disponiveis
- Entao a API retorna as solicitacoes da mais antiga para a mais recente

### AC-024 - Limitar cobertura pelo raio

- Dado um prestador com centro e raio de atendimento cadastrados
- Quando houver solicitacoes dentro e fora desse raio
- Entao seus motoristas visualizam e podem aceitar somente as solicitacoes internas

### AC-025 - Autenticar com JWT da API

- Dado um cliente autocadastrado com credenciais validas
- Quando ele realizar login
- Entao a API valida o hash da senha e emite access token JWT e refresh token
  revogavel sem depender do Supabase Auth

### AC-026 - Bloquear novo aceite do prestador ocupado

- Dado um prestador com uma solicitacao em `ACCEPTED`, `EN_ROUTE` ou `IN_PROGRESS`
- Quando qualquer motorista desse prestador consultar a fila ou tentar aceitar
  outra solicitacao
- Entao a fila disponivel fica vazia e o aceite direto retorna `409` sem alteracoes

### AC-027 - Resolver aceites concorrentes do mesmo prestador

- Dado dois motoristas do mesmo prestador e duas solicitacoes `PENDING` diferentes
- Quando os motoristas tentarem aceitar as solicitacoes simultaneamente
- Entao somente um aceite e confirmado e o outro recebe `409`

## Dados e contratos

Todos os identificadores internos devem ser UUID. Todas as tabelas mutaveis devem
possuir `created_at` e `updated_at` em `timestamptz`; registros inativaveis devem
possuir `is_active`. Nomes finais podem ser ajustados no plano, mas os conceitos e
relacionamentos abaixo fazem parte da especificacao.

### `users`

Identidades autorizadas a usar a API.

| Campo | Regra |
|---|---|
| `id` | UUID, chave primaria |
| `email` | Credencial normalizada e unica |
| `password_hash` | Hash seguro da senha; a senha pura nunca e persistida |
| `role` | `ADMIN`, `OPERATOR`, `CLIENT`, `PROVIDER` ou `DRIVER` |
| `client_id` | Obrigatorio somente para papel `CLIENT` |
| `provider_id` | Obrigatorio para `PROVIDER` e derivado para `DRIVER` |
| `driver_id` | Obrigatorio somente para papel `DRIVER` |
| `is_active` | Bloqueia acesso quando falso |

### `refresh_tokens`

| Campo | Regra |
|---|---|
| `id` | UUID, chave primaria |
| `user_id` | FK obrigatoria para `users` |
| `token_hash` | Hash unico do refresh token; o token puro nunca e persistido |
| `expires_at` | Instante de expiracao |
| `revoked_at` | Instante opcional de revogacao |
| `replaced_by_token_id` | FK opcional usada na rotacao do token |

### `clients`

| Campo | Regra |
|---|---|
| `id` | UUID, chave primaria |
| `full_name` | Obrigatorio |
| `document_type` | `CPF` ou `CNPJ`, coerente com o documento informado |
| `document` | Obrigatorio, normalizado e unico |
| `email` | Obrigatorio, normalizado e unico para autocadastro |
| `phone` | Obrigatorio, normalizado |
| `notes` | Opcional, acesso restrito a operacao |
| `is_active` | Exclusao logica |

### `client_vehicles`

| Campo | Regra |
|---|---|
| `id` | UUID, chave primaria |
| `client_id` | FK obrigatoria para `clients` |
| `license_plate` | Obrigatoria, normalizada |
| `manufacturer` | Obrigatorio |
| `model` | Obrigatorio |
| `model_year` | Opcional, intervalo valido |
| `color` | Opcional |
| `notes` | Opcional |
| `is_active` | Exclusao logica |

A placa deve ser unica no cadastro e nao sera reutilizada para outro veiculo no MVP.

### `providers`

| Campo | Regra |
|---|---|
| `id` | UUID, chave primaria |
| `legal_name` | Obrigatorio |
| `trade_name` | Opcional |
| `document` | Obrigatorio, normalizado e unico |
| `email` | Opcional |
| `phone` | Obrigatorio |
| `service_center_latitude` | Obrigatoria, entre -90 e 90 |
| `service_center_longitude` | Obrigatoria, entre -180 e 180 |
| `service_radius_km` | Obrigatorio e maior que zero |
| `notes` | Opcional, acesso restrito a operacao |
| `is_active` | Define elegibilidade para aceite |

### `drivers`

| Campo | Regra |
|---|---|
| `id` | UUID, chave primaria |
| `provider_id` | FK obrigatoria para `providers` |
| `full_name` | Obrigatorio |
| `document` | Obrigatorio, normalizado e unico |
| `phone` | Obrigatorio |
| `license_number` | CNH obrigatoria e unica |
| `license_category` | Obrigatoria |
| `license_expires_on` | Obrigatoria |
| `is_active` | Define elegibilidade para aceite |

### `provider_vehicles`

| Campo | Regra |
|---|---|
| `id` | UUID, chave primaria |
| `provider_id` | FK obrigatoria para `providers` |
| `license_plate` | Obrigatoria, normalizada e unica |
| `vehicle_type` | Ex.: guincho plataforma, moto ou utilitario |
| `description` | Opcional |
| `capacity_kg` | Opcional e positivo |
| `is_active` | Define elegibilidade para aceite |

### `service_types`

| Campo | Regra |
|---|---|
| `id` | UUID, chave primaria |
| `code` | Unico: `TOW`, `BATTERY_REPLACEMENT`, `TIRE_CHANGE` |
| `name` | Nome exibivel |
| `description` | Opcional |
| `requires_destination` | Verdadeiro somente para reboque no MVP |
| `is_active` | Impede novas solicitacoes quando falso |

### `provider_service_types`

Relacionamento N:N entre prestadores e tipos de servico, com chave composta
`provider_id` e `service_type_id` e indicador `is_active`.

### `locations`

| Campo | Regra |
|---|---|
| `id` | UUID, chave primaria |
| `client_id` | FK opcional quando for endereco salvo do cliente |
| `label` | Ex.: Casa, Trabalho, Origem ou Destino |
| `street` | Obrigatorio |
| `number` | Opcional para locais sem numeracao |
| `complement` | Opcional |
| `district` | Opcional |
| `city` | Obrigatorio |
| `state` | UF obrigatoria |
| `postal_code` | Opcional |
| `reference` | Ponto de referencia opcional |
| `latitude` | Opcional em local salvo e obrigatoria no snapshot de origem da solicitacao |
| `longitude` | Opcional em local salvo e obrigatoria no snapshot de origem da solicitacao |
| `is_saved` | Indica endereco reutilizavel do cliente |

Locais vinculados a solicitacoes devem ser snapshots imutaveis ou copias
dedicadas, nunca referencias mutaveis que alterem o historico.

### `service_requests`

| Campo | Regra |
|---|---|
| `id` | UUID interno, chave primaria |
| `public_id` | Identificador publico unico e nao sequencial |
| `client_id` | FK obrigatoria para `clients` |
| `client_vehicle_id` | FK obrigatoria para `client_vehicles` |
| `service_type_id` | FK obrigatoria para `service_types` |
| `origin_location_id` | FK obrigatoria para snapshot em `locations` |
| `destination_location_id` | FK obrigatoria apenas para reboque |
| `provider_id` | FK opcional ate o aceite pelo motorista |
| `driver_id` | FK opcional, deve pertencer ao prestador |
| `provider_vehicle_id` | FK opcional, deve pertencer ao prestador |
| `status` | Estado valido do fluxo |
| `customer_notes` | Sintomas e instrucoes do cliente |
| `operator_notes` | Observacoes internas com acesso restrito |
| `completion_notes` | Resultado informado na conclusao |
| `assigned_at` | Preenchido no aceite pelo motorista |
| `accepted_at` | Preenchido no aceite |
| `en_route_at` | Preenchido no deslocamento |
| `started_at` | Preenchido no inicio do servico |
| `completed_at` | Preenchido na conclusao |
| `cancelled_at` | Preenchido no cancelamento |
| `version` | Controle de concorrencia otimista |

Deve existir uma restricao transacional, preferencialmente um indice unico parcial
por `provider_id` para os estados `ACCEPTED`, `EN_ROUTE` e `IN_PROGRESS`, garantindo
que um prestador possua no maximo uma solicitacao ativa mesmo sob concorrencia. Ao
voltar para `PENDING`, concluir ou cancelar, a capacidade do prestador e liberada.

### `service_request_items`

| Campo | Regra |
|---|---|
| `id` | UUID, chave primaria |
| `service_request_id` | FK obrigatoria para `service_requests` |
| `name` | Obrigatorio |
| `description` | Opcional |
| `quantity` | Decimal positivo, padrao 1 |
| `unit` | Opcional, ex.: unidade, litro ou hora |
| `notes` | Opcional |

Valores financeiros nao fazem parte deste item no MVP. Caso precificacao entre no
escopo, devera usar entidade propria para orcamento, cobranca e repasse.

### `service_request_status_history`

| Campo | Regra |
|---|---|
| `id` | UUID, chave primaria |
| `service_request_id` | FK obrigatoria |
| `from_status` | Nulo somente no evento de criacao |
| `to_status` | Obrigatorio |
| `changed_by_user_id` | Usuario responsavel |
| `reason` | Obrigatorio para liberacao ou cancelamento |
| `created_at` | Instante imutavel da transicao |

### `service_request_assignments`

Mantem o historico de aceites e liberacoes realizados pelos motoristas.

| Campo | Regra |
|---|---|
| `id` | UUID, chave primaria |
| `service_request_id` | FK obrigatoria |
| `provider_id` | FK obrigatoria |
| `driver_id` | FK opcional |
| `provider_vehicle_id` | FK opcional |
| `status` | `ACCEPTED`, `RELEASED` ou `CANCELLED` |
| `accepted_by_user_id` | Usuario do motorista que realizou o aceite |
| `accepted_at` | Instante em que o motorista obteve o atendimento |
| `released_at` | Instante opcional da liberacao |
| `release_reason` | Obrigatorio quando o motorista liberar o atendimento |

### `service_request_tracking`

Registra somente a ultima posicao no contexto de uma solicitacao ativa. Cada nova
amostra valida e mais recente substitui o estado anterior. Nao deve existir campo
de localizacao global no cadastro do motorista nem historico de rota no MVP.

| Campo | Regra |
|---|---|
| `service_request_id` | PK e FK para `service_requests`; garante uma linha por solicitacao |
| `source_event_id` | UUID do evento mais recente, unico para idempotencia |
| `assignment_id` | FK obrigatoria para a atribuicao ativa |
| `driver_id` | FK obrigatoria para o motorista atribuido |
| `latitude` | Decimal obrigatorio entre -90 e 90 |
| `longitude` | Decimal obrigatorio entre -180 e 180 |
| `accuracy_meters` | Precisao opcional, maior ou igual a zero |
| `speed_kmh` | Velocidade opcional, maior ou igual a zero |
| `heading_degrees` | Direcao opcional entre 0 e 359.99 |
| `captured_at` | Instante UTC informado pelo dispositivo |
| `received_at` | Instante UTC de recebimento pela API |

A atualizacao deve comparar `captured_at` e ocorrer por upsert atomico. Eventos
duplicados ou mais antigos nao alteram a linha atual. Como nao existe historico de
rota, nao e necessaria politica especifica de retencao dos pontos no MVP.

## Contratos REST iniciais

Os caminhos representam recursos e capacidades esperados, nao a assinatura final
de todos os DTOs. O plano deve detalhar requests, responses e exemplos OpenAPI.

| Metodo e rota | Finalidade | Papeis principais |
|---|---|---|
| `POST /api/v1/auth/register` | Autocadastrar cliente e suas credenciais | Publico |
| `POST /api/v1/auth/login` | Validar credenciais e emitir JWT e refresh token | Publico |
| `POST /api/v1/auth/refresh` | Rotacionar refresh token e renovar acesso | Publico com token valido |
| `POST /api/v1/auth/logout` | Revogar refresh token da sessao | Autenticado |
| `GET/POST /api/v1/clients` | Listar e criar clientes pela operacao | Admin, Operator |
| `GET/PATCH /api/v1/clients/{id}` | Consultar e atualizar cliente | Admin, Operator, proprio Client |
| `GET/POST /api/v1/clients/{id}/vehicles` | Gerenciar veiculos do cliente | Admin, Operator, proprio Client |
| `GET/POST /api/v1/clients/{id}/locations` | Gerenciar locais salvos | Admin, Operator, proprio Client |
| `GET/POST /api/v1/providers` | Listar e criar prestadores | Admin, Operator |
| `GET/PATCH /api/v1/providers/{id}` | Consultar e atualizar prestador | Admin, Operator, proprio Provider |
| `GET/POST /api/v1/providers/{id}/drivers` | Gerenciar motoristas | Admin, Operator, proprio Provider |
| `GET/POST /api/v1/providers/{id}/vehicles` | Gerenciar frota | Admin, Operator, proprio Provider |
| `GET/PUT /api/v1/providers/{id}/service-types` | Gerenciar servicos do parceiro | Admin, Operator |
| `GET /api/v1/service-types` | Consultar catalogo ativo | Todos autenticados |
| `POST /api/v1/service-requests` | Abrir solicitacao | Admin, Operator, Client |
| `GET /api/v1/service-requests` | Listar com filtros e paginacao | Conforme escopo do papel |
| `GET /api/v1/service-requests/available` | Listar solicitacoes elegiveis abertas | Driver |
| `GET /api/v1/service-requests/{publicId}` | Consultar detalhes e historico | Partes autorizadas |
| `POST /api/v1/service-requests/{id}/accept` | Aceitar e assumir atendimento | Driver elegivel |
| `POST /api/v1/service-requests/{id}/release` | Liberar antes do atendimento | Driver atribuido |
| `POST /api/v1/service-requests/{id}/en-route` | Informar deslocamento | Driver, Operator |
| `POST /api/v1/service-requests/{id}/start` | Iniciar atendimento | Driver, Operator |
| `POST /api/v1/service-requests/{id}/complete` | Concluir e registrar itens | Driver, Operator |
| `POST /api/v1/service-requests/{id}/cancel` | Cancelar com motivo | Conforme estado e papel |
| `POST /api/v1/service-requests/{id}/tracking/positions` | Publicar posicao GPS | Driver atribuido |
| `GET /api/v1/service-requests/{id}/tracking/latest` | Consultar ultima posicao | Partes autorizadas |
| `GET /health` | Verificar saude da API | Monitoramento |

## Componentes logicos esperados

Esta lista define responsabilidades necessarias, sem obrigar uma interface por
tabela. Interfaces devem existir apenas onde houver fronteira real de dependencia.

### Repositories ou portas de saida

- `IClientRepository`: clientes e seus dados essenciais.
- `IClientVehicleRepository`: veiculos dos clientes.
- `ILocationRepository`: locais salvos e snapshots de atendimento.
- `IProviderRepository`: prestadores, servicos, motoristas e frota ou agregados
  equivalentes, conforme o modelo de dominio final.
- `IServiceTypeRepository`: catalogo de servicos.
- `IServiceRequestRepository`: solicitacoes, atribuicoes, itens e historico.
- `ILocationTrackingRepository`: substituir e consultar a ultima posicao vinculada
  a uma solicitacao.
- Unidade de trabalho ou transacao equivalente para operacoes atomicas.

### Services ou casos de uso

- Criar e manter cliente.
- Criar e manter veiculo do cliente.
- Criar e manter local salvo.
- Credenciar e manter prestador, motorista, frota e servicos oferecidos.
- Criar solicitacao.
- Listar solicitacoes abertas elegiveis para o motorista autenticado.
- Aceitar atendimento de forma atomica, resolver concorrencia e garantir o limite
  de uma solicitacao ativa por prestador.
- Liberar atendimento aceito antes do inicio, registrando o motivo.
- Avancar estado do atendimento.
- Cancelar solicitacao.
- Concluir atendimento com itens.
- Consultar detalhes e listar solicitacoes com filtros.
- Publicar uma posicao validando motorista, atribuicao, estado e idempotencia.
- Consultar a ultima posicao e calcular se ela esta desatualizada.

## Suposicoes

- O MVP atende uma unica empresa TowFast e uma unica base operacional.
- O backend sera desenvolvido em .NET 10 com ASP.NET Core 10 por ser a stack definida
  para o projeto.
- O Supabase sera usado inicialmente como PostgreSQL gerenciado; recursos extras
  da plataforma so serao adotados se simplificarem a solucao sem acoplar o dominio.
- A autenticacao sera implementada pela API com JWT, hash seguro de senha, refresh
  token rotativo e revogacao; Supabase Auth nao sera utilizado.
- O cliente podera realizar autocadastro com CPF ou CNPJ.
- O despacho sera por autoaceite: motoristas elegiveis escolhem solicitacoes
  `PENDING`; operadores nao direcionam atendimentos no fluxo normal.
- Cada prestador pode manter somente uma solicitacao em `ACCEPTED`, `EN_ROUTE` ou
  `IN_PROGRESS`; todos os seus motoristas compartilham esse unico limite.
- Ao aceitar, o motorista seleciona um veiculo ativo pertencente ao seu prestador.
- Antes do aceite, a fila exibe tipo de servico, modelo do veiculo e local de origem
  necessario para avaliar o atendimento; contatos e outros dados pessoais do cliente
  ficam disponiveis somente apos o aceite.
- Um motorista pertence a um unico prestador por vez.
- A elegibilidade geografica usa a distancia entre a origem da solicitacao e o
  centro de atendimento do prestador, limitada por seu raio cadastrado.
- A fila do motorista e ordenada por `created_at` crescente, com `public_id` como
  criterio de desempate deterministico.
- Uma solicitacao possui um unico tipo de servico principal.
- Coordenadas geograficas podem ser opcionais em locais salvos, mas sao obrigatorias
  no snapshot de origem usado para verificar o raio de atendimento.
- Troca de bateria e troca de pneu usam apenas o modelo ja registrado no veiculo;
  nao ha campos de amperagem, aro, medida do pneu ou disponibilidade de estepe.
- O rastreamento do MVP usara publicacao e consulta periodicas via REST, com
  frequencia configuravel e sem conexao persistente obrigatoria.
- O compartilhamento com o cliente comeca em `EN_ROUTE` e termina em
  `COMPLETED` ou `CANCELLED`.
- Somente a ultima posicao do motorista sera armazenada e exibida ao cliente.
- O dispositivo do motorista sera responsavel por obter consentimento e permissao
  do sistema operacional para acessar o GPS.
- Precos e pagamentos nao fazem parte do primeiro ciclo.
- Cancelamentos nao geram taxa ou efeito financeiro no MVP.
- O item solicitado pelo usuario foi interpretado como material ou recurso
  associado ao atendimento, registrado em `service_request_items`.
- Cadastros com historico sao inativados, nunca removidos fisicamente pela API.
- Nao havera rotina automatica de retencao ou anonimizacao dos demais dados pessoais
  no MVP; o acesso continua restrito por autorizacao e os registros sao preservados.

## Decisoes confirmadas

- [x] Backend e testes usam exclusivamente .NET 10 (`net10.0`) no MVP.
- [x] Autenticacao JWT implementada pela API, sem Supabase Auth.
- [x] Cliente pode realizar autocadastro.
- [x] Cliente pode usar CPF ou CNPJ.
- [x] Motorista pertence a somente um prestador.
- [x] Prestador pode possuir somente um atendimento ativo por vez.
- [x] Area de atendimento definida por centro geografico e raio.
- [x] Fila ordenada por horario de abertura, da mais antiga para a mais recente.
- [x] Nao ha representacao de localizacao aproximada antes do aceite.
- [x] Troca de bateria e pneu exigem somente o modelo do veiculo.
- [x] A placa nao e reutilizada em outro cadastro de veiculo no MVP.
- [x] O cliente visualiza somente a ultima posicao, sem historico de rota.
- [x] Somente a ultima posicao e armazenada, sem prazo de retencao de pontos.
- [x] Nao existe intervalo fixo obrigatorio para publicacao da localizacao.
- [x] Cancelamento nao gera taxa.
- [x] Nao existe prazo automatico de retencao ou anonimizacao dos demais dados
  pessoais no MVP.

## Correcao de persistencia das transicoes

- `REQ-057`: Historicos, atribuicoes e itens criados pelo dominio com GUID proprio devem
  ser persistidos como novos registros em todas as transicoes do atendimento.
- A concorrencia otimista da solicitacao continua baseada em `Version`; somente uma
  alteracao realmente concorrente pode produzir `409` por versao desatualizada.
- Erros de persistencia nao devem expor mensagens internas do EF Core ao consumidor.

### Criterio de aceite

- `AC-028`: Depois de recarregar uma solicitacao `Pending` do PostgreSQL, aceitar ou
  cancelar grava a transicao, incrementa `Version`, adiciona o historico e deixa a
  listagem consistente. Um conflito real retorna mensagem de negocio estavel.
