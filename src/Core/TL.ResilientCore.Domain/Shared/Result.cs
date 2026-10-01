namespace TL.ResilientCore.Domain.Shared;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected internal Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Um sucesso não pode conter um erro.");
            
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Uma falha precisa conter um erro.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error) 
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("O valor de um resultado de falha não pode ser acessado.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}