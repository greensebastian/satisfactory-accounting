using System.Globalization;
using System.Text.Json;

namespace SatisfactoryAccounting.Model;

public class SatisfactoryModelFactory
{
    private class IntermediateDocsModel
    {
        public string NativeClass { get; set; } = string.Empty;

        public Dictionary<string, JsonElement>[] Classes { get; set; } = [];
    }

    public static SatisfactoryModel FromDocsJson(string json)
    {
        var allData = JsonSerializer.Deserialize<IntermediateDocsModel[]>(json);

        return ParseModelComponentTypes(allData ?? []);
    }

    private static SatisfactoryModel ParseModelComponentTypes(IList<IntermediateDocsModel> models)
    {
        var model = new SatisfactoryModel
        {
            Id = Guid.NewGuid(),
            ResourceDescriptors = ParseClass<ResourceDescriptor>(models),
            ItemDescriptors = ParseClass<ItemDescriptor>(models),
            Recipes = ParseClass<Recipe>(models),
            BuildableManufacturers = ParseClass<BuildableManufacturer>(models)
        };

        return model;
    }

    private static List<T> ParseClass<T>(IEnumerable<IntermediateDocsModel> allModels) where T : class, new()
    {
        var output = new List<T>();
        foreach (var model in allModels)
        {
            if (!model.NativeClass.EndsWith($"FG{typeof(T).Name}'")) continue;
            var properties = typeof(T).GetProperties();
            foreach (var rawClass in model.Classes)
            {
                var result = new T();
                foreach (var propertyInfo in properties)
                {
                    var valueExists = rawClass.TryGetValue($"m{propertyInfo.Name}", out var jsonElement) || rawClass.TryGetValue(propertyInfo.Name, out jsonElement);
                    if (!valueExists) continue;
                    switch (propertyInfo.Name)
                    {
                        case nameof(Recipe.Ingredients):
                        case nameof(Recipe.Product):
                        {
                            var parts = new List<ItemRate>();
                            string value = GetJsonValue(jsonElement);
                            value = value.Trim('(', ')');
                            var resources = value.Split("),(");
                            foreach (var resource in resources)
                            {
                                if (string.IsNullOrEmpty(resource)) continue;
                                
                                var itemClass = NormalizeClassName(resource
                                    .Split(',').Single(part => part.StartsWith("ItemClass")));
                                var amount = double.Parse(resource
                                    .Split(',').Single(part => part.StartsWith("Amount"))
                                    .Split('=').Last());
                                parts.Add(new ItemRate
                                {
                                    ClassName = itemClass,
                                    Amount = amount
                                });
                            }
                            propertyInfo.SetValue(result, parts);
                            break;
                        }
                        case nameof(Recipe.ProducedIn):
                        {
                            string value = GetJsonValue(jsonElement);
                            value = value.Trim('(', ')');
                            var productionSources = value.Split(",");
                            var producedIn = productionSources.Select(NormalizeClassName).ToList();
                            propertyInfo.SetValue(result, producedIn);
                            break;
                        }
                        default:
                            propertyInfo.SetValue(result, GetJsonValue(jsonElement));
                            break;
                    }
                }

                output.Add(result);
            }
        }
            

        return output;
    }

    private static string NormalizeClassName(string className) => className.Split('.').Last().Trim('\\', '\'', '\"');

    private static dynamic GetJsonValue(JsonElement jsonElement)
    {
        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.Array: return jsonElement.GetRawText();
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;
            case JsonValueKind.Number: return jsonElement.GetDouble();
            case JsonValueKind.Object: return jsonElement.GetRawText();
            case JsonValueKind.String:
            {
                var value = jsonElement.GetString();
                if (double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var doubleResult)) return doubleResult;
                if (Enum.TryParse(value, out StackSize stackSizeResult)) return stackSizeResult;
                if (Enum.TryParse(value, out Form formResult)) return formResult;
                if (bool.TryParse(value, out var boolResult)) return boolResult;
                return value;
            }
            case JsonValueKind.True: return true;
            case JsonValueKind.Undefined: return null;
            default: return null;
        }
    }
}