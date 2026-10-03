# 🏛️ Visão Geral da Arquitetura — TL.ResilientCore

O **TL.ResilientCore** é um Enterprise Microservice Starter Kit e Template para .NET 9 projetado sob os princípios de **Clean Architecture**, **Domain-Driven Design (DDD)**, **Command Query Responsibility Segregation (CQRS)**, **Design for Failure** e **Reutilização Inteligente de Bibliotecas**. 

O template foi concebido para cenários de alta criticidade, integridade transacional estrita e ambientes distribuídos orientados a eventos, eliminando falhas silenciosas e gargalos clássicos de microsserviços.

---

## 🎯 1. Diagrama C4 — Nível 1: Contexto do Sistema (System Context)

O diagrama abaixo ilustra os limites do sistema gerado pelo `TL.ResilientCore`, seus consumidores, serviços de suporte e integrações externas:

```mermaid
C4Context
    title Diagrama de Contexto de Sistema (C4 Level 1) - TL.ResilientCore

    Person(usuario, "Cliente / Consumidor", "Aplicações SPA, Mobile ou Clientes de API que consomem os serviços.")
    System_Ext(identityHub, "TL.IdentityHub (Keycloak)", "Provedor OpenID Connect / OAuth2 para autenticação e emissão de JWT.")
    
    System(resilientApi, "TL.ResilientCore Microservice", "Microsserviço corporativo resiliente em .NET 9 baseado em Clean Architecture e CQRS.")
    
    SystemDb_Ext(postgres, "PostgreSQL 16", "Banco de dados relacional principal armazenando agregados e a tabela de Outbox.")
    SystemDb_Ext(redis, "Redis Cache", "Cache distribuído em memória para otimização de consultas e controle de taxa/sessão.")
    SystemQueue_Ext(messageBroker, "Message Broker (Kafka / RabbitMQ)", "Barramento de eventos assíncronos para integração entre microsserviços.")
    System_Ext(servicoExterno, "Serviços Externos / APIs Terceiras", "Gateways de pagamento, ERPs e APIs parceiras protegidas por Polly.")

    Rel(usuario, identityHub, "Autentica-se e obtém Bearer Token", "HTTPS/OIDC")
    Rel(usuario, resilientApi, "Executa Comandos e Consultas", "HTTPS / JSON / JWT")
    Rel(resilientApi, identityHub, "Valida Assinatura do Token JWT e Claims", "JWKS / HTTPS")
    Rel(resilientApi, postgres, "Persiste entidades e mensagens Outbox na mesma transação", "TCP / Npgsql")
    Rel(resilientApi, redis, "Consulta e armazena cache de leituras", "RESP / TCP")
    Rel(resilientApi, messageBroker, "Publica eventos de domínio garantidos via Outbox Job", "AMQP / Kafka Protocol")
    Rel(resilientApi, servicoExterno, "Integração HTTP resiliente com Retry & Circuit Breaker", "HTTPS / Polly")
```

---

## 📦 2. Diagrama C4 — Nível 2: Containers & Componentes (Containers & Architecture)

A estrutura interna do `TL.ResilientCore` segue o isolamento concêntrico da **Clean Architecture**, onde o Domínio é o núcleo inviolável e a Infraestrutura e Apresentação integram as bibliotecas reutilizáveis do ecossistema:

```mermaid
C4Container
    title Diagrama de Containers (C4 Level 2) - TL.ResilientCore

    Container_Boundary(c1, "TL.ResilientCore Application") {
        Component(api, "TL.ResilientCore.Api", "ASP.NET Core Minimal APIs", "Endpoints HTTP, MiddlewareLibrary (Correlation ID, RFC 7807), Scalar OpenAPI e TL.ClaimsPrincipalExtensionsLibrary.")
        Component(app, "TL.ResilientCore.Application", "MediatR & FluentValidation", "Comandos (ICommand), Consultas (IQuery), Handlers, ValidationBehavior e QueryableExtensionsLibrary.")
        Component(dom, "TL.ResilientCore.Domain", "Pure C# Domain Model", "Entidades, Agregados, Domain Events (sealed), Result Pattern e TL.EnumExtensionsLibrary.")
        Component(infra, "TL.ResilientCore.Infrastructure", "EF Core 9, Npgsql, BackgroundServices", "ApplicationDbContext, Outbox Interceptor, ProcessOutboxMessagesJob, Dapper.Helpers, Caching.Helpers e HealthCheck.")
    }

    ContainerDb(db, "PostgreSQL Database", "PostgreSQL 16", "Esquema relacional de negócio + Tabela OutboxMessages com Filtered Index.")
    ContainerDb(redisCache, "Redis Server", "Redis 7+", "Cache distribuído e controle de estado.")

    Rel(api, app, "Despacha Comandos/Queries", "MediatR ISender")
    Rel(api, dom, "Mapeia Erros para Respostas HTTP", "ResultExtensions")
    Rel(app, dom, "Invoca comportamentos e invariantes", "C# In-Memory")
    Rel(infra, app, "Implementa contratos e interfaces", "IUnitOfWork, IApplicationDbContext")
    Rel(infra, dom, "Mapeia entidades para persistência", "EF Core Fluent API")
    Rel(infra, db, "Lê/Escreve dados transacionais e Outbox", "Npgsql Connection Pool")
    Rel(infra, redisCache, "Operações de Cache via Caching.Helpers", "StackExchange.Redis")
```

---

## 🌐 3. Pipeline HTTP de Apresentação e Middlewares

O pipeline de requisições na camada `Presentation.Api` segue uma ordem estrita de execução garantindo observabilidade e padronização:

```mermaid
sequenceDiagram
    autonumber
    actor Cliente as Cliente HTTP
    participant Corr as CorrelationIdMiddleware (MiddlewareLibrary)
    participant Log as RequestResponseLoggingMiddleware
    participant Ex as GlobalExceptionMiddleware (RFC 7807)
    participant Auth as Authentication / Authorization (JWT)
    participant Endp as Minimal API Endpoint
    participant MediatR as MediatR Pipeline (ValidationBehavior)

    Cliente->>Corr: HTTP Request
    Corr->>Corr: Extrai ou gera X-Correlation-Id
    Corr->>Log: Passa requisição
    Log->>Log: Registra payload de entrada e contexto
    Log->>Ex: Passa requisição
    Ex->>Auth: Passa requisição
    Auth->>Auth: Valida Token JWT e Claims
    Auth->>Endp: Rota autenticada
    Endp->>MediatR: sender.Send(Command/Query)
    MediatR-->>Endp: Result / Result<T>
    Endp-->>Ex: HTTP Response via ResultExtensions
    Ex-->>Log: HTTP Response
    Log->>Log: Registra tempo de execução e status code
    Log-->>Corr: HTTP Response com X-Correlation-Id
    Corr-->>Cliente: Resposta final
```

---

## 🔄 4. Fluxo de Execução e Garantia Transacional (Outbox Pattern)

O grande diferencial do `TL.ResilientCore` é a garantia de entrega **At-Least-Once** sem introduzir o risco de *Dual Write* ou travamentos no banco.

```mermaid
sequenceDiagram
    autonumber
    actor Cliente as Cliente HTTP
    participant API as Api Endpoint (Minimal API)
    participant Pipe as Pipeline Behavior (Validation)
    participant Handler as Command Handler (Application)
    participant Domain as Agregado / Entidade (Domain)
    participant UoW as DbContext & Outbox Interceptor
    participant DB as PostgreSQL (OutboxMessages)
    participant Job as ProcessOutboxMessagesJob (Worker)
    participant MediatR as INotification Publisher

    Cliente->>API: POST /recurso (Bearer JWT)
    API->>Pipe: sender.Send(Command)
    Pipe->>Pipe: Executa FluentValidation (se falhar, aborta com Result.Failure)
    Pipe->>Handler: Invoca Handle()
    Handler->>Domain: Executa Regra de Negócio
    Domain->>Domain: Protege Invariantes & Registra DomainEvent
    Domain-->>Handler: Retorna Result.Success(entity)
    Handler->>UoW: SaveChangesAsync()
    UoW->>UoW: InsertOutboxMessagesInterceptor extrai DomainEvents
    UoW->>DB: Salva Entidade + OutboxMessages na MESMA TRANSAÇÃO SQL
    UoW-->>Handler: Transação Confirmada (Commit)
    Handler-->>API: Result.Success()
    API-->>Cliente: 200 OK / 201 Created (via ResultExtensions)

    Note over Job,DB: Processamento Assíncrono Contínuo (< 1ms com Filtered Index)
    loop A cada 5-10s (Background Worker)
        Job->>DB: SELECT * FROM OutboxMessages WHERE ProcessedOnUtc IS NULL ORDER BY OccurredOnUtc LIMIT 20
        DB-->>Job: Mensagens Pendentes
        Job->>MediatR: publisher.Publish(DomainEventNotification)
        Job->>DB: UPDATE OutboxMessages SET ProcessedOnUtc = UtcNow WHERE Id = @Id
    end
```

---

## 🧱 5. Detalhamento das Camadas e Bibliotecas Integradas

### 5.1. Camada de Domínio (`TL.ResilientCore.Domain`)
* **Isolamento Total**: Depende exclusivamente do runtime básico do .NET e bibliotecas utilitárias puras (`TL.EnumExtensionsLibrary`). Zero dependência de frameworks web ou ORMs.
* **Entidades Ricas (`Entity`, `AggregateRoot`)**: Construtores públicos são proibidos (testado via NetArchTest). O acesso e mutação de estado ocorrem estritamente por métodos de domínio que protegem suas invariantes.
* **Domain Events Imutáveis (`IDomainEvent`)**: Eventos são definidos como `sealed record`, garantindo imutabilidade e identificadores únicos (`EventId`, `OccurredOnUtc`).
* **Result Pattern (`Result`, `Result<T>`, `Error`)**: Todas as operações de negócio retornam `Result` ou `Result<T>`. Lançamento de exceções (`throw`) é proibido para regras de validação e restrições de negócio (vide [ADR-002](../adr/002-result-pattern.md)).

### 5.2. Camada de Aplicação (`TL.ResilientCore.Application`)
* **CQRS Puro**: Segregação de `ICommand` / `ICommandHandler` para mutações e `IQuery` / `IQueryHandler` para leituras.
* **Pipeline Behaviors**: O `ValidationBehavior<TRequest, TResponse>` intercepta comandos antes da execução, executa os validadores do `FluentValidation` e retorna erros estruturados caso existam inconsistências.
* **Extensões de Consulta LINQ**: `QueryableExtensionsLibrary` fornece métodos de paginação e filtragem condicional (`.ApplyFilterIf()`, `.ToPagedListAsync()`).

### 5.3. Camada de Infraestrutura (`TL.ResilientCore.Infrastructure`)
* **Entity Framework Core 9**: Mapeamento objeto-relacional com PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`.
* **Outbox Pattern com Índices Filtrados**:
  * Configuração: `builder.HasIndex(x => x.OccurredOnUtc).HasFilter("\"ProcessedOnUtc\" IS NULL");`
  * O índice cobre exclusivamente mensagens pendentes, garantindo tempo de resposta constante `< 1ms` mesmo após milhões de registros históricos processados (vide [ADR-001](../adr/001-indices-filtrados-outbox.md)).
* **Background Worker (`ProcessOutboxMessagesJob`)**: `BackgroundService` que realiza polling em lotes com tratamento de exceções catastróficas e deserialização tipada de eventos.
* **Dapper & Cache**: Utilização de `CommonHelpers.Data.Dapper` para consultas ultra-rápidas e `DataHelpers.Caching.Helpers` para cache distribuído em Redis.
* **HealthCheck Probes**: Diagnóstico de conectividade com banco e cache via `CommonHelpers.HealthCheck`.

### 5.4. Camada de Apresentação (`TL.ResilientCore.Api`)
* **Minimal APIs Modernas**: Roteamento enxuto de alta performance.
* **Middlewares Corporativos**: `MiddlewareLibrary` configurando Correlation ID, logging estruturado e tratamento global RFC 7807 (ProblemDetails).
* **Scalar OpenAPI Reference**: Documentação interativa em `/scalar/v1` em substituição ao Swagger UI tradicional.
* **ResultExtensions**: Conversor funcional que transforma `Result` em `Results.Ok()`, `Results.BadRequest()` ou `Results.NotFound()` sem necessidade de blocos `try/catch`.
* **Autenticação JWT (Keycloak / TL.IdentityHub)**: Configuração padrão para consumo de tokens Bearer com extração de claims via `TL.ClaimsPrincipalExtensionsLibrary`.

---

## 🔺 6. Estratégia de Testes Automatizados

O template implementa a pirâmide completa de testes automatizados:

| Projeto | Tecnologia | Responsabilidade |
| :--- | :--- | :--- |
| **`TL.ResilientCore.ArchitectureTests`** | NetArchTest.Rules + xUnit | Valida o isolamento de camadas da Clean Architecture, garantia de imutabilidade dos Domain Events, convenções de sufixo CQRS e proteção de construtores de entidades. |
| **`TL.ResilientCore.UnitTests`** | xUnit + FluentAssertions | Testa comportamentos do Result Pattern, agregação de Domain Events e regras isoladas de domínio. |
| **`TL.ResilientCore.IntegrationTests`** | Testcontainers (PostgreSQL 16) + WebApplicationFactory | Sobe instâncias reais e efêmeras do PostgreSQL via Docker, aplica migrações EF Core no boot, e valida o fluxo HTTP da API e integridade da tabela Outbox. |

---

## 🛡️ 7. Decisões Arquiteturais Registradas (ADRs)

1. [ADR-000: Arquitetura Base e Convenções](../adr/000-arquitetura-e-convencoes.md)
2. [ADR-001: Uso de Índices Filtrados na Tabela Outbox](../adr/001-indices-filtrados-outbox.md)
3. [ADR-002: Adoção do Result Pattern em detrimento de Exceptions](../adr/002-result-pattern.md)
4. [ADR-003: Separação de Command e Query (CQRS)](../adr/003-cqrs-adoption.md)
5. [ADR-004: Restrições Arquiteturais, Idempotência e Anti-Patterns](../adr/004-design-for-failure.md)
