using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace SatisfactoryAccounting.Model;

public class SatisfactoryModel
{
    public SatisfactoryResourceCollection<Recipe> Recipes { get; }
    public SatisfactoryResourceCollection<ItemDescriptor> ItemDescriptors { get; }
    public SatisfactoryResourceCollection<ResourceDescriptor> ResourceDescriptors { get; }

    public SatisfactoryModel(List<IntermediateSatisfactoryResourceCollection> resourceCollections)
    {
        Recipes = resourceCollections.Single(rc => rc.NativeClass == Recipe.NativeClass).ToType<Recipe>();
        ItemDescriptors = resourceCollections.Single(rc => rc.NativeClass == ItemDescriptor.NativeClass).ToType<ItemDescriptor>();
        ResourceDescriptors = resourceCollections.Single(rc => rc.NativeClass == ResourceDescriptor.NativeClass).ToType<ResourceDescriptor>();
    }

    public ItemDescriptor? FindItemDescriptor(string match) =>
        ItemDescriptors.Classes.FirstOrDefault(r =>
            r.DisplayName.Contains(match, StringComparison.InvariantCultureIgnoreCase) ||
            r.ClassName.Contains(match, StringComparison.InvariantCultureIgnoreCase));
    
    public ItemDescriptor? ItemDescriptorByClassName(string className) =>
        ItemDescriptors.Classes.FirstOrDefault(r => r.ClassName.Equals(className, StringComparison.InvariantCultureIgnoreCase));
}

// ReSharper disable once ClassNeverInstantiated.Global
public record IntermediateSatisfactoryResourceCollection(string NativeClass, JArray Classes)
{
    public SatisfactoryResourceCollection<T> ToType<T>() => new(NativeClass, Classes.ToObject<List<T>>() ?? []);
}

public static class Regexes
{
    public static Regex ItemClassAndAmountGroup { get; } = new(@"\((ItemClass=""[^""]+"",Amount=\d+)\)", RegexOptions.Compiled);
    public static Regex ItemClass { get; } = new(@"ItemClass=""[^""]+\.([^""']+)'""", RegexOptions.Compiled);
    public static Regex Amount { get; } = new(@"Amount=(\d+)", RegexOptions.Compiled);
    public static Regex ProducedIn { get; } = new(@"((?<=\.)([a-zA-Z0-9_]+))+", RegexOptions.Compiled);
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
    private ItemRates? _ingredients;
    private ItemRates? _product;
    public const string NativeClass = "/Script/CoreUObject.Class'/Script/FactoryGame.FGRecipe'";
    public string DisplayName { get; } = MDisplayName;
    public ItemRates Ingredients => _ingredients ??= ItemRate.FromItemClassCollectionString(MIngredients, ManufacturingDuration);
    public ItemRates Product => _product ??= ItemRate.FromItemClassCollectionString(MProduct, ManufacturingDuration);
    public TimeSpan ManufacturingDuration { get; } = TimeSpan.FromSeconds(double.Parse(MManufactoringDuration, CultureInfo.InvariantCulture));
    public List<string> ProducedIn { get; } = Regexes.ProducedIn.Matches(MProducedIn).Select(match => match.Value).ToList();
    public string ProducedInReadable
    {
        get
        {
            var buildPrefixed = string.Join(", ", ProducedIn.Where(name => name.StartsWith("Build_")));
            return buildPrefixed.Length > 0 ? buildPrefixed : string.Join(", ", ProducedIn);
        }
    }

    public bool IsAlternate { get; } = ClassName.StartsWith("Recipe_Alternate_");

    public double GetSmallestRequiredMultiplierToMake(List<ItemRate> items) => items
        .Select(desired => desired.Amount / Product.Single(p => p.ItemClassName == desired.ItemClassName).Amount).Max();
}

public class ItemRates(IEnumerable<ItemRate> items) : IReadOnlyCollection<ItemRate>
{
    private readonly List<ItemRate> _items = items.ToList();

    public ItemRate this[string className] => _items.Single(i => i.ItemClassName == className);
    
    public ItemRates Scale(double factor) => new(this.Select(i => i with { Amount = i.Amount * factor } ));
    public IEnumerator<ItemRate> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _items.Count;
}

public record ItemRate(string ItemClassName, double Amount)
{
    public static ItemRates FromItemClassCollectionString(string itemClassCollectionString, TimeSpan manufacturingDuration)
    {
        var components = Regexes.ItemClassAndAmountGroup.Matches(itemClassCollectionString);
        var perMinuteMultiplier = 60.0 / manufacturingDuration.TotalSeconds;
        var itemRates = components.Select(match => new ItemRate(
            Regexes.ItemClass.Match(match.Value).Groups[1].Value,
            double.Parse(Regexes.Amount.Match(match.Value).Groups[1].Value, CultureInfo.InvariantCulture) *
            perMinuteMultiplier));
        return new ItemRates(itemRates);
    }
    
    public ItemRate Copy() => new(ItemClassName, Amount);
}

public record ResourceDescriptor(
    string ClassName,
    string MDecalSize,
    string MPingColor,
    string MCollectSpeedMultiplier,
    string MManualMiningAudioName,
    string MDisplayName,
    string MDescription,
    string MAbbreviatedDisplayName,
    string MStackSize,
    string MCanBeDiscarded,
    string MRememberPickUp,
    string MEnergyValue,
    string MRadioactiveDecay,
    string MForm,
    string MGasType,
    string MSmallIcon,
    string MPersistentBigIcon,
    string MCrosshairMaterial,
    string MDescriptorStatBars,
    string MIsAlienItem,
    string MSubCategories,
    string MMenuPriority,
    string MFluidColor,
    string MGasColor,
    string MCompatibleItemDescriptors,
    string MClassToScanFor,
    string MScannableType,
    string MShouldOverrideScannerDisplayText,
    string MScannerDisplayText,
    string MScannerLightColor,
    string MNeedsPickUpMarker,
    string MResourceSinkPoints
)
{
    public const string NativeClass = "/Script/CoreUObject.Class'/Script/FactoryGame.FGResourceDescriptor'";
    public string DisplayName { get; } = MDisplayName;
}

public record ItemDescriptor(
    string ClassName,
    string MDisplayName,
    string MDescription,
    string MAbbreviatedDisplayName,
    string MStackSize,
    string MCanBeDiscarded,
    string MRememberPickUp,
    string MEnergyValue,
    string MRadioactiveDecay,
    string MForm,
    string MGasType,
    string MSmallIcon,
    string MPersistentBigIcon,
    string MCrosshairMaterial,
    string MDescriptorStatBars,
    string MIsAlienItem,
    string MSubCategories,
    string MMenuPriority,
    string MFluidColor,
    string MGasColor,
    string MCompatibleItemDescriptors,
    string MClassToScanFor,
    string MScannableType,
    string MShouldOverrideScannerDisplayText,
    string MScannerDisplayText,
    string MScannerLightColor,
    string MNeedsPickUpMarker,
    string MResourceSinkPoints
)
{
    public const string NativeClass = "/Script/CoreUObject.Class'/Script/FactoryGame.FGItemDescriptor'";
    public string DisplayName { get; } = MDisplayName;
}

