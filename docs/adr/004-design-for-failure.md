# ADR 004: Restrições Arquiteturais, Idempotência e Anti-Patterns

## 1. Contexto

O template `TL.ResilientCore` foi desenhado para ambientes distribuídos de alta complexidade, utilizando padrões como *Outbox*, *Sagas* e Mensageria (Kafka/RabbitMQ). 
Nesse tipo de ambiente, não assumimos que a rede é confiável. Falhas de conexão, *timeouts* de banco de dados e quedas de instâncias vão acontecer. 

Como o sistema é baseado em eventos e garante a entrega *At-Least-Once* (Pelo menos uma vez), é matematicamente garantido que, em algum momento de falha da rede, **uma mesma mensagem ou comando será processado duas vezes**. 
Se os desenvolvedores que utilizarem este template programarem com a mentalidade de um "CRUD tradicional", teremos duplicação de dados, cobranças duplas e inconsistências financeiras graves.

## 2. Decisão

Definimos um conjunto de regras rígidas e **pontos de atenção obrigatórios (Guardrails)** para qualquer código escrito sobre este template:

1. **Idempotência Obrigatória (O maior cuidado):** 
   Todo `CommandHandler` ou consumidor de eventos (Background Service) deve ser **idempotente**. Isso significa que se o mesmo comando (ex: "ProcessarPagamento_ID_123") for executado 10 vezes seguidas, o resultado final no banco de dados deve ser exatamente o mesmo da primeira execução. Nenhuma duplicação deve ocorrer.

2. **Proibição de I/O no Domínio (Anti-Pattern):**
   A camada de `Domain` (Entidades e Value Objects) é estritamente isolada. É terminantemente proibido injetar interfaces de repositório, disparar chamadas HTTP ou acessar o disco de dentro de uma Entidade. Toda dependência externa deve ser resolvida pela camada de Aplicação (`Use Cases`) antes de interagir com o Domínio.

3. **Restrição de Retries Síncronos:**
   Não implementaremos laços de repetição infinitos (`while`) para chamadas falhas. O uso de *Polly* (Retry with Exponential Backoff + Circuit Breaker) é obrigatório nas integrações externas, mas deve possuir um limite curto (ex: 3 tentativas) antes de falhar graciosamente e delegar a recuperação para a *Saga* ou *Dead Letter Queue*.

## 3. Consequências

### Pontos Positivos (Ganhos):
* **Sistema à prova de balas:** Mesmo com a infraestrutura em colapso (restart de pods no Kubernetes, falhas de rede), o estado da aplicação se mantém consistente quando o serviço voltar.
* **Governança Clara:** Documentar o que "não fazer" acelera o *Onboarding* de novos desenvolvedores na fábrica de software, cortando pela raiz discussões longas em *Pull Requests*.

### Pontos Negativos (Trade-offs / Cuidado Contínuo):
* **Custo Cognitivo (Atenção redobrada):** O desenvolvedor não pode mais simplesmente fazer `saldo -= valor`. Ele precisará verificar coisas como `if (TransacaoJaProcessada(id)) return Success()`. O código fica ligeiramente maior para garantir a idempotência.
* **Necessidade de Ferramental:** Para garantir que essas regras não fiquem apenas no papel, pode ser necessário introduzir ferramentas como `NetArchTest` (Testes de Arquitetura) para quebrar o *Build* do CI/CD caso alguém viole o isolamento do domínio.