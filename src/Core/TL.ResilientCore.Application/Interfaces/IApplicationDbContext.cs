using Microsoft.EntityFrameworkCore;
using TL.ResilientCore.Domain.Entities;

namespace TL.ResilientCore.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Cliente> Clientes { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}