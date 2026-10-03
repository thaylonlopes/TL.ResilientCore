# Arquitetura de Software — TL.ResilientCore

Guia de referência arquitetural e padrões de engenharia para microsserviços corporativos baseados na plataforma TL.ResilientCore.

---

## 1. Visão Geral

O **TL.ResilientCore** é um starter kit corporativo para desenvolvimento de microsserviços e APIs em .NET 9 de missão crítica. Sua concepção prioriza resiliência transacional, isolamento estrito de regras de negócio, performance e observabilidade.

O projeto adota os princípios de Clean Architecture (Onion/Hexagonal), CQRS com separação física de operações de leitura e escrita, Result Pattern para evitar o uso de exceções como controle de fluxo, e Outbox Pattern com índice filtrado para garantir entrega de eventos sem risco de Dual Write.

---

## 2. Estrutura em 4 Camadas

A solução é dividida em quatro projetos principais organizados em camadas concêntricas, onde a dependência aponta estritamente para o centro:

```
[ Presentation (Api) ]
          │
          ▼
 [ Infrastructure ] ──► [ Application ]
                             │
                             ▼
                         [ Domain ]
```

### 2.1. Domain (`TL.ResilientCore.Domain`)
O núcleo do sistema. Não possui nenhuma dependência de bibliotecas externas, frameworks de acesso a dados ou transporte.
- **Entidades e Value Objects:** Encapsulam regras e invariantes de negócio. Construtores privados ou protegidos forçam mutações via métodos de negócio.
- **Domain Events:** Eventos imutáveis (`record`) que representam fatos que ocorreram no domínio.
- **Interfaces de Repositório:** Contratos abstratos de persistência consumidos pelos casos de uso.
- **Pacotes Base:** Consome apenas metapacotes puramente conceituais (`TL.ExtensionLibrary.Domain`).

### 2.2. Application (`TL.ResilientCore.Application`)
Orquestra os fluxos e casos de uso da aplicação sem conhecer detalhes de implementação de banco ou infraestrutura.
- **CQRS com MediatR:**
  - *Commands:* Representam intenções de alteração de estado (`ICommand`, `ICommandHandler`).
  - *Queries:* Representam consultas somente-leitura otimizadas (`IQuery`, `IQueryHandler`).
- **Validação Automática:** Pipeline Behavior (`ValidationBehavior`) com `FluentValidation`, executando antes que o comando atinja o handler.
- **Caching CQRS Transparente:** Pipeline Behavior (`CachingBehavior`) intercepta requisições que implementam `ICachedQuery`, gerenciando chaves de cache, expiração determinística e fallback resiliente.
- **Result Pattern:** Handlers retornam instâncias de `Result` ou `Result<TValue>`, permitindo tratamento determinístico de sucesso e erro.
- **Contratos Abstratos:** Define interfaces para serviços de infraestrutura (`IClock`, `IUnitOfWork`, `IEventPublisher`, `ICacheService`).

### 2.3. Infrastructure (`TL.ResilientCore.Infrastructure`)
Implementa adaptadores externos, acesso a banco de dados e serviços de segundo plano.
- **Persistência Relacional:** Entity Framework Core configurado para PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- **Outbox Interceptor:** `InsertOutboxMessagesInterceptor` captura eventos de domínio emitidos pelas entidades durante o `SaveChangesAsync` e os persiste na tabela `OutboxMessages` dentro da mesma transação do banco.
- **Background Worker & Mensageria:** `ProcessOutboxMessagesJob` consome as mensagens pendentes e delega o despacho para `RabbitMqEventPublisher` (com publicação resiliente no RabbitMQ e fallback transparente para `MediatREventPublisher` com garantia *At-Least-Once*).
- **Building Blocks:** Integrações auxiliares de cache FinOps híbrido (`TL.Caching.Helpers`), mensageria (`TL.RabbitMQ`), paginação eficiente (`TL.KeysetPagination`) e resiliência HTTP com Polly v8 (`TL.Resilience`).

### 2.4. Presentation (`TL.ResilientCore.Api`)
Ponto de entrada HTTP do sistema via Minimal APIs (.NET 9).
- **Endpoints Desacoplados:** Rotas mapeadas por grupo de funcionalidade com mapeamento semântico de `Result` para status HTTP via extensões (`ToHttpResult`).
- **Segurança:** Autenticação via tokens JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`) integrável a provedores de identidade corporativos (`TL.IdentityHub`).
- **Documentação Interativa:** Suporte a OpenAPI e interface moderna via Scalar (`Scalar.AspNetCore`).
- **Middlewares:** Tratamento global de exceções não previstas e log de requisições (`TL.MiddlewareLibrary`, `TL.AuditLogger`).

---

## 3. Padrões Fundamentais

### CQRS (Command Query Responsibility Segregation)
- Comandos executam alterações de estado e persistem dados através de agregados de domínio e repositórios.
- Consultas são projetadas para performance, lendo dados projetados ou utilizando extensões de query (`TL.QueryableExtensionsLibrary`) sem carregar rastreamento desnecessário no contexto.

### Caching CQRS FinOps Híbrido (Memory $0 vs Redis)
- **Estratégia FinOps Híbrida:** Atende desde pequenos negócios até operações corporativas multinacionais com o mesmo contrato arquitetural:
  - *Tier Local / Startup (Custo $0):* Utiliza `IMemoryCache` nativo em memória da aplicação, eliminando custos de infraestrutura de rede e nós dedicados em ambientes de baixo volume ou desenvolvimento.
  - *Tier Enterprise / Multi-Instância:* Utiliza Redis distribuído via `TL.Caching.Helpers`, com proteção contra *Cache Stampede* (TTL Jitter decorrelacionado), compressão GZip e criptografia de payload em repouso AES-GCM com chave de 256 bits.
- **Transparência no Pipeline:** Ativado automaticamente decorando queries CQRS com a interface `ICachedQuery`. Em caso de indisponibilidade transitória do cache, o `CachingBehavior` executa degradação graciosa (*graceful fallback*) consultando a fonte original sem interromper o usuário.

### Result Pattern
- Exceções do .NET são reservadas exclusivamente para falhas catastróficas inesperadas de infraestrutura (queda de rede, falta de memória, interrupção de I/O).
- Erros de validação, entidade não encontrada e conflitos de negócio retornam `Result.Failure(Error)` com códigos e mensagens padronizados.

### Outbox Pattern Transacional e Despacho RabbitMQ
- Elimina o problema de *Dual Write*: o banco de dados e o registro do evento são atualizados na mesma transação atômica ACID.
- A tabela de Outbox possui índice filtrado (`"ProcessedOnUtc" IS NULL AND "RetryCount" < 5`), garantindo custo $O(1)$ nas consultas de polling do background job.
- **Despacho Externo com RabbitMQ:** O `ProcessOutboxMessagesJob` despacha os eventos via `RabbitMqEventPublisher`, garantindo entrega *At-Least-Once*, topologia com Dead Letter Exchange (DLX/DLQ) e fallback local determinístico via MediatR caso o broker esteja temporariamente inacessível.
- Mensagens que falham 5 vezes consecutivas permanecem na tabela para análise (Dead Letter), evitando sobrecarga contínua no worker.

### Inbox Pattern (Deduplicação Idempotente)
- Como brokers AMQP/RabbitMQ operam sob a garantia de entrega *At-Least-Once*, mensagens duplicadas podem ocorrer devido a timeouts de rede ou reconexões.
- A tabela `InboxMessages` com índice único `(Id, Type)` garante que o consumo de eventos seja estritamente idempotente via `IInboxService`, rejeitando mensagens já processadas antes de executar efeitos colaterais.

### Observabilidade e Rastreabilidade Distribuída W3C
- Utiliza `ActivitySource` nativo do .NET 9 (`TL.ResilientCore`), propagando o cabeçalho W3C `traceparent` no momento da publicação de mensagens AMQP no RabbitMQ.
- Permite correlação de ponta a ponta entre a requisição HTTP de origem e o processamento de eventos assíncronos no Jaeger, Datadog ou Grafana Tempo.

### Health Checks Cloud-Native (`TL.HealthCheck`)
- Integração nativa da biblioteca `TL.HealthCheck` expondo probes de orquestração compatíveis com Kubernetes:
  - `/livez`: Liveness probe validando a integridade do processo da aplicação.
  - `/readyz`: Readiness probe verificando a prontidão das conexões com PostgreSQL, Redis e RabbitMQ antes de rotear tráfego.

---

## 4. Gestão Centralizada de Dependências (CPM)

A solução utiliza **Central Package Management (CPM)** através do arquivo `Directory.Packages.props` na raiz:
- Todas as versões de pacotes NuGet são declaradas centralmente com `<PackageVersion Include="..." Version="..." />`.
- Os projetos da solução (`.csproj`) declaram apenas `<PackageReference Include="..." />` sem atributo de versão.
- Suporta herança condicional para integração transparente em monorepos corporativos:
  ```xml
  <Import Project="$([MSBuild]::GetPathOfFileAbove(Directory.Packages.props, $(MSBuildThisFileDirectory)..))" 
          Condition="Exists('$([MSBuild]::GetPathOfFileAbove(Directory.Packages.props, $(MSBuildThisFileDirectory)..))')" />
  ```

---

## 5. Estratégia de Testes Automatizados

A suíte de testes do template é categorizada em três níveis:
1. **Testes de Arquitetura (`TL.ResilientCore.ArchitectureTests`):** Executam regras de integridade via `NetArchTest.Rules`, assegurando que `Domain` não dependa de camadas externas, que `Domain Events` sejam imutáveis e que convenções de injeção sejam respeitadas.
2. **Testes Unitários (`TL.ResilientCore.UnitTests`):** Cobrem validações de comandos, agregados de domínio e comportamentos de handlers utilizando `xUnit` e `FluentAssertions`.
3. **Testes de Integração (`TL.ResilientCore.IntegrationTests`):** Validam o ciclo completo da API utilizando `WebApplicationFactory` e `Testcontainers.PostgreSql`, executando migrations e consultas contra containers PostgreSQL reais.

---

## 6. Governança e Licenciamento

- **Mantenedor Oficial:** [Thaylon Lopes](https://github.com/thaylonlopes)
- **Organização:** Plataforma e Padrões TL
- **Licença:** Distribuído sob os termos da licença MIT. Consulte o arquivo `LICENSE` para diretrizes de uso, modificação e atribuição de autoria.
