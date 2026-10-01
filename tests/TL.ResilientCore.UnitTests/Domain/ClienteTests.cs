using FluentAssertions;
using TL.ResilientCore.Domain.Entities;
using Xunit;

namespace TL.ResilientCore.UnitTests.Domain;

public class ClienteTests
{
    [Fact]
    public void Create_ComDadosValidos_DeveRetornarSucessoEEmitirDomainEvent()
    {
        var nome = "Empresa Alpha Ltda";
        var email = "contato@alpha.com.br";

        var result = Cliente.Create(nome, email);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Nome.Should().Be(nome);
        result.Value.Email.Should().Be(email);
        result.Value.Ativo.Should().BeTrue();

        var domainEvents = result.Value.GetDomainEvents();
        domainEvents.Should().HaveCount(1);
        domainEvents.First().Should().BeOfType<ClienteCreatedDomainEvent>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ComNomeInvalido_DeveRetornarFalha(string? nomeInvalido)
    {
        var result = Cliente.Create(nomeInvalido!, "teste@empresa.com");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Cliente.NomeInvalido");
    }

    [Theory]
    [InlineData("")]
    [InlineData("email-invalido-sem-arroba")]
    [InlineData(null)]
    public void Create_ComEmailInvalido_DeveRetornarFalha(string? emailInvalido)
    {
        var result = Cliente.Create("Cliente Valido", emailInvalido!);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Cliente.EmailInvalido");
    }

    [Fact]
    public void Desativar_DeveAlterarStatusParaInativo()
    {
        var cliente = Cliente.Create("Cliente Ativo", "ativo@empresa.com").Value;

        cliente.Desativar();

        cliente.Ativo.Should().BeFalse();
    }
}

