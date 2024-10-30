using Newtonsoft.Json;

namespace SatisfactoryAccounting.Model;

public class SatisfactoryModelFactory
{
    public static SatisfactoryModel FromDocsJson(string json)
    {
        var allData = JsonConvert.DeserializeObject<List<IntermediateSatisfactoryResourceCollection>>(json);
        return new SatisfactoryModel(allData ?? []);
    }
}