using FluentAssertions;
namespace SatisfactoryAccounting.Tests;

public class UnitTest1(ModelFixture fixture) : IClassFixture<ModelFixture>
{
    [Fact]
    public async Task CanLoadModel()
    {
        var model = await fixture.GetModel();

        model.Should().NotBeNull();
    }
}