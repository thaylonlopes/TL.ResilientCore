using AuditLogger.DependencyInjection;
using ClaimsPrincipalExtensionsLibrary;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using TL.HealthCheck;
using TL.MiddlewareLibrary.Extensions;
using TL.ResilientCore.Api.Extensions;
using TL.ResilientCore.Application;
using TL.ResilientCore.Application.Features.Clientes.Queries.GetClientes;
using TL.ResilientCore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuditLogger(builder.Configuration);
builder.Services.AddLightweightHealthChecks();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Identity:Authority"];
        options.Audience = builder.Configuration["Identity:Audience"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "TL.ResilientCore API",
            Version = "v1",
            Description = "Template .NET Corporativo com Clean Architecture, CQRS e Resiliência.",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Thaylon Lopes",
                Url = new Uri("https://github.com/thaylonlopes")
            }
        };
        return Task.CompletedTask;
    });
}); 

var app = builder.Build();

app.UseExceptionHandling();
app.UseRequestTiming();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Powered-By", "TL.ResilientCore");
    await next();
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapLightweightHealthChecks();

app.MapGet("/", () => Results.Ok(new
{
    status = "Healthy",
    service = "TL.ResilientCore"
}))
.WithName("HealthCheck")
.WithOpenApi();

app.MapGet("/clientes", async (string? nome, bool? ativo, int? pageNumber, int? pageSize, ISender sender, CancellationToken ct) =>
{
    var query = new GetClientesQuery(nome, ativo, pageNumber ?? 1, pageSize ?? 10);
    var result = await sender.Send(query, ct);
    return result.ToHttpResult();
})
.WithName("GetClientes")
.WithOpenApi();

app.MapGet("/secure-data", (System.Security.Claims.ClaimsPrincipal user) =>
{
    return Results.Ok(new
    {
        message = "Acesso autorizado com sucesso.",
        user = user.FullName() ?? user.Identity?.Name,
        userId = user.ClaimSub(),
        email = user.Email(),
        claims = user.Claims.Select(c => new { c.Type, c.Value })
    });
}).RequireAuthorization();   

app.Logger.LogInformation("Iniciando serviço TL.ResilientCore");

app.Run();

public partial class Program { }