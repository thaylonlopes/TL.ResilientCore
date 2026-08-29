# ADR 005: Adoção de Bibliotecas Reutilizáveis do Ecossistema TL via NuGet

## 1. Contexto

À medida que a fábrica de software desenvolve múltiplos microsserviços a partir deste starter kit, utilitários comuns de manipulação de enums, extensões de IQueryable, extração de claims JWT, tratamento de exceções e métricas de pipeline HTTP correm o risco de sofrer duplicação contínua de código (*copy-paste*) e divergência de manutenção entre repositórios.

Para manter o template enxuto e centralizar a evolução desses utilitários com versionamento semântico, foi necessário padronizar a forma de consumo dos pacotes utilitários do ecossistema.

---

## 2. Decisão

Decidimos consumir as bibliotecas do ecossistema oficial **TL** exclusivamente como **pacotes NuGet publicados** (`PackageReference`), segregando o consumo de acordo com a responsabilidade de cada camada da Clean Architecture:

### 2.1. Catálogo de Bibliotecas Integradas

| Pacote NuGet | Camada | Finalidade no Template |
| :--- | :--- | :--- |
| **`TL.EnumExtensionsLibrary`** | `Domain`, `Application` | Conversão e extração de descrições de enums de domínio sem reflection pesada. |
| **`TL.StringExtensionsLibrary`** | `Domain`, `Application`, `Api` | Validações e manipulações funcionais de texto e sanitização de entrada. |
| **`TL.NumericExtensionsLibrary`** | `Domain` | Validações numéricas e predicados de domínio. |
| **`TL.QueryableExtensionsLibrary`** | `Application` | Filtros dinâmicos (`.Filter()`), ordenação (`.Order()`) e paginação (`.Page()`) sobre `IQueryable` nas consultas CQRS. |
| **`TL.CollectionExtensionsLibrary`** | `Application` | Particionamento e operações seguras sobre coleções em memória. |
| **`TL.ObjectExtensionsLibrary`** | `Application` | Operações e clonagem tipada de objetos. |
| **`TL.HttpClientExtensionsLibrary`** | `Infrastructure` | Métodos fluentes para consumo e deserialização com `HttpClient`. |
| **`TL.AssemblyExtensionLibrary`** | `Infrastructure` | Leitura de metadados e inspeção tipada de assemblies. |
| **`TL.DateTimeExtensionsLibrary`** | `Infrastructure` | Utilitários de cálculo e formatação de períodos temporais. |
| **`TL.ClaimsPrincipalExtensionsLibrary`** | `Api`, `Infrastructure` | Extração simplificada e segura de claims de autenticação (`UserId`, `Email`, `Roles`, `ClaimSub()`). |
| **`TL.MiddlewareLibrary`** | `Api` | Tratamento global de exceções conforme RFC 7807 (ProblemDetails) e medição de tempo de resposta HTTP. |

---

## 3. Consequências

### Pontos Positivos:
* **Eliminação de Código Duplicado**: Os microsserviços derivados utilizam implementações testadas e consolidadas sem reimplementar utilitários básicos.
* **Isolamento e Evolução Independente**: Cada biblioteca evolui com seu próprio ciclo de release e versionamento semântico no NuGet.org.
* **Template Mais Leve e Focado**: O template foca na arquitetura de software, CQRS, resiliência e testes, delegando utilitários gerais aos seus respectivos pacotes.

### Pontos Negativos / Mitigações:
* **Gestão de Dependências**: Atualizações nos pacotes exigem bumping de versão nos projetos derivados. *Mitigação*: Dependabot / automação de atualizações de dependências via CI/CD.

