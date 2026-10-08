# Guia de Início Rápido — TL.ResilientCore

Guia de configuração inicial, convenções de desenvolvimento e execução local para projetos gerados a partir do template `TL.ResilientCore`.

---

## 1. Ambiente Local e Dependências (Docker Compose)

Antes de iniciar a aplicação, inicialize os serviços de banco de dados, cache e mensageria via Docker Compose na raiz do projeto:

```bash
docker-compose up -d
```

### Serviços Provisionados
* **PostgreSQL 16:** Porta `5432` (Banco: `TLResilientDb`, Usuário/Senha: `postgres`/`postgres`)
* **Redis 7:** Porta `6379` (Cache distribuído)
* **RabbitMQ 3:** Porta `5672` (Protocolo AMQP) e Porta `15672` (Painel Web de Gerenciamento: `guest`/`guest`)

---

## 2. Fluxo de Implementação de Casos de Uso

A solução organiza os fluxos sob **Clean Architecture** e **CQRS**. A implementação de novos casos de uso segue o isolamento de dentro para fora:

### Passo 1: Domínio (`src/Core/TL.ResilientCore.Domain`)
1. Crie a entidade ou raiz de agregação herdando de `Entity` ou `AggregateRoot`.
2. Mantenha os construtores não-públicos e forneça métodos de fábrica ou comportamentos expressivos para mutações de estado.
3. Emita eventos de domínio via `RaiseDomainEvent(new MeuDomainEvent(...))` quando fatos relevantes ocorrerem.
4. Utilize o **Result Pattern** (`Result.Success` ou `Result.Failure(Error)`) em operações com possibilidade de falha de negócio, evitando lançamento de exceções de controle de fluxo (vide [ADR-002](adr/002-result-pattern.md)).

### Passo 2: Aplicação (`src/Core/TL.ResilientCore.Application`)
1. Defina o contrato de comando ou consulta implementando `ICommand<TResponse>` ou `IQuery<TResponse>`. Para consultas cacheáveis, implemente `ICachedQuery<TResponse>`.
2. Implemente o handler associado via `ICommandHandler<TCommand, TResponse>` ou `IQueryHandler<TQuery, TResponse>`.
3. Adicione regras de validação herdando de `AbstractValidator<TCommand>` com FluentValidation. O `ValidationBehavior` executará a validação automaticamente antes do handler.

### Passo 3: Apresentação (`src/Presentation/TL.ResilientCore.Api`)
1. Mapeie o endpoint na Minimal API (em `Program.cs` ou classe de extensão de rotas).
2. Utilize a extensão `.ToHttpResult()` para converter o `Result<T>` retornado pelo MediatR no status HTTP correspondente (200, 201, 400, 404, 422):

```csharp
app.MapPost("/clientes", async (CriarClienteCommand command, ISender sender, CancellationToken ct) => 
{
    var result = await sender.Send(command, ct);
    return result.ToHttpResult();
});
```

---

## 3. Persistência e Mensageria Transacional

### Transactional Outbox
1. Ao invocar `SaveChangesAsync()`, o `InsertOutboxMessagesInterceptor` serializa os eventos de domínio registrados nas entidades e os grava na tabela `OutboxMessages` dentro da mesma transação SQL.
2. O serviço em segundo plano `ProcessOutboxMessagesJob` consome as mensagens pendentes utilizando um índice parcial (`"ProcessedOnUtc" IS NULL AND "RetryCount" < 5`), mantendo o custo de varredura constante (vide [ADR-001](adr/001-indices-filtrados-outbox.md)).
3. O despacho ocorre via `RabbitMqEventPublisher` (`TL.RabbitMQ`) com Dead Letter Queue (`resilientcore.events.dlq`), injeção de contexto W3C (`traceparent`) e fallback in-process para MediatR.

### Transactional Inbox
1. Eventos recebidos de mensageria assíncrona são validados via `IInboxService` contra a tabela `inbox_messages`.
2. Mensagens já processadas são descartadas de forma idempotente, prevenindo efeitos colaterais duplicados em cenários de reentrega do broker (vide [ADR-008](adr/008-inbox-pattern-e-observabilidade-cloud-native.md)).

---

## 4. Testes Automatizados e Carga

### Execução dos Testes da Solução
```bash
dotnet test TL.ResilientCore.sln -c Release
```

### Testes de Carga (k6 via Docker)
Com a API em execução local, execute o container de validação de carga na pasta `load-tests/` ou na raiz:

```bash
docker compose -f load-tests/docker-compose.k6.yml up
```

O script avalia limites de vazão, latência P95 e consumo de conexões do PostgreSQL (vide [ADR-006](adr/006-testes-de-carga-desacoplados-k6-docker.md) e [load-tests/README.md](../load-tests/README.md)).