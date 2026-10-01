using TL.ResilientCore.Domain.Shared;

namespace TL.ResilientCore.Application.Exceptions;

public sealed record ValidationError : Error
{
    public ValidationError(Error[] errors) 
        : base("Validation.General", "Um ou mais erros de validação ocorreram.")
    {
        Errors = errors;
    }

    public Error[] Errors { get; }
}