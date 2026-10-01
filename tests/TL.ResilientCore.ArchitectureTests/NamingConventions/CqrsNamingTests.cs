using MediatR;
using NetArchTest.Rules;
using TL.ResilientCore.Domain.Primitives;
using Xunit;

namespace TL.ResilientCore.ArchitectureTests.NamingConventions;

public class CqrsNamingTests
{
   [Fact]
    public void Handlers_DevemTerminarComA_PalavraHandler()
    {
        var applicationAssembly = TL.ResilientCore.Application.AssemblyReference.Assembly;

        var result = Types.InAssembly(applicationAssembly)
            .That()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        var failingTypes = result.FailingTypeNames != null 
            ? string.Join(", ", result.FailingTypeNames) 
            : string.Empty;

        Assert.True(result.IsSuccessful, $"Classes que violaram a convenção: {failingTypes}");
    }

    [Fact]
    public void DomainEvents_DevemTerminarComA_PalavraDomainEvent_Ou_Event()
    {
        var domainAssembly = TL.ResilientCore.Domain.AssemblyReference.Assembly;

        var result = Types.InAssembly(domainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .Should()
            .HaveNameEndingWith("DomainEvent")
            .Or()
            .HaveNameEndingWith("Event")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}