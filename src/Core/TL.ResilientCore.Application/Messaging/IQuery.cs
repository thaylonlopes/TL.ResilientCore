using MediatR;
using TL.ResilientCore.Domain.Shared;

namespace TL.ResilientCore.Application.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}