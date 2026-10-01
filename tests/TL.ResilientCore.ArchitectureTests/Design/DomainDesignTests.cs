using NetArchTest.Rules;
using TL.ResilientCore.Domain.Primitives;
using Xunit;

namespace TL.ResilientCore.ArchitectureTests.Design;

public class DomainDesignTests
{
   [Fact]
    public void Entities_NaoDevem_TerParametrosPublicos_NosConstrutores()
    {
        var domainAssembly = TL.ResilientCore.Domain.AssemblyReference.Assembly;

        var entityTypes = Types.InAssembly(domainAssembly)
            .That()
            .Inherit(typeof(Entity))
            .And()
            .AreNotAbstract()
            .GetTypes();

        var hasPublicConstructors = entityTypes
            .Any(t => t.GetConstructors().Any(c => c.IsPublic));

        Assert.False(hasPublicConstructors, "Entidades de domínio devem proteger suas invariantes usando construtores privados/protegidos e Factory Methods.");
    }

   [Fact]
    public void DomainEvents_DevemSer_Sealed()
    {
        var domainAssembly = TL.ResilientCore.Domain.AssemblyReference.Assembly;

        var result = Types.InAssembly(domainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain Events devem ser selados (sealed) para garantir imutabilidade e evitar hierarquias complexas de eventos.");
    }
}