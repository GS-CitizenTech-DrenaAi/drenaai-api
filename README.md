# DrenaAi — Plataforma de Zeladoria Urbana e Prevenção de Enchentes

## Índice

| [Motivação](#motivação-do-projeto) | [Integrantes](#integrantes--3espg) | [Evidência](#evidência-da-aplicação) | [Integração](#integração-com-o-projeto) | [Tecnologias](#estruturas-arquitetura-e-tecnologias) | [Endpoints](#endpoints-da-api) | [Como Executar](#como-executar-o-projeto) | [Testes Automatizados](#testes-automatizados) |

## Motivação do Projeto

O **DrenaAi** é uma solução CitizenTech voltada à zeladoria urbana preventiva e à mitigação de enchentes na cidade de São Paulo. Moradores de áreas vulneráveis convivem com o acúmulo de resíduos sólidos em vias públicas e o entupimento de bueiros, fatores que agravam alagamentos durante períodos de chuva intensa.

Em vez de tratar cada denúncia por simples ordem de chegada, o núcleo da plataforma cruza o relato visual do cidadão com leituras contínuas de sensores instalados em bueiros e com previsões climáticas de risco. A partir desse cruzamento, a API calcula dinamicamente o **Índice de Prioridade de Coleta (IPC)** e reordena a fila operacional para que as equipes de limpeza atuem primeiro onde a probabilidade de alagamento é maior.

O projeto está alinhado aos Objetivos de Desenvolvimento Sustentável da ONU, com foco no **ODS 11 (Cidades e Comunidades Sustentáveis)**, apoiando a gestão de resíduos e a prevenção de desastres urbanos, e no **ODS 13 (Ação contra a Mudança Global do Clima)**, fortalecendo a resiliência da infraestrutura urbana diante de eventos climáticos extremos.

---

## Integrantes — 3ESPG
* João Victor Soave **(RM557595)**
* Maria Alice Freitas Araújo **(RM557516)**
* Pedro Henrique Mendes dos Santos **(RM555332)**
* Rafael Teofilo Lucena **(RM555600)**
* Vinicius Fernandes Tavares Bittencourt **(RM558909)**

---

## Evidência da Aplicação

**[Assista ao vídeo no YouTube](https://www.youtube.com/watch?v=dxE7q2-48ik)**: Backend C# respondendo às requisições feitas pelo app DrenaAi.

---

## Integração com o Projeto

A Web API desenvolvida em C# (.NET 10) atua como o barramento central da arquitetura orientada a serviços do DrenaAi, conectando quatro frentes da solução:

1. **Canal Mobile (Aplicativo do Cidadão):** Aplicativo em React Native (Expo) pelo qual o morador realiza cadastro, autentica-se via JWT e envia ocorrências com foto, categoria, descrição e localização.
2. **Canal IoT Físico (ESP32):** Dispositivos embarcados instalados nos bueiros coletam periodicamente a distância da coluna de água/resíduos (sensor ultrassônico) e a concentração de gases (`gasPpm`), enviando os pacotes para a API com autenticação por chave de dispositivo (`X-Device-Api-Key`).
3. **Canal de Visão Computacional:** Serviço de IA que analisa a imagem enviada pelo morador e retorna a severidade normalizada da obstrução, o nível de confiança e a justificativa técnica.
4. **Canal de Machine Learning Climático e Topográfico:** Serviço preditivo que informa o volume de chuva previsto para 12h e 24h, o risco de alagamento e o índice de vulnerabilidade da via no bairro consultado.

### Motor de Priorização Dinâmica (Regras de Negócio e IPC)

Sempre que uma ocorrência é registrada pelo aplicativo (`APP`) ou aberta automaticamente por uma leitura crítica do sensor (`SENSOR`), o serviço `CalculadoraIpcService` calcula o Índice de Prioridade de Coleta pela fórmula:

```text
IPC = (Ws * S) + (Wc * C) + (Wv * V)
```

Onde:
* **S (Severidade, peso padrão 0,4):** Valor normalizado entre `0.0` e `1.0`, obtido pelo modelo de visão computacional no fluxo do aplicativo ou calculado a partir de `distanciaCm` e `gasPpm` no fluxo do sensor ESP32.
* **C (Chuva Normalizada, peso padrão 0,3):** Calculada como `min(chuvaMm12h / TetoChuvaMm, 1.0)`, utilizando `TetoChuvaMm = 50.0` mm por padrão.
* **V (Vulnerabilidade da Via, peso padrão 0,3):** Índice entre `0.0` e `1.0` retornado pelo modelo de risco climático/topográfico.

Todos os pesos e o teto de chuva são lidos da configuração via `IOptions<IpcSettings>`, evitando valores fixos no código. Na consulta da fila (`GET /api/fila`), a ordenação prioriza o maior `Ipc` e utiliza a data de criação (`CriadaEm`) crescente como critério de desempate. No cenário de referência da aplicação, uma ocorrência recente com `S = 0.75`, `chuvaMm12h = 35` mm (`C = 0.70`) e `V = 0.90` resulta em `IPC = 0.78`, assumindo o topo da fila acima de uma ocorrência anterior com `S = 0.50`, `chuvaMm12h = 0` mm (`C = 0.00`) e `V = 0.20` (`IPC = 0.26`).

---

## Estruturas, Arquitetura e Tecnologias

A solução `DrenaAi.sln` é organizada em dois projetos na raiz do diretório `DrenaAi`: `DrenaAi.Api` (Web API em .NET 10) e `DrenaAi.Tests` (suíte de testes automatizados).

### Estruturação do Domínio e Separação de Contextos
O código da API segue uma organização modular por contextos de negócio na pasta `Contexts/`:
* `Contexts/Ocorrencias`: Entidades (`Ocorrencia`, `HistoricoStatus`), Value Object (`Endereco`), enums (`OrigemOcorrencia`, `StatusOcorrencia`), DTOs, `IOcorrenciaService` e `OcorrenciaService`.
* `Contexts/Usuarios`: Entidade `Usuario`, DTOs de autenticação, `JwtSettings`, `IAuthService` e `AuthService`.
* `Contexts/Priorizacao`: `ICalculadoraIpcService`, `CalculadoraIpcService`, `IIntegracaoIaService`, implementações `IntegracaoIaHttpService` e `IntegracaoIaMockService`, `IpcSettings`, `IaServicesSettings` e DTOs de priorização.
* `Contexts/Telemetrias`: Entidade `SensorBueiro`, `ITelemetriaService`, `TelemetriaService` e DTOs de cadastro, consulta e leitura IoT.

### Modelagem de Domínio e Programação Orientada a Objetos (POO)
As entidades de domínio (`Ocorrencia`, `Usuario`, `HistoricoStatus`, `SensorBueiro`) aplicam encapsulamento com propriedades de escrita privada (`private set`), construtores parametrizados e métodos de comportamento para regras de negócio, como cálculo/atualização de IPC, registro de leituras com cálculo de obstrução e transição controlada de status com registro automático na linha do tempo (`HistoricoStatus`). O endereço e as coordenadas geográficas são modelados como um Value Object dedicado (`Endereco`).

### Abstração, Interfaces e Injeção de Dependência (DI)
Os controladores dependem exclusivamente de abstrações definidas por interfaces, registradas no container nativo de injeção de dependência em `Program.cs`:
* `IAuthService` -> `AuthService`
* `IOcorrenciaService` -> `OcorrenciaService`
* `ITelemetriaService` -> `TelemetriaService`
* `ICalculadoraIpcService` -> `CalculadoraIpcService`
* `IPiiSanitizerService` -> `PiiSanitizerService`
* `IIntegracaoIaService` -> `IntegracaoIaMockService` ou `IntegracaoIaHttpService` (alternado via configuração `IaServices:UseMocks`)

### Entity Framework Core, LINQ e Oracle Database
A persistência utiliza **Entity Framework Core** com o provedor **Oracle.EntityFrameworkCore** (compatibilidade Oracle 19c) configurado no `AppDbContext`. O mapeamento relacional define as tabelas `TB_DRENA_USUARIO`, `TB_DRENA_OCORRENCIA`, `TB_DRENA_HISTORICO_STATUS` e `TB_DRENA_SENSOR_BUEIRO`, além do mapeamento do Value Object `Endereco` via `OwnsOne` e conversão de enums para texto. As consultas utilizam LINQ assíncrono com `Where`, `Include`, `OrderByDescending`, `ThenBy` e `AsNoTracking`.

### Migrações (Migrations)
O controle de versão do esquema do banco de dados está estruturado em `DrenaAi.Api/Data/Migrations`, contendo:
* `InitialCreate`: criação das tabelas iniciais de usuários, ocorrências e histórico de status.
* `AddEnderecoValueObject`: evolução do esquema para persistência do Value Object `Endereco` na tabela de ocorrências.
* `AddTabelaSensorBueiro`: criação da tabela `TB_DRENA_SENSOR_BUEIRO` para persistência dos bueiros monitorados por sensores IoT e suas últimas leituras.
* `AddConfirmacoesOcorrencia`: adição da coluna `CONFIRMACOES` em `TB_DRENA_OCORRENCIA` para consolidação de denúncias no mesmo local.

### Programação Assíncrona (Async/Await)
Toda a cadeia de I/O — das ações nos Controllers até os Services, consultas do Entity Framework Core (`ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `SaveChangesAsync`) e requisições HTTP externas — utiliza `async`/`await` com propagação de `CancellationToken`.

### Tratamento Global de Exceções (Middleware) e Segurança
* **Middleware Global:** `GlobalExceptionMiddleware` intercepta exceções no pipeline HTTP e retorna JSON padronizado (`statusCode`, `erro`, `timestamp`), mapeando `DomainException` para HTTP `400`, `UnauthorizedException` para HTTP `401`, `NotFoundException` para HTTP `404` e falhas inesperadas para HTTP `500`.
* **DTOs Imutáveis:** Os payloads de entrada e saída são definidos como `record` com validações via Data Annotations, evitando a exposição direta das entidades de domínio.
* **Autenticação e Sanitização de PII:** O acesso mobile utiliza **JWT Bearer** com armazenamento de senha em hash (`SenhaHash`). O acesso dos sensores utiliza o filtro `ApiKeyAuthFilter` validando o cabeçalho `X-Device-Api-Key`. Descrições enviadas pelos moradores passam pelo `PiiSanitizerService` antes de serem persistidas ou registradas em logs, removendo padrões de CPF, e-mail, telefone, senha e termos de saúde.

### Documentação Automática (Swagger / OpenAPI)
O projeto habilita `<GenerateDocumentationFile>true</GenerateDocumentationFile>` no `DrenaAi.Api.csproj` e expõe documentação XML completa em todas as classes, métodos, parâmetros e DTOs. O OpenAPI nativo do .NET 10 e o **Swagger UI** exibem os contratos dos endpoints e permitem testar requisições autenticadas com `Bearer` JWT e `X-Device-Api-Key`.

---

## Endpoints da API

### Autenticação (`/api/auth`)
* `POST /api/auth/cadastro` — Cadastra um novo morador (`nome`, `email`, `senha`) e retorna `201 Created`.
* `POST /api/auth/login` — Autentica um morador cadastrado e retorna `200 OK` com o token JWT.

### Ocorrências (`/api/ocorrencias`) — Protegido por JWT
* `POST /api/ocorrencias/foto` — Recebe arquivo de imagem via multipart/form-data, armazena em `wwwroot/uploads` e retorna a URL pública.
* `POST /api/ocorrencias` — Registra uma nova ocorrência reportada pelo morador, sanitiza a descrição, consulta severidade/risco, calcula o IPC e retorna `201 Created`.
* `GET /api/ocorrencias` — Lista ocorrências com filtros opcionais por query (`?bairro=&status=`).
* `GET /api/ocorrencias/{id}` — Retorna os detalhes completos da ocorrência, endereço e linha do tempo `historicoStatus`.
* `PATCH /api/ocorrencias/{id}/status` — Atualiza o status da ocorrência (webhook de zeladoria) e registra novo item no histórico.

### Telemetria IoT e Painel de Sensores (`/api/telemetria`)
* `POST /api/telemetria/sensores` — Protegido pelo cabeçalho `X-Device-Api-Key`. Cadastra um novo bueiro monitorado (`logradouro`, `bairro`, `latitude`, `longitude` e leitura inicial opcional), gerando o `id` (`Guid`) automaticamente e retornando `201 Created`.
* `GET /api/telemetria/sensores` — Lista todos os bueiros monitorados com localização, percentual de obstrução, status de gás e última leitura (com filtro opcional `?bairro=`).
* `GET /api/telemetria/sensores/{bueiroId}` — Consulta os detalhes e a última leitura de um bueiro monitorado específico.
* `POST /api/telemetria` — Protegido pelo cabeçalho `X-Device-Api-Key`. Recebe leitura recorrente do ESP32 (`bueiroId`, `distanciaCm`, `gasPpm`, com `sensorId` e `timestamp` opcionais registrados pelo servidor), atualiza o painel do sensor e cria ocorrência automática de origem `SENSOR` quando os níveis são críticos (`202 Accepted` ou `404 Not Found` se o bueiro não estiver cadastrado).

### Fila de Priorização e Previsão (`/api/fila` e `/api/previsao`)
* `GET /api/fila` — Retorna a fila de coleta ordenada por `Ipc` decrescente e `CriadaEm` crescente, com filtro opcional `?bairro=`.
* `POST /api/fila/priorizar` — Webhook que recalcula o IPC de uma ocorrência (`ocorrenciaId`) e retorna a fila reordenada com registro de notificação.
* `GET /api/previsao` — Consulta a previsão de chuva (`chuvaMm12h`, `chuvaMm24h`), `riscoAlagamento` e vulnerabilidade da via para o `?bairro=` informado.

### Severidade por Visão Computacional (`/api/severidade`)
* `POST /api/severidade` — Avalia a referência de uma imagem (`foto`) e retorna `severidade`, `confianca` e `justificativa`.

---

## Como Executar o Projeto

### Pré-requisitos
* SDK do **.NET 10** (`net10.0`)
* Instância do **Oracle Database** acessível para persistência (ou execução da suíte de testes com provedor em memória)

### 1. Configurando o `appsettings.Development.json`
O arquivo `DrenaAi.Api/appsettings.json` versionado contém apenas placeholders estruturais, sem credenciais reais. Crie o arquivo `DrenaAi.Api/appsettings.Development.json` (ignorado pelo `.gitignore`) com as configurações locais:

```json
{
  "ConnectionStrings": {
    "OracleConnection": "Data Source=HOST:PORTA/SERVICO;User Id=USUARIO;Password=SENHA;"
  },
  "JwtSettings": {
    "SecretKey": "SUA_CHAVE_SECRETA_COM_PELO_MENOS_32_CARACTERES",
    "Issuer": "DrenaAi.Api",
    "Audience": "DrenaAi.Mobile",
    "ExpiracaoHoras": 8
  },
  "DeviceAuth": {
    "ApiKey": "SUA_API_KEY_DO_ESP32"
  },
  "IpcSettings": {
    "PesoSeveridade": 0.4,
    "PesoChuva": 0.3,
    "PesoVulnerabilidade": 0.3,
    "TetoChuvaMm": 50.0
  },
  "IaServices": {
    "VisionBaseUrl": "http://localhost:8001",
    "MlFloodRiskBaseUrl": "http://localhost:8002",
    "UseMocks": true
  }
}
```

### 2. Aplicando as Migrations e executando no terminal
Posicione o terminal na pasta `DrenaAi` e execute:

```bash
# Restaura os pacotes NuGet da solução
dotnet restore

# Aplica as migrations no banco Oracle configurado (opcional se o banco já estiver atualizado)
dotnet ef database update --project DrenaAi.Api

# Compila a solução e inicia a API
dotnet run --project DrenaAi.Api --launch-profile http
```

### 3. Documentação Interativa
Com a aplicação em execução, acesse:
* **Swagger UI:** `/swagger`
* **Especificação OpenAPI:** `/openapi/v1.json`

---

## Testes Automatizados

O projeto `DrenaAi.Tests` implementa testes unitários e de integração utilizando **xUnit**, **Moq**, **FluentAssertions**, **Entity Framework Core InMemory** e **WebApplicationFactory**.

Para executar toda a suíte de testes a partir da pasta `DrenaAi`:

```bash
dotnet test
```

Principais cenários validados pela suíte:
* `CalculadoraIpcServiceTests`: validação da fórmula do IPC, normalização do teto de chuva e cenário de referência (`IPC 0.78` vs `IPC 0.26`).
* `OcorrenciaServiceTests`: criação de ocorrência, ordenação da fila por `Ipc` decrescente e `CriadaEm` crescente, detalhamento e atualização de status com histórico.
* `TelemetriaServiceTests`: ingestão de leituras ESP32 e abertura automática de ocorrência `SENSOR` em condições críticas.
* `AuthServiceTests`: cadastro com hash de senha, validação de duplicidade de e-mail e emissão de token JWT.
* `PiiSanitizerServiceTests`: remoção de CPF, e-mail, telefone, senhas e termos sensíveis em descrições livres.
* `IntegracaoIaServiceTests`: contratos de avaliação de severidade e previsão climática.
* `ApiSecurityAndEndpointsTests`: testes de integração HTTP validando proteção JWT, validação de `X-Device-Api-Key`, tratamento global de erros e fluxo completo dos endpoints.
