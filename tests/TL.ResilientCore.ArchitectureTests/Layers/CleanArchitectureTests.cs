using NetArchTest.Rules;
using Xunit;

public class CleanArchitectureTests
{
    [Fact]
    public void DomainLayer_NaoDeve_TerDependencias_DaInfrastructure()
    {
       var domainAssembly = TL.ResilientCore.Domain.AssemblyReference.Assembly;
        
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("TL.ResilientCore.Infrastructure")
            .GetResult();

        
        Assert.True(result.IsSuccessful);
    }
    
    [Fact]
    public void DomainLayer_NaoDeve_TerDependencias_DaApplication()
    {
        var domainAssembly = TL.ResilientCore.Domain.AssemblyReference.Assembly;

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("TL.ResilientCore.Application")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void ApplicationLayer_NaoDeve_TerDependencias_DaInfrastructure()
    {
        var applicationAssembly = TL.ResilientCore.Application.AssemblyReference.Assembly;

        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("TL.ResilientCore.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}