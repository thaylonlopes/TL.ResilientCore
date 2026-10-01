using MediatR;
using Microsoft.EntityFrameworkCore;
using QueryableExtensionsLibrary;
using TL.ResilientCore.Application.Interfaces;
using TL.ResilientCore.Domain.Entities;
using TL.ResilientCore.Domain.Shared;

namespace TL.ResilientCore.Application.Features.Clientes.Queries.GetClientes;

/// <summary>
/// Handler CQRS responsável por processar a consulta de clientes com filtros dinâmicos e paginação.
/// </summary>
/// <param name="dbContext">Contexto de acesso a dados da aplicação.</param>
internal sealed class GetClientesQueryHandler(IApplicationDbContext dbContext) 
    : IRequestHandler<GetClientesQuery, Result<IReadOnlyList<ClienteResponse>>>
{
    /// <summary>
    /// Executa a query de clientes aplicando filtros condicionais e paginação otimizada com QueryableExtensionsLibrary.
    /// </summary>
    /// <param name="request">Parâmetros da consulta paginada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Lista paginada de clientes encapsulada em Result.</returns>
    public async Task<Result<IReadOnlyList<ClienteResponse>>> Handle(GetClientesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IQueryable<Cliente> query = dbContext.Clientes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Nome))
        {
            query = query.Filter(nameof(Cliente.Nome), request.Nome);
        }

        if (request.Ativo.HasValue)
        {
            query = query.Filter(nameof(Cliente.Ativo), request.Ativo.Value);
        }

        var clientes = await query
            .Page(request.PageNumber, request.PageSize)
            .Select(c => new ClienteResponse(c.Id, c.Nome, c.Email, c.Ativo))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ClienteResponse>>(clientes);
    }
}