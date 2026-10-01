using MediatR;
using TL.ResilientCore.Domain.Shared;

namespace TL.ResilientCore.Application.Messaging;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}