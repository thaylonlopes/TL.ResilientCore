using System.Security.Claims;
using ClaimsPrincipalExtensionsLibrary;
using Microsoft.AspNetCore.Http;
using TL.ResilientCore.Application.Interfaces;

namespace TL.ResilientCore.Infrastructure.Services;

/// <summary>
/// Serviço de infraestrutura responsável por fornecer informações do usuário autenticado no contexto HTTP atual.
/// </summary>
/// <param name="httpContextAccessor">Acessor do contexto HTTP.</param>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;

    /// <summary>
    /// Obtém o identificador único (Guid) do usuário a partir da claim sub ou NameIdentifier.
    /// </summary>
    public Guid? UserId => Guid.TryParse(_user?.ClaimSub() ?? _user?.Claim(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    /// <summary>
    /// Obtém o e-mail do usuário autenticado a partir das claims JWT.
    /// </summary>
    public string? Email => _user?.Email() ?? _user?.Claim(ClaimTypes.Email)?.Value;

    /// <summary>
    /// Indica se o usuário está autenticado no contexto atual.
    /// </summary>
    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;
}
