using Microsoft.EntityFrameworkCore;
using TL.ResilientCore.Application.Abstractions.Data;
using TL.ResilientCore.Application.Interfaces;
using TL.ResilientCore.Domain.Entities;
using TL.ResilientCore.Infrastructure.Outbox;

namespace TL.ResilientCore.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IUnitOfWork, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }
}