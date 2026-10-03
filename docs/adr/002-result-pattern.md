# ADR 002: Adoção do Result Pattern em detrimento de Exceptions

## 1. Contexto

No desenvolvimento .NET tradicional, é comum lançar exceções (`throw new BusinessRuleValidationException()`) para validar regras de domínio, como "Saldo Insuficiente" ou "E-mail já cadastrado". 

O problema dessa abordagem (*Exception-driven flow*) é duplo:
1. **Performance:** Lançar uma exceção no .NET é uma operação computacionalmente custosa, pois exige a coleta de toda a *Stack Trace*. Sob alta carga (milhares de requisições por segundo), isso causa degradação severa de performance.
2. **Legibilidade (GOTO disfarçado):** Exceções quebram o fluxo linear do código. Os *Controllers* ou *Handlers* precisam adivinhar quais exceções o domínio pode lançar e encapsulá-las em blocos `try/catch` genéricos.

## 2. Decisão

Decidimos utilizar o **Result Pattern** para todo o fluxo de controle de regras de negócio. 
O domínio e a camada de aplicação nunca lançarão exceções para regras de negócio (apenas para erros de infraestrutura reais, como "Banco de Dados Fora do Ar").

As respostas de todos os *Use Cases* e *Domain Models* retornarão um objeto `Result<T>` ou `Result`, que encapsula tanto o sucesso quanto as mensagens de falha.

Exemplo:
```csharp
public Result<Account> Withdraw(decimal amount)
{
    if (Balance < amount)
        return Result.Failure(DomainErrors.Account.InsufficientFunds);
        
    Balance -= amount;
    return Result.Success(this);
}
```

## 3. Consequências

### Pontos Positivos (Ganhos):
* **Performance previsível:** Elimina o *overhead* absurdo de geração de Stack Traces para problemas de negócio triviais.
* **Contratos Explícitos:** A assinatura do método diz exatamente o que ele retorna. O desenvolvedor é forçado pelo compilador a lidar com a possibilidade de falha.
* **Código Limpo:** Fim dos blocos `try/catch` de negócios. A camada *Presentation* (Controllers) mapeia de forma elegante a falha (ex: `Result.IsFailure`) para o status HTTP adequado (400 Bad Request, 404 Not Found).

### Pontos Negativos (Trade-offs):
* **Boilerplate Adicional:** Requer a criação (ou importação via NuGet) das classes `Result`, `Error` e extensões de mapeamento.
* **Curva de Aprendizado:** Desenvolvedores menos experientes podem estranhar o estilo funcional inicialmente, tendo o instinto de usar `throw` por hábito.