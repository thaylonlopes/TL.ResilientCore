# TL.ResilientCore

Template .NET 9 corporativo para microsserviços de missão crítica baseado em Clean Architecture, CQRS, Transactional Outbox/Inbox e observabilidade distribuída.

---

## 📦 Instalação e Criação de Projetos

O template é distribuído via NuGet Gallery como pacote de scaffolding do `dotnet new`.

### 1. Instalar o Template
```bash
dotnet new install TL.ResilientCore.Template::1.3.1
```

### 2. Criar um Novo Microsserviço
```bash
dotnet new tl-resilientcore -n MeuProjeto.Api
```

---

## 🏗️ Padrões e Componentes de Engenharia

* **Clean Architecture & DDD:** Isolamento concêntrico em 4 camadas (`Domain`, `Application`, `Infrastructure`, `Api`) com inversão de dependência.
* **CQRS com MediatR:** Segregação física de comandos de mutação (`ICommand`) e consultas de leitura (`IQuery`).
* **Result Pattern:** Modelagem determinística de resultados e falhas de negócio (`Result`, `Result<T>`, `Error`) eliminando o uso de exceções como controle de fluxo.
* **FinOps Caching Híbrido:** Suporte a cache em memória (`IMemoryCache`) para custo local zero e Redis corporativo distribuído via `TL.Caching.Helpers`, com TTL jitter, fallback transparente e criptografia AES-GCM.
* **Transactional Outbox com RabbitMQ:** Persistência atômica de eventos na mesma transação relacional, índice parcial no PostgreSQL (`cost O(1)`), despacho assíncrono via `TL.RabbitMQ` com Dead Letter Queue e fallback local para MediatR.
* **Transactional Inbox:** Consumo idempotente de mensagens via `IInboxService`, protegendo a aplicação contra processamento duplicado decorrente de reentregas de rede.
* **Resiliência HTTP:** Integração de políticas do Polly v8 via `TL.Resilience` para clientes HTTP externos.
* **Observabilidade e Tracing W3C:** Propagação de cabeçalhos de rastreamento distribuído `traceparent` via `ActivitySource` nativo do .NET 9.
* **Health Checks Cloud-Native:** Probes `/livez` e `/readyz` integrados via `TL.HealthCheck` para orquestração em Kubernetes e Docker.
* **Central Package Management (CPM):** Governança centralizada de versões de pacotes NuGet via `Directory.Packages.props`.

---

## 🐳 Infraestrutura Local (Docker Compose)

O repositório inclui a topologia de containers para execução e testes locais. Para provisionar o ambiente:

```bash
docker-compose up -d
```

### Serviços Provisionados
* **PostgreSQL 16:** `localhost:5432` (Banco: `TLResilientDb`, Usuário/Senha: `postgres`/`postgres`)
* **Redis 7:** `localhost:6379` (Cache distribuído)
* **RabbitMQ 3:** `localhost:5672` (Broker AMQP) e `http://localhost:15672` (Painel Web: `guest`/`guest`)

---

## 🚀 Execução Local da Solução

### 1. Restauração e Compilação
```bash
dotnet restore TL.ResilientCore.sln
dotnet build TL.ResilientCore.sln -c Release
```

### 2. Execução dos Testes Automatizados
```bash
dotnet test TL.ResilientCore.sln -c Release
```

### 3. Execução da API
```bash
dotnet run --project src/Presentation/TL.ResilientCore.Api/TL.ResilientCore.Api.csproj
```

### Probes e Documentação da API
* **Scalar OpenAPI Reference:** `https://localhost:7001/scalar/v1`
* **Especificação OpenAPI JSON:** `https://localhost:7001/openapi/v1.json`
* **Liveness Probe:** `GET /livez`
* **Readiness Probe:** `GET /readyz`

---

## 📖 Central de Documentação

### Guias e Arquitetura
* [Guia de Início Rápido (Getting Started)](docs/getting_started.md) — Configuração do ambiente local, convenções de código e passos para criação de novos endpoints.
* [Visão Geral da Arquitetura & Diagramas C4](docs/arquitetura/visao-geral.md) — Diagramas C4 (Contexto e Containers), diagramas de sequência do Outbox/Inbox e detalhamento de camadas.
* [Testes de Carga com k6 e Docker](load-tests/README.md) — Guia de execução dos cenários de estresse, concorrência e validação de latência P95.

### Decisões Arquiteturais (ADRs)
1. [ADR-000: Arquitetura Base e Convenções Herdadas](docs/adr/000-arquitetura-e-convencoes-herdados.md) — Premissas do starter kit e regras de acoplamento entre camadas.
2. [ADR-001: Uso de Índices Filtrados na Tabela Outbox](docs/adr/001-indices-filtrados-outbox.md) — Otimização de polling e Dead Letter Queue com índices parciais no PostgreSQL.
3. [ADR-002: Adoção do Result Pattern em Detrimento de Exceptions](docs/adr/002-result-pattern.md) — Padronização de erros funcionais e preservação de performance do runtime.
4. [ADR-003: Separação de Command e Query (CQRS)](docs/adr/003-cqrs-adoption.md) — Segregação de responsabilidades entre operações de escrita e leitura.
5. [ADR-004: Restrições Arquiteturais, Idempotência e Design for Failure](docs/adr/004-design-for-failure.md) — Diretrizes para falhas parciais e idempotência em microsserviços.
6. [ADR-005: Integração de Bibliotecas Reutilizáveis do Ecossistema TL](docs/adr/005-integracao-bibliotecas-reutilizaveis-ecossistema.md) — Matriz de pacotes corporativos consumidos pela solução.
7. [ADR-006: Testes de Carga Desacoplados com k6 e Docker](docs/adr/006-testes-de-carga-desacoplados-k6-docker.md) — Estrutura de validação de performance em container isolado.
8. [ADR-007: FinOps Caching em Consultas CQRS com Fallback Resiliente](docs/adr/007-finops-caching-cqrs.md) — Caching transparente de dois níveis com TTL jitter e degradação graciosa.
9. [ADR-008: Transactional Inbox Pattern e Observabilidade Cloud-Native](docs/adr/008-inbox-pattern-e-observabilidade-cloud-native.md) — Deduplicação idempotente, tracing W3C e probes de saúde.

---

## 📜 Licença e Atribuição

Distribuído sob os termos da **Licença MIT**. Desenvolvido e mantido por **[Thaylon Lopes](https://github.com/thaylonlopes)**.
Consulte o arquivo `LICENSE` para termos legais e condições de uso e distribuição.
