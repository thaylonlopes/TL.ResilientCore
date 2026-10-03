# ADR 007: Caching FinOps Híbrido no Pipeline CQRS (In-Memory $0 vs Redis Enterprise)

## 1. Contexto

Em arquiteturas de microsserviços baseadas em CQRS, o caching de consultas (*Queries*) é indispensável para reduzir latência de leitura e poupar CPU/I/O do banco de dados relacional. Contudo, a escolha do mecanismo de cache frequentemente introduz dilemas operacionais e financeiros (FinOps):

1. **Custo Desproporcional para Pequenos Cenários (Overprovisioning FinOps)**:
   - Projetos embrionários, startups, ferramentas internas ou microsserviços de baixo tráfego que adotam Redis gerenciado na nuvem (AWS ElastiCache, Azure Cache for Redis) incorrem em custos fixos mensais desnecessários ($15 a $100+ por instância), tornando a adoção do template proibitiva.
2. **Fragilidade em Larga Escala sem Governança de Cache Distribuído**:
   - Em operações corporativas multi-instância, o uso exclusivo de cache em memória local gera inconsistência de dados (*cache skew*). Além disso, caches distribuídos mal configurados sofrem de *Cache Stampede* (quando milhares de chaves expiram no mesmo segundo e derrubam o banco) e vulnerabilidades de conformidade (LGPD/GDPR) caso dados sensíveis sejam cacheados em texto plano no cluster.

---

## 2. Decisão

Decidimos adotar uma estratégia de **Caching FinOps Híbrido** guiada pelo princípio da Inversão de Dependência (DIP), desacoplando a camada de aplicação da infraestrutura física através de um contrato abstrato e um pipeline behavior do MediatR:

### 2.1. Arquitetura em Duas Camadas (Tiers FinOps)

1. **Tier Local / Small Business (Custo $0)**:
   - Configurado via `IMemoryCache` nativo do ASP.NET Core em memória RAM de processo.
   - Ideal para desenvolvimento local, suítes de teste, microsserviços monolíticos ou empresas em fase inicial com restrição orçamentária.
   - Custo adicional de nuvem: **$0,00**.

2. **Tier Enterprise Distribuído (Redis com Resiliência Avançada)**:
   - Integrado através do pacote corporativo `TL.Caching.Helpers`.
   - Suporte a multi-instância e alta disponibilidade via Redis (`tl-redis` no Docker local ou clusters gerenciados em nuvem).
   - **TTL Jitter Decorrelacionado**: Aplica variação estocástica no tempo de expiração para dispersar a carga e eliminar o risco de *Cache Stampede*.
   - **Criptografia AES-GCM (256 bits)**: Garante criptografia de payloads serializados em repouso, atendendo a requisitos rígidos de conformidade (PCI-DSS, LGPD).
   - **Compressão GZip**: Reduz consumo de banda e pegada de memória RAM do cluster Redis para payloads volumosos.

### 2.2. Integração Transparente via CQRS MediatR

As consultas de negócio que demandam cache apenas implementam a interface marcadora `ICachedQuery`:

```csharp
public sealed record GetClientePorIdQuery(Guid Id) : ICachedQuery<ClienteResponse>
{
    public string CacheKey => $"cliente:{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
```

O `CachingBehavior<TRequest, TResponse>` intercepta a execução no pipeline:
- **Cache Hit**: Retorna imediatamente os dados desserializados.
- **Cache Miss**: Executa o handler original, persiste o resultado no cache configurado e retorna o valor.
- **Graceful Fallback**: Se o Redis ou o broker de cache sofrer instabilidade ou timeout, o comportamento captura o erro internamente, registra log de aviso e executa a consulta diretamente no banco relacional, garantindo 100% de disponibilidade para o usuário final.

---

## 3. Consequências

### Pontos Positivos:
* **Flexibilidade FinOps**: O mesmo microsserviço gerado pelo template pode iniciar com custo zero de infraestrutura e migrar para Redis corporativo apenas alterando a configuração no `appsettings.json`, sem alterar nenhuma linha de regra de negócio.
* **Resiliência e Proteção de Dados**: Mitigação automática de *Cache Stampede* e proteção de segurança de ponta a ponta com criptografia autenticada AES-GCM.
* **Alta Disponibilidade Garantida**: Falhas transitórias no cluster de cache não degradam a operação da API graças ao mecanismo de *Graceful Fallback*.

### Pontos Negativos / Mitigações:
* **Invalidação em Cache Distribuído**: Requer disciplina na modelagem de chaves e eventos de invalidação em mutações (Commands). *Mitigação*: Uso de chaves semânticas (`cliente:{id}`) e invalidação reativa orientada a eventos.
