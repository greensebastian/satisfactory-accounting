using SatisfactoryAccounting.Model;

namespace SatisfactoryAccounting.Client;

public class SatisfactoryModelProvider(HttpClient httpClient)
{
    private SemaphoreSlim CacheSemaphore { get; } = new(1, 1);
    private SatisfactoryModel? CachedSatisfactoryModel { get; set; }
    
    public async Task<SatisfactoryModel> GetModel()
    {
        await CacheSemaphore.WaitAsync();

        try
        {
            if (CachedSatisfactoryModel is null)
            {
                var docsJson = await httpClient.GetStringAsync("resources/en-US.json");
                CachedSatisfactoryModel = SatisfactoryModelFactory.FromDocsJson(docsJson);
            }
            return CachedSatisfactoryModel;
        }
        finally
        {
            CacheSemaphore.Release();
        }
    }
}