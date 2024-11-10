using System.Text;

namespace SatisfactoryAccounting.Model;

public class BasicSolution
{
    public SatisfactoryModel Model { get; }
    public ItemRates DesiredProducts { get; }
    public ItemRates BaseResources { get; }
    public ItemRates AvailableProducts { get; }
    public List<SolutionComponent> Components { get; } = new();
    public List<List<SolutionComponent>> ComponentsByDependencyTier { get; }
    public ItemRates Input { get; }
    public ItemRates TotalItems { get; }

    public IEnumerable<List<SolutionComponent>> ComputeComponentsByDependencyTier()
    {
        var componentsToMake = Components.ToList();
        var availableResources = Model.ResourceDescriptors.Classes.Select(c => c.ClassName).ToHashSet();
        var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        while (componentsToMake.Count > 0)
        {
            if (timeout.IsCancellationRequested)
                throw new ApplicationException("Failed to find sorted component dependencies");
            var ableToMake = componentsToMake.Where(component =>
                component.Input.All(i => availableResources.Contains(i.ItemClassName))).ToList();
            foreach (var component in ableToMake)
            {
                var resourcesCreated = component.Output.Select(i => i.ItemClassName);
                foreach (var item in resourcesCreated)
                {
                    availableResources.Add(item);
                }

                componentsToMake.Remove(component);
            }
            
            yield return ableToMake;
        }
    }

    public BasicSolution(SatisfactoryModel model, IEnumerable<ItemRate> desiredProducts, IEnumerable<ItemRate>? availableProducts = null)
    {
        Model = model;
        DesiredProducts = new ItemRates(desiredProducts);
        BaseResources = GetBaseResources(model);
        AvailableProducts = BaseResources;
        if (availableProducts != null) AvailableProducts = new ItemRates(AvailableProducts.Concat(availableProducts));

        foreach (var desiredProduct in DesiredProducts)
        {
            AddDesiredProduct(desiredProduct);
        }

        ComponentsByDependencyTier = ComputeComponentsByDependencyTier().Reverse().ToList();
        Input = ComponentsByDependencyTier.Last()
            .Aggregate(new ItemRates([]), (rates, component) => rates + component.Input);
        TotalItems = ComponentsByDependencyTier.AsEnumerable().Reverse().Aggregate(new ItemRates([]),
            (tierRates, tierComponents) => tierComponents.Aggregate(tierRates,
                (rates, component) => rates - component.Input + component.Output));
    }

    private void AddDesiredProduct(ItemRate item)
    {
        if (BaseResources.Any(c => c.ItemClassName == item.ItemClassName)) return;
        
        SolutionComponent? component = null;
        var existingComponents = Components
            .Where(c => c.Recipe.HasProduct(item.ItemClassName))
            .Where(c => !c.Recipe.HasIngredient(item.ItemClassName)) // Avoid self-feeding recipes
            .ToArray();
        foreach (var existingComponent in existingComponents)
        {
            var outputOfItem = existingComponent.Recipe.Product.Single(p => p.ItemClassName == item.ItemClassName).Amount;
            if (component == null ||
                outputOfItem > component.Recipe.Product.Single(p => p.ItemClassName == item.ItemClassName).Amount)
            {
                component = existingComponent;
            }
        }

        if (component == null)
        {
            component = new SolutionComponent(GetRecipe(item.ItemClassName));
            Components.Add(component);
        }

        foreach (var itemRate in component.AddOutput(item))
        {
            AddDesiredProduct(itemRate);
        }
    }
    
    public class SolutionComponent(Recipe recipe)
    {
        public Recipe Recipe { get; } = recipe;
        public double Multiplier => Recipe.GetSmallestRequiredMultiplierToMake(Desired.Select(i => new ItemRate(i.Key, i.Value)).ToList());
        public ItemRates Output => Recipe.Product * Multiplier;
        public ItemRates Input => Recipe.Ingredients * Multiplier;
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Recipe: {Recipe.DisplayName}");
            sb.AppendLine($"Multiplier: {Multiplier}");
            sb.AppendLine("Output:");
            foreach (var output in Output)
            {
                sb.AppendLine($"\t{output.ItemClassName}: {output.Amount}");
            }
            sb.AppendLine("Input:");
            foreach (var input in Input)
            {
                sb.AppendLine($"\t{input.ItemClassName}: {input.Amount}");
            }
            return sb.ToString();
        }

        private Dictionary<string, double> Desired { get; } = recipe.Product.ToDictionary(r => r.ItemClassName, _ => 0d);
        
        public IEnumerable<ItemRate> AddOutput(ItemRate itemRate)
        {
            var oldMultiplier = Multiplier;
            Desired[itemRate.ItemClassName] += itemRate.Amount;
            var newMultiplier = Multiplier;
            var oldRequirements = Recipe.Ingredients * oldMultiplier;
            var newRequirements = Recipe.Ingredients * newMultiplier;
            foreach (var newRequirement in newRequirements)
            {
                var oldRequirement =
                    oldRequirements.SingleOrDefault(ir => ir.ItemClassName == newRequirement.ItemClassName);
                if (oldRequirement == null)
                {
                    yield return newRequirement;
                }
                else if (Math.Abs(oldRequirement.Amount - newRequirement.Amount) > SatisfactoryModel.FloatingPointTolerance)
                {
                    yield return oldRequirement with { Amount = newRequirement.Amount - oldRequirement.Amount };
                }
            }
        }
    }

    private Recipe GetRecipe(string itemClassName)
    {
        var recipes = Model.Recipes.Classes
            .Where(recipe => recipe.HasProduct(itemClassName))
            .Where(recipe => !recipe.IsAlternate)
            .Where(recipe => !recipe.IsUnpackaging)
            .ToList();
        if (recipes.Count > 1)
        {
            return itemClassName switch
            {
                "Desc_Plastic_C" => recipes.Single(r => r.ClassName == "Recipe_Plastic_C"),
                "Desc_SulfuricAcid_C" => recipes.Single(r => r.ClassName == "Recipe_SulfuricAcid_C"),
                "Desc_Silica_C" => recipes.Single(r => r.ClassName == "Recipe_Silica_C"),
                _ => throw new ApplicationException(
                    $"Multiple recipes outputting {itemClassName} found: {string.Join(", ", recipes.Select(r => r.ClassName))}")
            };
        }
        if (recipes.Count > 0)
        {
            return recipes[0];
        }

        throw new ApplicationException($"No recipes outputting {itemClassName} found");
    }

    private ItemRates GetBaseResources(SatisfactoryModel model) => new(model.ResourceDescriptors.Classes.Select(c => new ItemRate(c.ClassName, double.MaxValue)));
}