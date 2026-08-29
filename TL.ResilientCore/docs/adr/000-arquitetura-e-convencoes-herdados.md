# ADR 000: Arquitetura Base e Convenções do TL.ResilientCore

## 1. Contexto e Forças

Ao iniciar o desenvolvimento de microsserviços corporativos de missão crítica, times de desenvolvimento frequentemente enfrentam desafios de:
1. **Acoplamento Arquitetural**: Lógica de domínio misturada com detalhes de framework web, bibliotecas de banco de dados ou mensageria.
2. **Inconsistência Distribuída (Dual Write)**: Risco de salvar dados no banco de dados e falhar ao publicar o evento no message broker, ou vice-versa.
3. **Degradação de Performance por Exceções**: Uso massivo de `throw Exception` para controle de fluxo de regras de negócio, gerando overhead severo de stack trace allocation.
4. **Fragilidade de Testes**: Testes de integração baseados em mocks irrealistas ou SQLite in-memory, incapazes de validar dialetos SQL específicos (como Partial Indexes do PostgreSQL) ou comportamento real de concorrência.
5. **Divergência de Padrões**: Ausência de guardrails automatizados no CI/CD para impedir a violação das fronteiras arquiteturais.

Precisamos de uma fundação arquitetural padronizada, robusta, altamente testável e com convenções explícitas para todos os microsserviços derivados deste starter kit.

---

## 2. Decisão

Adotamos a combinação dos seguintes pilares como convenção padrão herdada no **TL.ResilientCore**:

1. **Clean Architecture (Onion/Hexagonal)**:
   - Divisão estrita em 4 camadas: `Domain` (núcleo puro), `Application` (casos de uso e CQRS), `Infrastructure` (persistência EF Core e adaptadores externos) e `Presentation.Api` (Minimal APIs e autenticação).
   - Regra de dependência estritamente para dentro: `Domain` não possui dependências externas além do runtime e extensões utilitárias puras.

2. **CQRS com MediatR e Pipeline Behaviors**:
   - Segregação de `ICommand` / `ICommandHandler` (mutações) e `IQuery` / `IQueryHandler` (leituras).
   - Validação automática de comandos via `ValidationBehavior<TRequest, TResponse>` integrado ao `FluentValidation`, executando validações antes que o comando atinja o handler.

3. **Result Pattern Funcional**:
   - Eliminação de `throw new Exception` para regras de negócio (conforme [ADR-002](002-result-pattern.md)).
   - Métodos de domínio e casos de uso retornam `Result` ou `Result<TValue>`, com erros mapeados por `ResultExtensions` diretamente para respostas HTTP (200 OK, 400 Bad Request, 404 Not Found).

4. **Outbox Pattern com Índices Filtrados e Dead Letter**:
   - Captura automática de `IDomainEvent` via `InsertOutboxMessagesInterceptor` do EF Core, gravando na tabela `OutboxMessages` na mesma transação atômica do banco.
   - Índice filtrado (`HasFilter("\"ProcessedOnUtc\" IS NULL AND \"RetryCount\" < 5")`) no PostgreSQL, garantindo que a busca do worker `ProcessOutboxMessagesJob` ocorra em `< 1ms` e isole falhas permanentes (conforme [ADR-001](001-indices-filtrados-outbox.md)).

5. **Design for Failure & Idempotência**:
   - Todo handler e consumidor deve ser idempotente (conforme [ADR-004](004-design-for-failure.md)).
   - Proibição de I/O no Domínio e retries infinitos síncronos.

6. **Pirâmide de Testes Automatizados**:
   - **Testes de Arquitetura (`NetArchTest.Rules`)**: Garantem a integridade das camadas, imutabilidade de `Domain Events` (`sealed record`) e encapsulamento de construtores de `Entity`.
   - **Testes de Integração (`Testcontainers.PostgreSql`)**: Executam contra containers reais de PostgreSQL 16 provisionados dinamicamente via Docker e `WebApplicationFactory`.

---

## 3. Alternativas Consideradas

- **Arquitetura Tradicional em Camadas (N-Tier Monolítica)**:
  - *Motivo de rejeição*: Alto acoplamento com o banco de dados; regras de negócio dispersas entre controllers e stored procedures/services genéricos.
- **Controle de Fluxo Baseado em Exceções**:
  - *Motivo de rejeição*: Custo computacional excessivo de criação de stack traces sob alta volumetria e quebra de fluxo previsível (GOTO disfarçado).
- **Publicação Direta no Message Broker (Sem Outbox)**:
  - *Motivo de rejeição*: Vulnerável a perda de mensagens em caso de falha de rede pós-commit do banco (Dual Write Problem).
- **Mocks In-Memory / SQLite para Testes de Integração**:
  - *Motivo de rejeição*: SQLite não suporta a sintaxe de índices parciais/filtrados do PostgreSQL nem comportamentos transacionais idênticos ao ambiente de produção.

---

## 4. Consequências

### Pontos Positivos:
- **Resiliência e Integridade**: Garantia transacional sem perda de eventos e proteção contra indisponibilidade momentânea de brokers.
- **Performance Previsível**: Consultas da Outbox em tempo submilisegundo e eliminação do overhead de exceptions.
- **Governança Automatizada**: O build do projeto quebra automaticamente caso algum desenvolvedor viole as regras de arquitetura (via NetArchTest).
- **Testabilidade Real**: Testes de integração reproduzem exatamente o ambiente de produção através do Testcontainers.

### Negativas / Riscos Mitigados:
- **Boilerplate Estrutural**: Criação de múltiplos arquivos por caso de uso (Command, CommandHandler, Validator). *Mitigação*: Uso do template via CLI (`dotnet new tl-resilientcore`).
