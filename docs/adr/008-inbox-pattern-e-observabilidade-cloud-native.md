# ADR 008: Inbox Pattern, Observabilidade W3C e Health Checks Cloud-Native

## 1. Contexto
Com a consolidação do Transactional Outbox e integração com RabbitMQ na versão 1.3.0, duas novas necessidades de governança operacional e resiliência foram identificadas:
1. **Deduplicação de Mensagens no Consumidor (Inbox Pattern):** Mensagens enviadas via RabbitMQ possuem garantia *At-Least-Once*, podendo ser entregues mais de uma vez devido a desconexões transitórias ou falhas de acknowledgment de rede.
2. **Rastreabilidade e Monitoramento Cloud-Native:** Orquestradores como Kubernetes exigem probes de liveness e readiness segregadas (`/livez` e `/readyz`), e plataformas de tracing distribuído (Jaeger, Datadog) demandam propagação W3C (`traceparent`).

---

## 2. Decisões Técnicas

### 2.1. Adoção do Inbox Pattern Idempotente
* Criada a tabela `InboxMessages` com chave e índice único composto `(Id, Type)`.
* Implementada a abstração `IInboxService` para verificar se um evento já foi processado antes de executar qualquer efeito colateral de domínio.

### 2.2. Integração do Pacote Proprietário TL.HealthCheck
* Utilizado o pacote `TL.HealthCheck` (v0.5.0) para expor probes `/livez` e `/readyz` com serialização leve e não-bloqueante em `System.Text.Json`.

### 2.3. Propagação de Contexto de Rastreio W3C (OpenTelemetry)
* O `RabbitMqEventPublisher` inicia uma `Activity` com `ActivityKind.Producer` e propaga o cabeçalho W3C `traceparent` no envelope AMQP.

### 2.4. Containerização Multi-Stage Chiseled
* Disponibilizado `Dockerfile` otimizado para .NET 9 utilizando imagens Chiseled (`aspnet:9.0-chiseled-extra`), garantindo imagens ultraleves (< 110 MB) e sem superfície de ataque (sem shell nem root).

---

## 3. Consequências
* **Positivas:** Tolerância total contra eventos duplicados, suporte nativo a Kubernetes/OpenShift com probes prontas, correlação distribuída de logs e imagens de container seguras.
* **Trade-offs:** Exige persistência adicional da tabela de Inbox para microsserviços que atuem como consumidores de mensageria.
