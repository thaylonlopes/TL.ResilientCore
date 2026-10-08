# ADR 005: Adoção de Bibliotecas Reutilizáveis do Ecossistema TL via NuGet

## 1. Contexto

À medida que a fábrica de software desenvolve múltiplos microsserviços a partir deste starter kit, utilitários comuns de manipulação de enums, extensões de IQueryable, paginação por cursor, contratos base, resiliência, auditoria, extração de claims JWT, tratamento global de exceções e métricas de pipeline HTTP correm o risco de sofrer duplicação contínua de código (*copy-paste*) e divergência de manutenção entre repositórios.

Para manter o template enxuto e centralizar a evolução desses utilitários com versionamento semântico, padronizou-se o consumo das bibliotecas utilitárias e estruturais do ecossistema TL via Central Package Management (CPM).

---

## 2. Decisão

Consumir as bibliotecas do ecossistema oficial **TL** exclusivamente como **pacotes NuGet publicados** (`PackageReference`), segregando o consumo de acordo com a responsabilidade de cada camada da Clean Architecture, com controle centralizado de versões em `Directory.Packages.props`:

### 2.1. Catálogo de Pacotes e Metapacotes Integrados por Camada

| Camada / Projeto | Pacote NuGet | Versão | Conteúdo Embutido & Finalidade no Template |
| :--- | :--- | :---: | :--- |
| **`Domain`** | **`TL.ExtensionLibrary.Domain`** | `0.8.0` | Metapacote agrupador de domínio: `TL.EnumExtensionsLibrary` (Bounded Cache), `TL.NumericExtensionsLibrary` e `TL.StringExtensionsLibrary` (Zero-Allocation). |
| **`Application`** | **`TL.ExtensionLibrary.Application`** | `0.8.0` | Metapacote agrupador de aplicação: coleções, objetos e utilitários de extensão. |
| **`Application`** | **`TL.BaseContracts`** | `0.5.0` | Contratos base de domínio e aplicação: `IUnitOfWork`, `IClock`, interfaces de paginação e ordenação determinística. |
| **`Application`** | **`TL.QueryableExtensionsLibrary`** | `0.8.0` | Extensões LINQ e IQueryable para projeções e consultas otimizadas. |
| **`Infrastructure`** | **`TL.ExtensionLibrary.Infrastructure`** | `0.8.0` | Metapacote agrupador de infraestrutura: reflexão de assemblies, manipulação de datas e clientes HTTP. |
| **`Infrastructure`** | **`TL.Caching.Helpers`** | `0.5.0` | Abstrações e utilitários para estratégias de cache distribuído/em memória e controle de expiração. |
| **`Infrastructure`** | **`TL.PagingFiltering.Helpers`** | `0.5.0` | Paginação determinística (keyset/offset) e filtros dinâmicos via Specification Pattern. |
| **`Infrastructure`** | **`TL.Resilience`** | `0.5.0` | Estratégias de resiliência e políticas de retry/circuit-breaker integradas ao pipeline de infraestrutura. |
| **`Presentation.Api`** | **`TL.MiddlewareLibrary`** | `0.5.0` | Middlewares para tratamento global de exceções (RFC 7807 ProblemDetails), correlação e métricas HTTP. |
| **`Presentation.Api`** | **`TL.AuditLogger`** | `0.5.0` | Registro estruturado de auditoria e telemetria de requisições na borda da API. |
| **`Presentation.Api`** | **`TL.ClaimsPrincipalExtensionsLibrary`** | `0.8.0` | Extração fortemente tipada de claims JWT (`UserId`, `Email`, `Roles`, `ClaimSub()`) nos endpoints e filtros. |

---

## 3. Consequências

### Pontos Positivos:
* **Eliminação de Código Duplicado**: Microsserviços derivados utilizam implementações testadas e consolidadas sem reimplementar utilitários básicos.
* **Isolamento e Evolução Independente**: Cada biblioteca evolui com seu próprio ciclo de release e versionamento semântico no NuGet.org.
* **Governança via Central Package Management (CPM)**: Todas as versões são unificadas no `Directory.Packages.props`, evitando conflitos de dependências transitivas.
* **Aderência à Inversão de Dependência (DIP)**: O núcleo (`Domain`/`Application`) depende apenas de contratos e utilitários puros, delegando integrações externas para `Infrastructure` e `Presentation.Api`.

### Pontos Negativos / Mitigações:
* **Gestão de Dependências**: Atualizações nos pacotes exigem bumping de versão no `Directory.Packages.props` da solução. *Mitigação*: Atualização centralizada e testes automatizados de arquitetura (NetArchTest) em CI/CD.

