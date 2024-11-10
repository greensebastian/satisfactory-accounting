using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace SatisfactoryAccounting.Model;

public class SatisfactoryModel
{
    public const double FloatingPointTolerance = 0.000001;
    public SatisfactoryResourceCollection<Recipe> Recipes { get; }
    public SatisfactoryResourceCollection<ItemDescriptor> ItemDescriptors { get; }
    public SatisfactoryResourceCollection<ResourceDescriptor> ResourceDescriptors { get; }

    public SatisfactoryModel(List<IntermediateSatisfactoryResourceCollection> resourceCollections)
    {
        Recipes = resourceCollections.Single(rc => rc.NativeClass == Recipe.NativeClass).ToType<Recipe>();
        var allItemDescriptors = resourceCollections.Where(rc => ItemDescriptor.NativeClasses.Contains(rc.NativeClass));
        var joinedItemDescriptors = new JArray();
        foreach (var itemDescriptorCollection in allItemDescriptors)
        {
            foreach (var itemDescriptor in itemDescriptorCollection.Classes)
            {
                joinedItemDescriptors.Add(itemDescriptor);
            }
        }

        ItemDescriptors = new IntermediateSatisfactoryResourceCollection(ItemDescriptor.NativeClasses[0], joinedItemDescriptors).ToType<ItemDescriptor>();
        ResourceDescriptors = resourceCollections.Single(rc => rc.NativeClass == ResourceDescriptor.NativeClass).ToType<ResourceDescriptor>();
    }

    public ItemDescriptor? FindItemDescriptor(string match) =>
        ItemDescriptors.Classes.FirstOrDefault(r =>
            r.DisplayName.Contains(match, StringComparison.InvariantCultureIgnoreCase) ||
            r.ClassName.Contains(match, StringComparison.InvariantCultureIgnoreCase));
    
    public ItemDescriptor? ItemDescriptorByClassName(string className) =>
        ItemDescriptors.Classes.FirstOrDefault(r => r.ClassName.Equals(className, StringComparison.InvariantCultureIgnoreCase));
    
    public ResourceDescriptor? ResourceDescriptorByClassName(string className) =>
        ResourceDescriptors.Classes.FirstOrDefault(r => r.ClassName.Equals(className, StringComparison.InvariantCultureIgnoreCase));

    public bool IsBaseResource(string className)
    {
        if (ResourceDescriptorByClassName(className) is not null)
        {
            return true;
        }

        return false;
    }
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
    public bool HasIngredient(string itemClassName) => Ingredients.Any(i => i.ItemClassName == itemClassName);
    public ItemRates Product => _product ??= ItemRate.FromItemClassCollectionString(MProduct, ManufacturingDuration);
    public bool HasProduct(string itemClassName) => Product.Any(i => i.ItemClassName == itemClassName);
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

    public bool IsAlternate { get; } = ClassName.StartsWith("Recipe_Alternate_") || ClassName == "Recipe_PureAluminumIngot_C";

    public double GetSmallestRequiredMultiplierToMake(List<ItemRate> items) => items
        .Select(desired => desired.Amount / Product.Single(p => p.ItemClassName == desired.ItemClassName).Amount).Max();

    public bool IsUnpackaging { get; } = ClassName.StartsWith("Recipe_Unpackage");
}

public class ItemRates(IEnumerable<ItemRate> items) : IReadOnlyCollection<ItemRate>
{
    private readonly List<ItemRate> _items = items.ToList();

    public ItemRate this[string className] => _items.Single(i => i.ItemClassName == className);
    
    private ItemRates Scale(double factor) => new(this.Select(i => i with { Amount = i.Amount * factor } ));

    public static ItemRates operator *(ItemRates rates, double factor) => rates.Scale(factor);
    
    public static ItemRates operator +(ItemRates first, ItemRates second)
    {
        var combined = new Dictionary<string, double>();
        foreach (var itemRateToAdd in first.Concat(second))
        {
            combined.TryAdd(itemRateToAdd.ItemClassName, 0);
            combined[itemRateToAdd.ItemClassName] += itemRateToAdd.Amount;
        }

        return new ItemRates(combined.Where(i => Math.Abs(i.Value) > SatisfactoryModel.FloatingPointTolerance).Select(i => new ItemRate(i.Key, i.Value)));
    }

    public static ItemRates operator -(ItemRates first, ItemRates second) => first + second * -1;
    
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
    public static readonly IReadOnlyList<string> NativeClasses =
    [
        "/Script/CoreUObject.Class'/Script/FactoryGame.FGItemDescriptor'",
        "/Script/CoreUObject.Class'/Script/FactoryGame.FGItemDescriptorNuclearFuel'",
        "/Script/CoreUObject.Class'/Script/FactoryGame.FGItemDescriptorBiomass'",
        "/Script/CoreUObject.Class'/Script/FactoryGame.FGItemDescriptorPowerBoosterFuel'"
    ];
    public string DisplayName { get; } = MDisplayName;
}

