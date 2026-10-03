# TL.ResilientCore: Guia de Início Rápido (Getting Started)

Bem-vindo ao **TL.ResilientCore**. Este documento destina-se aos desenvolvedores que acabaram de gerar um novo projeto a partir deste template e precisam entender a estrutura inicial, o fluxo de desenvolvimento e as convenções arquiteturais.

---

## 1. Ambiente Local

Antes de iniciar a API, certifique-se de que os serviços de banco de dados e cache estejam em execução via Docker Compose.

Na raiz do projeto, execute:

```bash
docker-compose up -d
```

Isso provisionará as seguintes instâncias:
- **PostgreSQL 16** (Porta 5432)
- **Redis** (Porta 6379)

---

## 2. Fluxo de Criação de Novas Funcionalidades

O template adota rigorosamente os princípios de **Clean Architecture** e **CQRS**. Para criar uma nova funcionalidade (ex: criação de clientes), siga o fluxo de dentro para fora:

### Passo A: Domínio (src/Core/Domain)
1. Crie a entidade ou agregado herdando de `AggregateRoot` ou `Entity`.
2. Mantenha os construtores protegidos/privados e exponha Factory Methods (ex: `Cliente.Create(...)`).
3. Centralize as regras de negócio nos métodos do domínio.
4. **Convenção de Erros**: Utilize o **Result Pattern** (`Result.Success` ou `Result.Failure(Error)`) em vez de lançar exceções de fluxo (consulte a [ADR-002](adr/002-result-pattern.md)).

### Passo B: Aplicação (src/Core/Application)
1. Defina o contrato de comando herdando de `ICommand` ou `ICommand<TResponse>` (ex: `CreateClienteCommand`).
2. Implemente o respectivo handler com `ICommandHandler<TCommand, TResponse>`.
3. **Validação Automática**: Crie uma classe de validação herdando de `AbstractValidator<TCommand>` com o FluentValidation. O `ValidationBehavior` interceptará automaticamente o pipeline do MediatR e executará a validação antes do handler.

### Passo C: Apresentação (src/Presentation/Api)
1. Exponha o endpoint na Minimal API (em `Program.cs` ou módulos dedicados).
2. Utilize o método de extensão `.ToHttpResult()` para converter o retorno `Result<T>` diretamente no status code HTTP apropriado (200 OK, 400 Bad Request, 404 Not Found):

```csharp
app.MapPost("/clientes", async (CreateClienteCommand command, ISender sender, CancellationToken ct) => 
{
    var result = await sender.Send(command, ct);
    return result.ToHttpResult();
});
```

---

## 3. Mecanismos de Infraestrutura e Resiliência

### Unit of Work e Transactional Outbox
Ao persistir dados via `IApplicationDbContext.SaveChangesAsync()`, o pipeline executa as seguintes etapas:
1. **Interceptor do EF Core**: O `InsertOutboxMessagesInterceptor` extrai os eventos de domínio registrados na entidade (`RaiseDomainEvent()`) e os grava na tabela `OutboxMessages` dentro da mesma transação de banco de dados.
2. **Processamento em Background**: O worker `ProcessOutboxMessagesJob` executa periodicamente lendo as mensagens não processadas e as publica para os handlers correspondentes.
3. **Índice Filtrado e Dead Letter Queue**: A busca das mensagens pendentes utiliza um índice parcial no PostgreSQL (`"ProcessedOnUtc" IS NULL AND "RetryCount" < 5`), mantendo o tempo de consulta em `< 1ms` e isolando mensagens com falhas recorrentes (consulte a [ADR-001](adr/001-indices-filtrados-outbox.md)).

---

## 4. Testes de Performance e Carga (k6 via Docker)

Para validar a vazão (RPS), latência P95 e estabilidade de conexões do PostgreSQL sob carga, execute com a API em execução:

```bash
docker compose -f load-tests/docker-compose.k6.yml up
```

O k6 executará uma rampa suave de até 50 usuários virtuais (VUs) e exibirá o resumo das métricas diretamente no seu terminal (consulte a [ADR-006](adr/006-testes-de-carga-desacoplados-k6-docker.md) e [load-tests/README.md](../load-tests/README.md)).