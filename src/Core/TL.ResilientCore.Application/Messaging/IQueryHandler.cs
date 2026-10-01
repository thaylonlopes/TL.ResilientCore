using MediatR;
using TL.ResilientCore.Domain.Shared;

namespace TL.ResilientCore.Application.Messaging;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}