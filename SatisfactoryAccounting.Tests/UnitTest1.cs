using FluentAssertions;
using SatisfactoryAccounting.Model;

namespace SatisfactoryAccounting.Tests;

public class UnitTest1(ModelFixture fixture) : IClassFixture<ModelFixture>
{
    [Fact]
    public async Task CanLoadModel()
    {
        fixture.Model.Should().NotBeNull();
    }

    [Fact]
    public void CanSolveSimpleCase()
    {
        var computation = new SimpleOutputComputation(fixture.Model,
            fixture.Model.ItemDescriptors.First(i => i.DisplayName.Contains("Iron Rod")), 1);

        var solution = computation.SolveProduction();
        solution.Should().NotBeEmpty();
    }
}