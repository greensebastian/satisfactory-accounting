using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace SatisfactoryAccounting.Model;

public class SatisfactoryModel
{
    public SatisfactoryResourceCollection<Recipe> Recipes { get; }

    public SatisfactoryModel(List<IntermediateSatisfactoryResourceCollection> resourceCollections)
    {
        Recipes = resourceCollections.Single(rc => rc.NativeClass == Recipe.NativeClass).ToType<Recipe>();
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
public record IntermediateSatisfactoryResourceCollection(string NativeClass, JArray Classes)
{
    public SatisfactoryResourceCollection<T> ToType<T>() =>
        new SatisfactoryResourceCollection<T>(NativeClass, Classes.ToObject<List<T>>() ?? []);
}

public static class Regexes
{
    public static Regex ItemClassAndAmountGroup { get; } = new(@"\((ItemClass=""[^""]+"",Amount=\d+)\)", RegexOptions.Compiled);
    public static Regex ItemClass { get; } = new(@"ItemClass=""[^""]+\.([^""]+)""", RegexOptions.Compiled);
    public static Regex Amount { get; } = new(@"Amount=(\d+)", RegexOptions.Compiled);
}

public record SatisfactoryResourceCollection<TResource>(string NativeClass, List<TResource> Classes);

public record Recipe(
    string ClassName,
    string FullName,
    string MDisplayName,
    string MIngredients,
    string MProduct,
    string MManufacturingMenuPriority,
    string MManufactoringDuration,
    string MManualManufacturingMultiplier,
    string MProducedIn,
    string MRelevantEvents,
    string MVariablePowerConsumptionConstant,
    string MVariablePowerConsumptionFactor
)
{
    private List<ItemRate>? _ingredients;
    private List<ItemRate>? _product;
    public const string NativeClass = "/Script/CoreUObject.Class'/Script/FactoryGame.FGRecipe'";
    public string DisplayName { get; } = MDisplayName;
    public List<ItemRate> Ingredients => _ingredients ??= ItemRate.FromItemClassCollectionString(MIngredients, ManufacturingDuration);
    public List<ItemRate> Product => _product ??= ItemRate.FromItemClassCollectionString(MProduct, ManufacturingDuration);
    public TimeSpan ManufacturingDuration { get; } = TimeSpan.FromSeconds(double.Parse(MManufactoringDuration, CultureInfo.InvariantCulture));
}

public record ItemRate(string ItemClass, double Amount)
{
    public static List<ItemRate> FromItemClassCollectionString(string itemClassCollectionString, TimeSpan manufacturingDuration)
    {
        var components = Regexes.ItemClassAndAmountGroup.Matches(itemClassCollectionString);
        var perMinuteMultiplier = 60.0 / manufacturingDuration.TotalSeconds;
        return components.Select(match => new ItemRate(Regexes.ItemClass.Match(match.Value).Groups[1].Value,
            double.Parse(Regexes.Amount.Match(match.Value).Groups[1].Value, CultureInfo.InvariantCulture) * perMinuteMultiplier)).ToList();
    }
};