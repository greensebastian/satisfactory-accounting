using System.Text;

namespace SatisfactoryAccounting.Model;

public class SatisfactoryModel
{
    public Guid Id { get; set; }
    public List<Recipe> Recipes { get; set; } = [];

    public List<ResourceDescriptor> ResourceDescriptors { get; set; } = [];

    public List<ItemDescriptor> ItemDescriptors { get; set; } = [];

    public List<BuildableManufacturer> BuildableManufacturers { get; set; } = [];
}

public class Descriptor
{
    public string ClassName { get; set; }
    public string FullName { get; set; }
    public string DisplayName { get; set; }
    public string AbbreviatedDisplayName { get; set; }
    public string Description { get; set; }
    public StackSize StackSize { get; set; }
    public Form Form { get; set; }
    public bool CanBeDiscarded { get; set; }
    public bool RememberPickUp { get; set; }
    public double EnergyValue { get; set; }
    public double RadioactiveDecay { get; set; }
    public string SmallIcon { get; set; }
    public string PersistentBigIcon { get; set; }
    public string SubCategories { get; set; }
    public double MenuPriority { get; set; }
    public string FluidColor { get; set; }
    public string GasColor { get; set; }
    public double BuildMenuPriority { get; set; }
}

public class ResourceDescriptor : Descriptor
{
    public double DecalSize { get; set; }
    public string PingColor { get; set; }
    public double CollectSpeedMultiplier { get; set; }
    public string ManualMiningAudioName { get; set; }
}

public class ItemDescriptor : Descriptor
{
    public double ResourceSinkPoints { get; set; }
}

public class Recipe
{
    public string ClassName { get; set; }
    public string FullName { get; set; }
    public string DisplayName { get; set; }
    public List<ItemRate> Ingredients { get; set; }
    public List<ItemRate> Product { get; set; }
    public double ManufacturingMenuPriority { get; set; }
    public double ManufactoringDuration { get; set; }
    public double ManualManufacturingMultiplier { get; set; }
    public List<string> ProducedIn { get; set; }
    public string RelevantEvents { get; set; }
    public double VariablePowerConsumptionConstant { get; set; }
    public double VariablePowerConsumptionFactor { get; set; }

    public List<ItemRate> IngredientsPerMinute => Ingredients.Select(ingredient => new ItemRate
    {
        ClassName = ingredient.ClassName,
        Amount = ingredient.Amount * 60 / ManufactoringDuration
    }).ToList();

    public List<ItemRate> ProductPerMinute => Product.Select(product => new ItemRate
    {
        ClassName = product.ClassName,
        Amount = product.Amount * 60 / ManufactoringDuration
    }).ToList();

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{DisplayName}\t{ClassName}");
        sb.AppendLine();

        sb.AppendLine("Input:");
        foreach (var ingredient in IngredientsPerMinute)
        {
            sb.AppendLine($"{ingredient.Amount}\t{ingredient.ClassName}");
        }
        sb.AppendLine();

        sb.AppendLine("Output:");
        foreach (var product in ProductPerMinute)
        {
            sb.AppendLine($"{product.Amount}\t{product.ClassName}");
        }

        return sb.ToString();
    }
}


public class BuildableManufacturer
{
    public string ClassName { get; set; }
    public bool IsPowered { get; set; }
    public string CurrentRecipeCheck { get; set; }
    public string PreviousRecipeCheck { get; set; }
    public string CurrentPotentialConvert { get; set; }
    public string CurrentRecipeChanged { get; set; }
    public double ManufacturingSpeed { get; set; }
    public string FactoryInputConnections { get; set; }
    public string PipeInputConnections { get; set; }
    public string FactoryOutputConnections { get; set; }
    public string PipeOutputConnections { get; set; }
    public double PowerConsumption { get; set; }
    public double PowerConsumptionExponent { get; set; }
    public bool DoesHaveShutdownAnimation { get; set; }
    public string OnHasPowerChanged { get; set; }
    public string OnHasProductionChanged { get; set; }
    public string OnHasStandbyChanged { get; set; }
    public double MinimumProducingTime { get; set; }
    public double MinimumStoppedTime { get; set; }
    public double NumCyclesForProductivity { get; set; }
    public bool CanChangePotential { get; set; }
    public double MinPotential { get; set; }
    public double MaxPotential { get; set; }
    public double MaxPotentialIncreasePerCrystal { get; set; }
    public StackSize FluidStackSizeDefault { get; set; }
    public double FluidStackSizeMultiplier { get; set; }
    public string OnReplicationDetailActorCreatedEvent { get; set; }
    public double EffectUpdateInterval { get; set; }
    public string CachedSkeletalMeshes { get; set; }
    public bool AddToSignificanceManager { get; set; }
    public double SignificanceRange { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public double MaxRenderDistance { get; set; }
    public string HighlightVector { get; set; }
    public bool AllowColoring { get; set; }
    public bool SkipBuildEffect { get; set; }
    public double BuildEffectSpeed { get; set; }
    public bool ForceNetUpdateOnRegisterPlayer { get; set; }
    public bool ToggleDormancyOnInteraction { get; set; }
    public bool ShouldShowHighlight { get; set; }
    public bool ShouldShowAttachmentPointVisuals { get; set; }
    public bool CreateClearanceMeshRepresentation { get; set; }
    public string AttachmentPoints { get; set; }
    public string InteractingPlayers { get; set; }
    public bool IsUseable { get; set; }
    public bool HideOnBuildEffectStart { get; set; }
    public bool ShouldModifyWorldGrid { get; set; }
}


public class ItemRate
{
    public string ClassName { get; set; }
    public double Amount { get; set; }
}

public enum StackSize
{
    SS_ONE,
    SS_SMALL,
    SS_MEDIUM,
    SS_BIG,
    SS_LARGE,
    SS_HUGE,
    SS_FLUID
}

public enum Form
{
    RF_INVALID,
    RF_LIQUID,
    RF_SOLID,
    RF_GAS
}