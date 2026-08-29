# 🛡️ TL.ResilientCore

**Enterprise .NET 9 Microservice Template** focado em alta resiliência, integridade de dados e Clean Architecture.

## 🛑 O Princípio: "Não Existe Bala de Prata"

Este template **NÃO** foi feito para criar APIs genéricas ou CRUDs simples. Ele é uma base arquitetural focada em **alta resiliência e integridade de dados**, desenhada para suportar os desafios reais de ambientes distribuídos.

### 🎯 Quando USAR este Template:

1. **Sistemas com Regras de Negócio Ricas:** Onde o domínio precisa ser isolado e o *Result Pattern* evita o custo de processamento e a poluição visual de exceções de negócio.

2. **Sistemas Baseados em Eventos (Event-Driven):** Onde perder uma mensagem entre o Banco de Dados e o Message Broker não é uma opção (*Outbox Pattern* otimizado com Índices Filtrados e Dead Letter Queue).

3. **Fluxos Assíncronos Complexos:** Onde uma falha no meio de um processo financeiro ou de alocação exige desfazer/compensar os passos anteriores (*Saga Pattern*).

4. **Carga Intensiva de Leitura/Escrita:** Onde a separação de responsabilidades via CQRS e a automatização de paginação/ordenação mantêm o código limpo e performático.

### 🚫 Quando NÃO usar este Template:

1. **CRUDs Simples / Ferramentas Internas:** Minimal APIs com rotas diretas para o banco resolvem o problema sem a necessidade do overhead do Clean Architecture.

2. **POCs ou MVPs de Descarte:** Onde a velocidade de entrega imediata (código acoplado) importa mais do que a manutenibilidade a longo prazo.

3. **Proxies / BFFs Ultra-Leves:** Aplicações de borda que apenas atuam como *pass-through* (repassam requisições) sem regras de domínio.

## 🏗️ Pilares da Arquitetura

O **TL.ResilientCore** é construído sobre pilares fundamentais para microsserviços maduros:

* **Result Pattern:** Substituição do fluxo de `try/catch` (throw exceptions) por um controle de fluxo funcional e previsível para regras de domínio.

* **Outbox Pattern Extremamente Otimizado:** Garantia de entrega (At-Least-Once) para mensageria, utilizando Índices Filtrados no PostgreSQL para buscas em `< 1ms` e limite de retries (Dead Letter).

* **CQRS + EF Core:** Segregação rigorosa de Comandos (Escrita) e Consultas (Leitura) com validação automática via FluentValidation.

* **Observabilidade e Middlewares Padronizados:** Tratamento global  (ProblemDetails) e métricas de requisição via `TL.MiddlewareLibrary`.

## 🔺 Pirâmide de Testes Automatizados

O template inclui suítes cobrindo todas as camadas:

### 1. ⚡ Testes de Arquitetura (`TL.ResilientCore.ArchitectureTests`)
Executados via **NetArchTest.Rules**:
* **`Layers/CleanArchitectureTests.cs`**: Garante isolamento estrito — `Domain` não depende de `Application` nem `Infrastructure`.
* **`NamingConventions/CqrsNamingTests.cs`**: Valida convenções de nomenclatura CQRS (`Handlers` implementando `IRequestHandler`).
* **`Design/DomainDesignTests.cs`**: Garante imutabilidade de `Domain Events` (`sealed`/`record`) e encapsulamento de construtores em `Entities`.

### 2. ⚡ Testes Unitários (`TL.ResilientCore.UnitTests`)
Executados via **xUnit** e **FluentAssertions**:
* **`Domain/ClienteTests.cs`**: Valida regras do agregado Cliente e emissão de eventos.
* **`Domain/ResultTests.cs`**: Valida comportamento das estruturas `Result` e `Error`.
* **`Domain/AggregateRootTests.cs`**: Valida o motor de captura e limpeza de `Domain Events` nos agregados.

### 3. ⚡ Testes de Integração (`TL.ResilientCore.IntegrationTests`)
Executados via **Testcontainers (PostgreSQL 16)** e **WebApplicationFactory**:
* **`Setup/IntegrationTestWebAppFactory.cs`**: Inicializa um container PostgreSQL efêmero e aplica automaticamente as migrações do EF Core via `MigrateAsync()`.
* **`Features/Database/DatabaseMigrationTests.cs`**: Valida se a base sobe zerada sem migrações pendentes.
* **`Features/Outbox/OutboxIntegrationTests.cs`**: Valida o schema e consultas da tabela `OutboxMessages`.
* **`Features/Health/ApiHealthTests.cs`**: Valida o ciclo de vida e disponibilidade HTTP do pipeline.

### 4. ⚡ Testes de Carga Desacoplados (`load-tests/`)
Executados via **k6 e Docker**:
* Validação de vazão , latência e estabilidade de conexões do PostgreSQL via `docker compose -f load-tests/docker-compose.k6.yml up`.

## 🔐 Ecossistema de Identidade (Autenticação)

Este Starter Kit está pré-configurado para validar autenticação e autorização via JWT utilizando o Keycloak. Para subir a infraestrutura de segurança localmente, utilize o nosso repositório complementar:

👉 [TL.IdentityHub - Acesse o Repositório Oficial](https://github.com/thaylonmayk/TL.IdentityHub)

## 🚀 Como utilizar (Localmente)

*Este template foi desenhado para ser empacotado via CLI do .NET.*

Primeira vez gerando um projeto com este template? Leia o nosso [Guia de Início Rápido (Getting Started)](docs/getting_started.md) para entender como estruturar suas features, regras de domínio e fluxo de trabalho.

**1. Instale o template via NuGet:**

```bash
dotnet new install TL.ResilientCore.Template
```

**2. Crie um novo microsserviço:**

```bash
dotnet new tl-resilientcore -n NomeDoSeuMicrosservico
```

**3. Suba a infraestrutura local (Postgres e Redis):**

```bash
docker-compose up -d
```

**4. Crie e aplique as Migrations do EF Core:**

- obs.: Se você não tiver a ferramenta do EF instalada na máquina, rode primeiro:

```bash
dotnet tool install --global dotnet-ef
```

# Criar a Migration inicial (as tabelas no EF Core)
```bash
dotnet ef migrations add InitialCreate --project src/Infrastructure/TL.ResilientCore.Infrastructure/TL.ResilientCore.Infrastructure.csproj --startup-project src/Presentation/TL.ResilientCore.Api/TL.ResilientCore.Api.csproj
```
# Aplicar a Migration no banco que subiu no Docker
```bash
dotnet ef database update --project src/Infrastructure/TL.ResilientCore.Infrastructure/TL.ResilientCore.Infrastructure.csproj --startup-project src/Presentation/TL.ResilientCore.Api/TL.ResilientCore.Api.csproj
```

## 📂 Estrutura do Projeto (Blueprint)

```text
src/
 ├── Core/
 │    ├── Domain/         # Entidades, Agregados, Domain Events e Result Pattern
 │    └── Application/    # CQRS (Commands/Queries), Sagas e Interfaces
 ├── Infrastructure/
 │    ├── Persistence/    # EF Core DbContext, Interceptors, Repositories
 │    └── Outbox/         # Background Workers, Polly Pipelines
 └── Presentation/
      └── Api/            # Controllers / Minimal APIs, Middlewares de Tratamento
```

## 📖 Decisões de Arquitetura (ADRs)

Documentamos o "porquê" de cada decisão técnica tomada neste template. Consulte a pasta `/docs/adr` para entender os *trade-offs*:

* [ADR 000: Arquitetura Base e Convenções](docs/adr/000-arquitetura-e-convencoes-herdados.md)
* [ADR 001: Uso de Índices Filtrados na Tabela Outbox](docs/adr/001-indices-filtrados-outbox.md)
* [ADR 002: Adoção do Result Pattern em detrimento de Exceptions](docs/adr/002-result-pattern.md)
* [ADR 003: Separação de Command e Query (CQRS)](docs/adr/003-cqrs-adoption.md)
* [ADR 004: Restrições Arquiteturais, Idempotência e Anti-Patterns](docs/adr/004-design-for-failure.md)
* [ADR 005: Adoção de Bibliotecas Reutilizáveis do Ecossistema TL via NuGet](docs/adr/005-integracao-bibliotecas-reutilizaveis-ecossistema.md)
* [ADR 006: Estrutura de Testes de Carga Desacoplados com k6 e Docker](docs/adr/006-testes-de-carga-desacoplados-k6-docker.md)