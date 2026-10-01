using TL.ResilientCore.Domain.Primitives;
using TL.ResilientCore.Domain.Shared;

namespace TL.ResilientCore.Domain.Entities;

/// <summary>
/// Representa a entidade agregada de Cliente no domínio.
/// </summary>
public sealed class Cliente : AggregateRoot
{
    /// <summary>
    /// Obtém o nome completo ou razão social do cliente.
    /// </summary>
    public string Nome { get; private set; } = string.Empty;

    /// <summary>
    /// Obtém o endereço de e-mail de contato do cliente.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Indica se o cadastro do cliente está ativo no sistema.
    /// </summary>
    public bool Ativo { get; private set; }

    private Cliente(Guid id, string nome, string email) : base(id)
    {
        Nome = nome;
        Email = email;
        Ativo = true;
    }

    private Cliente() { }

    /// <summary>
    /// Factory Method responsável por instanciar um novo Cliente protegendo suas invariantes.
    /// </summary>
    /// <param name="nome">Nome do cliente.</param>
    /// <param name="email">E-mail do cliente.</param>
    /// <returns>Resultado contendo a instância de Cliente ou erro de validação.</returns>
    public static Result<Cliente> Create(string nome, string email)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Cliente>(new Error("Cliente.NomeInvalido", "O nome do cliente é obrigatório."));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure<Cliente>(new Error("Cliente.EmailInvalido", "O e-mail informado é inválido."));

        var cliente = new Cliente(Guid.NewGuid(), nome.Trim(), email.Trim());
        cliente.RaiseDomainEvent(new ClienteCreatedDomainEvent(cliente.Id, cliente.Nome, cliente.Email));

        return Result.Success(cliente);
    }

    /// <summary>
    /// Desativa o cadastro do cliente.
    /// </summary>
    public void Desativar()
    {
        Ativo = false;
    }

    /// <summary>
    /// Ativa o cadastro do cliente.
    /// </summary>
    public void Ativar()
    {
        Ativo = true;
    }
}

/// <summary>
/// Evento de domínio disparado quando um novo Cliente é cadastrado no sistema.
/// </summary>
/// <param name="ClienteId">Identificador único do cliente.</param>
/// <param name="Nome">Nome do cliente.</param>
/// <param name="Email">E-mail do cliente.</param>
public sealed record ClienteCreatedDomainEvent(Guid ClienteId, string Nome, string Email) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
