using EnumExtensionsLibrary;
using FluentAssertions;
using TL.ResilientCore.Domain.Enums;
using Xunit;

namespace TL.ResilientCore.UnitTests.Domain;

public class StatusProcessamentoEnumTests
{
    [Fact]
    public void GetValues_DeveRetornarTodosOsValoresDoEnum()
    {
        var values = EnumExtension.GetValues<StatusProcessamento>();

        values.Should().NotBeNull();
        values.Should().HaveCount(3);
        values.Should().Contain([StatusProcessamento.Pendente, StatusProcessamento.Concluido, StatusProcessamento.FalhaIntegracao]);
    }

    [Fact]
    public void GetNames_DeveRetornarNomesDosEnumsCorretamente()
    {
        var names = EnumExtension.GetNames<StatusProcessamento>();

        names.Should().NotBeNull();
        names.Should().Contain(nameof(StatusProcessamento.Pendente));
        names.Should().Contain(nameof(StatusProcessamento.Concluido));
        names.Should().Contain(nameof(StatusProcessamento.FalhaIntegracao));
    }

    [Fact]
    public void GetAllDescriptions_DeveRetornarDescricoesMapeadas()
    {
        var descriptions = EnumExtension.GetAllDescriptions<StatusProcessamento>();

        descriptions.Should().NotBeNull();
        descriptions[StatusProcessamento.Pendente].Should().Be("Processamento Pendente");
        descriptions[StatusProcessamento.Concluido].Should().Be("Processamento Concluído com Sucesso");
        descriptions[StatusProcessamento.FalhaIntegracao].Should().Be("Falha na Comunicação com Serviço Externo");
    }
}

