# ADR 003: Separação de Command e Query (CQRS)

## 1. Contexto

Em arquiteturas tradicionais (como N-Tier), os mesmos repositórios e DTOs são frequentemente usados tanto para ler dados (Queries) quanto para gravar dados (Commands). 

Com a evolução da complexidade, as consultas passam a exigir dados massivos, *joins* complexos, paginação e projeções otimizadas. Ao mesmo tempo, as escritas exigem regras de domínio rigorosas, rastreabilidade de eventos e carregamento de entidades completas na memória. 

Tentar usar os mesmos modelos para ambas as finalidades leva a classes inchadas e *queries* do Entity Framework que carregam mais dados do que o necessário, gerando gargalos de I/O.

## 2. Decisão

Decidimos adotar o padrão **CQRS (Command Query Responsibility Segregation)** na camada de Aplicação (`src/Application`).

* **Commands (Escrita):** Operações que alteram o estado (Create, Update, Delete). Serão orquestradas via padrão Mediator (`MediatR`) e carregarão as Entidades Ricas (Domain Models) utilizando o Entity Framework com rastreamento (`Tracking`).
* **Queries (Leitura):** Operações que apenas retornam dados. Não passarão pelo modelo de domínio rico. Serão executadas diretamente contra o banco de dados com `AsNoTracking()` (ou ferramentas leves como Dapper) projetando os resultados diretamente para DTOs.
* **Integração de Bibliotecas:** O boilerplate de paginação, filtros e ordenação dinâmica nas Queries será automatizado utilizando a biblioteca NuGet interna `TL.QueryableExtensions`.

## 3. Consequências

### Pontos Positivos (Ganhos):
* **Separação de Preocupações (SoC):** Cada *Use Case* tem um arquivo único (`CreateTaskCommand`, `GetTasksQuery`), facilitando a manutenção e a navegação no código.
* **Otimização Assíncrona:** A leitura pode escalar de forma totalmente independente da escrita, permitindo a futura introdução de caches (Redis) apenas nos *Handlers* de Query.
* **Integração com Tooling Próprio:** Dá propósito à existência da biblioteca `TL.QueryableExtensions`, resolvendo o problema real de lidar com *Dynamic LINQ* nas consultas.

### Pontos Negativos (Trade-offs):
* **Maior quantidade de arquivos:** Para um simples CRUD, serão gerados múltiplos arquivos (Command, CommandHandler, Query, QueryHandler, DTOs). O template não deve ser usado para micro-APIs que fazem apenas cadastros diretos (Minimal APIs sem regras de domínio).