# ADR 005: Adoção de Bibliotecas Reutilizáveis do Ecossistema TL via NuGet

## 1. Contexto

À medida que a fábrica de software desenvolve múltiplos microsserviços a partir deste starter kit, utilitários comuns de manipulação de enums, extensões de IQueryable, extração de claims JWT, tratamento de exceções e métricas de pipeline HTTP correm o risco de sofrer duplicação contínua de código (*copy-paste*) e divergência de manutenção entre repositórios.

Para manter o template enxuto e centralizar a evolução desses utilitários com versionamento semântico, foi necessário padronizar a forma de consumo dos pacotes utilitários do ecossistema.

---

## 2. Decisão

Decidimos consumir as bibliotecas do ecossistema oficial **TL** exclusivamente como **pacotes NuGet publicados** (`PackageReference`), segregando o consumo de acordo com a responsabilidade de cada camada da Clean Architecture através dos **Metapacotes Estruturais** (`TL.ExtensionLibrary.*` v0.4.1) e do pacote oficial de observabilidade (`TL.MiddlewareLibrary` v0.2.0):

### 2.1. Catálogo e Metapacotes Integrados por Camada

| Camada / Projeto | Pacote NuGet | Versão | Conteúdo Embutido & Finalidade no Template |
| :--- | :--- | :---: | :--- |
| **`Domain`** | **`TL.ExtensionLibrary.Domain`** | `0.4.1` | Metapacote agrupador de domínio: `TL.EnumExtensionsLibrary` (Bounded Cache), `TL.NumericExtensionsLibrary` e `TL.StringExtensionsLibrary` (Zero-Allocation). |
| **`Application`** | **`TL.ExtensionLibrary.Application`** | `0.4.1` | Metapacote agrupador de aplicação: `TL.CollectionExtensionsLibrary`, `TL.ObjectExtensionsLibrary`, `TL.QueryableExtensionsLibrary` (Keyset Pagination) e `TL.EnumExtensionsLibrary`. |
| **`Infrastructure`** | **`TL.ExtensionLibrary.Infrastructure`** | `0.4.1` | Metapacote agrupador de infraestrutura: `TL.AssemblyExtensionLibrary`, `TL.ClaimsPrincipalExtensionsLibrary`, `TL.DateTimeExtensionsLibrary` e `TL.HttpClientExtensionsLibrary`. |
| **`Presentation.Api`** | **`TL.MiddlewareLibrary`** | `0.2.0` | Middleware de tratamento global de exceções(ProblemDetails) e métricas de tempo de resposta HTTP. |
| **`Presentation.Api`** | **`TL.ClaimsPrincipalExtensionsLibrary`** | `0.4.1` | Extração tipada de claims JWT (`UserId`, `Email`, `Roles`, `ClaimSub()`) nos endpoints de API. |

---

## 3. Consequências

### Pontos Positivos:
* **Eliminação de Código Duplicado**: Os microsserviços derivados utilizam implementações testadas e consolidadas sem reimplementar utilitários básicos.
* **Isolamento e Evolução Independente**: Cada biblioteca evolui com seu próprio ciclo de release e versionamento semântico no NuGet.org.
* **Template Mais Leve e Focado**: O template foca na arquitetura de software, CQRS, resiliência e testes, delegando utilitários gerais aos seus respectivos pacotes.

### Pontos Negativos / Mitigações:
* **Gestão de Dependências**: Atualizações nos pacotes exigem bumping de versão nos projetos derivados. *Mitigação*: Dependabot / automação de atualizações de dependências via CI/CD.

