# ADR 006: Estrutura de Testes de Carga Desacoplados com k6 e Docker

## 1. Contexto

Com a incorporação de bibliotecas de infraestrutura e observabilidade (`TL.MiddlewareLibrary`) interceptando o pipeline de requisições HTTP, torna-se essencial dispor de um mecanismo para aferir a capacidade de vazão, latência e o impacto sob concorrência de conexões com o banco de dados PostgreSQL.

## 2. Decisão

Decidimos adotar a **Estrutura Desacoplada de Testes de Carga com k6 via Docker**, centralizada exclusivamente na pasta `load-tests/` na raiz do projeto.

Mantém a Solution C# 100% enxuta, não exige instalação de runtimes adicionais na máquina host (apenas Docker) e adota o padrão de mercado para testes de performance modernos

### 2.1. Topologia de Arquivos

```text
TL.ResilientCore/
├── src/
├── tests/
├── docs/
└── load-tests/
    ├── k6-config.js           # Definição dos cenários, thresholds e requisições
    ├── docker-compose.k6.yml  # Orquestração do container k6 montando volume
    └── README.md              # Guia de execução e interpretação de métricas
```

### 2.2. Cenários e Critérios de Aceite (Thresholds)

O script `k6-config.js` é configurado com um perfil de **Smoke Load Test**:
- **Rampa de Carga**: Rampa suave até 50 usuários virtuais concorrentes (VUs) durante 50 segundos.
- **Endpoints Cobertos**:
  1. `GET /`: Medição de throughput de borda do ASP.NET Core e do pipeline de middlewares.
  2. `GET /clientes?pageNumber=1&pageSize=10`: Medição de latência com I/O de banco e pool de conexões Npgsql.
  3. `GET /secure-data`: Validação de rejeição 401 Unauthorized e geração de ProblemDetails sob carga.
- **SLA / Thresholds**:
  - `http_req_failed`: $< 1\%$ de falhas para endpoints públicos.
  - `http_req_duration`: Percentil 95 ($P95$) $< 200\text{ ms}$.

### 2.3. Resolução de Rede e Portabilidade

Para permitir a execução tanto contra a API rodando no host da máquina de desenvolvimento quanto em ambientes remotos ou de staging, a URL base é resolvida via variável de ambiente:

```javascript
const BASE_URL = __ENV.API_URL || 'http://host.docker.internal:5000';
```

---

## 3. Consequências

### Pontos Positivos:
* **Zero Poluição na Solution C#**: O arquivo `.sln` e os comandos `dotnet build` / `dotnet test` continuam com a performance inalterada.
* **Execução com 1 Comando**: O desenvolvedor pode rodar os testes com um simples comando Docker:
  ```bash
  docker compose -f load-tests/docker-compose.k6.yml up
  ```
* **Portabilidade Total**: Funciona igualmente no Windows, Linux e macOS sem exigir instalação de ferramentas adicionais além do Docker já utilizado para o PostgreSQL .
* **Pronto para CI/CD**: O mesmo script pode ser acionado em pipelines do GitHub Actions ou GitLab CI via `grafana/k6-action`.

### Pontos Negativos / Mitigações:
* **Dependência do Docker**: Exige o Docker Desktop/Engine rodando localmente. *Mitigação*: O Docker já é um pré-requisito do template para o PostgreSQL (`docker-compose.yml`).
