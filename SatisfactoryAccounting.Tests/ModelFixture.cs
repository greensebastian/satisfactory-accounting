using SatisfactoryAccounting.Model;

namespace SatisfactoryAccounting.Tests;

public class ModelFixture
{
    private SemaphoreSlim Lock { get; } = new(1, 1);
    private SatisfactoryModel? Model { get; set; }
    public async Task<SatisfactoryModel> GetModel()
    {
        await Lock.WaitAsync();
        try
        {
            if (Model is null)
            {
                var serializedModel = await File.ReadAllTextAsync(Path.Join("Resources", "en-US.json"));
                Model = SatisfactoryModelFactory.FromDocsJson(serializedModel);
            }

            return Model;
        }
        finally
        {
            Lock.Release();
        }
    }
}