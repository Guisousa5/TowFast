using TowFast.Application.Common;

namespace TowFast.Application.Tests;

public sealed class PageResultTests
{
    [Fact]
    public void Calculates_total_pages()
    {
        var result = new PageResult<int>([1, 2], 2, 2, 5);
        Assert.Equal(3, result.TotalPages);
    }
}
