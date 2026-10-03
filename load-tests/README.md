# Testes de Carga com k6 e Docker

Esta pasta contém a estrutura desacoplada para execução de testes de carga e validação de performance sobre a API do `TL.ResilientCore`.

---

## Como Executar

### 1. Pré-requisito: API em Execução
Certifique-se de que a API esteja em execução localmente (via Docker ou localmente na porta 5000/5001).

### 2. Executar o Teste de Carga via Docker
Na raiz do projeto ou dentro da pasta `load-tests/`, execute:

```bash
docker compose -f load-tests/docker-compose.k6.yml up
```

---

## Executar contra Ambientes Remotos ou Staging

Para apontar o teste para outro servidor sem alterar o script, informe a variável `API_URL`:

```bash
# Exemplo apontando para staging:
API_URL=https://staging.api.empresa.com docker compose -f load-tests/docker-compose.k6.yml up
```

---

## Cenários Avaliados no `k6-config.js`

1. **Throughput de Borda (`GET /`)**: Avalia a vazão máxima do pipeline HTTP e middlewares.
2. **Latência com Banco (`GET /clientes`)**: Avalia o tempo de resposta do EF Core com paginação e pool de conexões do PostgreSQL.
3. **Rejeição Segura (`GET /secure-data`)**: Avalia o comportamento do pipeline sob requisições não autenticadas (HTTP 401).

---

## Critérios de Aceite (Thresholds)
* **P95 Latência**: $< 200\text{ ms}$
* **Taxa de Erro**: $< 1\%$
