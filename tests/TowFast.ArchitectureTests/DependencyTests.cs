using System.Reflection;
using TowFast.Application.Abstractions;
using TowFast.Domain.Common;

namespace TowFast.ArchitectureTests;

public sealed class DependencyTests
{
    [Fact]
    public void Domain_does_not_reference_adapters()
    {
        var references = typeof(Entity).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToHashSet();
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references);
        Assert.DoesNotContain("Microsoft.AspNetCore", references);
        Assert.DoesNotContain("TowFast.Infrastructure", references);
        Assert.DoesNotContain("TowFast.Api", references);
    }

    [Fact]
    public void Application_does_not_reference_infrastructure_or_api()
    {
        var references = typeof(ITowFastStore).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToHashSet();
        Assert.DoesNotContain("TowFast.Infrastructure", references);
        Assert.DoesNotContain("TowFast.Api", references);
    }
}
