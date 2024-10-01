using SatisfactoryAccounting.Model;

namespace SatisfactoryAccounting.Tests;

public class ModelFixture : IAsyncLifetime
{
    public SatisfactoryModel Model { get; private set; } = new();
    
    public async Task InitializeAsync()
    {
        var serializedModel = await File.ReadAllTextAsync(Path.Join("Resources", "en-US.json"));
        Model = SatisfactoryModelFactory.FromDocsJson(serializedModel);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}