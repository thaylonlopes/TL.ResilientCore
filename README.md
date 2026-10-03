# 🛡️ TL.ResilientCore

Template .NET 9 corporativo para microsserviços de missão crítica com Clean Architecture, CQRS, Outbox/Inbox transacional e observabilidade nativa.

---

## 🏗️ Pilares de Engenharia

- **Clean Architecture & DDD:** Camadas estritamente concêntricas com inversão de dependência (DIP).
- **CQRS com MediatR:** Separação entre comandos de escrita e queries de leitura otimizadas.
- **FinOps Caching Híbrido:** `IMemoryCache` (custo $0) para cenários locais/startups e Redis corporativo distribuído com TTL Jitter decorrelacionado e criptografia AES-GCM (256-bit).
- **Transactional Outbox com RabbitMQ:** Índice filtrado de custo $O(1)$, garantia de entrega *At-Least-Once*, Dead Letter Queue (DLQ) e fallback determinístico in-process.
- **Resiliência HTTP:** Polly v8 nativo via `AddResilientHttpClient` com Exponential Backoff, Jitter e Circuit Breaker.
- **Central Package Management (CPM):** Governança unificada de dependências via `Directory.Packages.props`.

---

## 🐳 Infraestrutura Local (Docker Compose)

Para provisionar a infraestrutura necessária de desenvolvimento e testes:

```bash
docker-compose up -d
```

### Serviços Provisionados:
- **PostgreSQL 16**: `localhost:5432` (`TLResilientDb`, usuário/senha: `postgres`/`postgres`)
- **Redis 7**: `localhost:6379` (Cache distribuído)
- **RabbitMQ 3**: `localhost:5672` (Broker AMQP) e `http://localhost:15672` (Management Web UI — `guest`/`guest`)

---

## 🚀 Executando a Solução

1. **Restaurar e Compilar:**
   ```bash
   dotnet restore TL.ResilientCore.sln
   dotnet build TL.ResilientCore.sln -c Release
   ```

2. **Executar Testes:**
   ```bash
   dotnet test TL.ResilientCore.sln
   ```

3. **Subir a API:**
   ```bash
   dotnet run --project src/Presentation/TL.ResilientCore.Api/TL.ResilientCore.Api.csproj
   ```

A documentação interativa da API e probes estarão acessíveis em:
- **Scalar UI:** `https://localhost:7001/scalar/v1`
- **OpenAPI Json:** `https://localhost:7001/openapi/v1.json`
- **Health Check Geral:** `GET /`
- **Liveness Probe:** `GET /livez`
- **Readiness Probe:** `GET /readyz`

---

## 📖 Arquitetura e Decisões Técnicas

- [Guia Completo de Arquitetura](ARCHITECTURE.md)
- [ADR 000: Arquitetura e Convenções](docs/adr/000-arquitetura-e-convencoes-herdados.md)
- [ADR 001: Índices Filtrados e DLQ no Outbox](docs/adr/001-indices-filtrados-outbox.md)
- [ADR 002: Result Pattern](docs/adr/002-result-pattern.md)
- [ADR 003: Adoção de CQRS](docs/adr/003-cqrs-adoption.md)
- [ADR 004: Design for Failure](docs/adr/004-design-for-failure.md)
- [ADR 005: Bibliotecas Reutilizáveis do Ecossistema](docs/adr/005-integracao-bibliotecas-reutilizaveis-ecossistema.md)
- [ADR 006: Testes de Carga Desacoplados com k6](docs/adr/006-testes-de-carga-desacoplados-k6-docker.md)
- [ADR 007: Caching FinOps Híbrido CQRS](docs/adr/007-finops-caching-cqrs.md)
- [ADR 008: Inbox Pattern e Observabilidade Cloud-Native](docs/adr/008-inbox-pattern-e-observabilidade-cloud-native.md)

---

## 📜 Licença e Atribuição

Distribuído sob a licença MIT. Desenvolvido e mantido por **[Thaylon Lopes](https://github.com/thaylonlopes)**.  
Ao utilizar este template ou seus derivados em projetos corporativos ou comerciais, a preservação dos avisos de direitos autorais originais é mandatória conforme os termos da licença.
