using FluentAssertions;
using TL.ResilientCore.Domain.Shared;
using Xunit;

public class ResultTests
{
    [Fact]
    public void Success_DeveRetornarIsSuccessTrue_E_ErrorNone()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        
        result.Error.Should().Be(Error.None); 
    }

    [Fact]
    public void Failure_DeveRetornarIsSuccessFalse_E_ErrorInformado()
    {
        var erroEsperado = new Error("Teste.Erro", "Mensagem de erro simulada.");

        var result = Result.Failure(erroEsperado);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(erroEsperado);
        result.Error.Code.Should().Be("Teste.Erro");
    }
}