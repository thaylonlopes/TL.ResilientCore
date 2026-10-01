using Caching.Helpers.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TL.Messaging.RabbitMQ.Extensions;
using TL.ResilientCore.Application.Abstractions.Caching;
using TL.ResilientCore.Application.Abstractions.Data;
using TL.ResilientCore.Application.Interfaces;
using TL.ResilientCore.Application.Messaging;
using TL.ResilientCore.Infrastructure.Outbox;
using TL.ResilientCore.Infrastructure.Persistence;
using TL.ResilientCore.Infrastructure.Persistence.Interceptors;
using TL.ResilientCore.Infrastructure.Services;
using TL.ResilientCore.Infrastructure.Services.Cache;

namespace TL.ResilientCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<InsertOutboxMessagesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<InsertOutboxMessagesInterceptor>();
            
            var connectionString = configuration.GetConnectionString("Database") 
                ?? throw new ArgumentNullException("Connection string 'Database' not found.");

            options.UseNpgsql(connectionString)
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<MediatREventPublisher>();
        services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
        services.AddScoped<TL.ResilientCore.Application.Abstractions.Messaging.IInboxService, Services.Inbox.InboxService>();
        services.AddHostedService<ProcessOutboxMessagesJob>();
        
        services.AddHttpContextAccessor();
        services.AddHttpClient();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddTlCaching(configuration);
        services.AddScoped<ICacheService, TlCacheServiceAdapter>();
        services.AddRabbitMqMessaging(configuration);

        return services;
    }
}