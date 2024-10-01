using FluentAssertions;
using SatisfactoryAccounting.Model;

namespace SatisfactoryAccounting.Tests;

public class UnitTest1
{
    [Fact]
    public async Task CanLoadModel()
    {
        var serializedModel = await File.ReadAllTextAsync(Path.Join("Resources", "en-US.json"));
        var model = SatisfactoryModelFactory.FromDocsJson(serializedModel);
        model.Should().NotBeNull();
    }
}