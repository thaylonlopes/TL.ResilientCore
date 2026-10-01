namespace TL.ResilientCore.Domain.Shared;

public record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "O valor fornecido é nulo.");
    public static readonly Error ConditionNotMet = new("Error.ConditionNotMet", "A condição de negócio não foi satisfeita.");

    public static implicit operator Result(Error error) => Result.Failure(error);
}