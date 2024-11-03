using System.Text;

namespace SatisfactoryAccounting.Model;

public class BasicSolution
{
    private const double FloatingPointTolerance = 0.0001;
    public SatisfactoryModel Model { get; }
    public ItemRates DesiredProducts { get; }
    public ItemRates AvailableProducts { get; }
    public List<SolutionComponent> Components { get; } = new();

    public BasicSolution(SatisfactoryModel model, IEnumerable<ItemRate> desiredProducts, IEnumerable<ItemRate>? availableProducts = null)
    {
        Model = model;
        DesiredProducts = new ItemRates(desiredProducts);
        AvailableProducts = new ItemRates(Model.Recipes.Classes.Select(rd => new ItemRate(rd.ClassName, double.MaxValue)));
        if (availableProducts != null) AvailableProducts = new ItemRates(AvailableProducts.Concat(availableProducts));

        foreach (var desiredProduct in DesiredProducts)
        {
            AddDesiredProduct(desiredProduct);
        }
    }

    private void AddDesiredProduct(ItemRate item)
    {
        if (Model.ResourceDescriptors.Classes.Any(c => c.ClassName == item.ItemClassName)) return;
        
        SolutionComponent? component = null;
        var existingComponents = Components.Where(c => c.Recipe.Product.Any(p => p.ItemClassName == item.ItemClassName)).ToArray();
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
        };
    }
    
    public class SolutionComponent(Recipe recipe)
    {
        public Recipe Recipe { get; } = recipe;
        public double Multiplier => Recipe.GetSmallestRequiredMultiplierToMake(Desired.Select(i => new ItemRate(i.Key, i.Value)).ToList());
        public ItemRates Output => Recipe.Product.Scale(Multiplier);
        public ItemRates Input => Recipe.Ingredients.Scale(Multiplier);
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

        private Dictionary<string, double> Desired { get; } = recipe.Product.ToDictionary(r => r.ItemClassName, r => 0d);
        
        public IEnumerable<ItemRate> AddOutput(ItemRate itemRate)
        {
            var oldMultiplier = Multiplier;
            Desired[itemRate.ItemClassName] += itemRate.Amount;
            var newMultiplier = Multiplier;
            var oldRequirements = Recipe.Ingredients.Scale(oldMultiplier);
            var newRequirements = Recipe.Ingredients.Scale(newMultiplier);
            foreach (var newRequirement in newRequirements)
            {
                var oldRequirement =
                    oldRequirements.SingleOrDefault(ir => ir.ItemClassName == newRequirement.ItemClassName);
                if (oldRequirement == null)
                {
                    yield return newRequirement;
                }
                else if (Math.Abs(oldRequirement.Amount - newRequirement.Amount) > FloatingPointTolerance)
                {
                    yield return oldRequirement with { Amount = newRequirement.Amount - oldRequirement.Amount };
                }
            }
        }
    }

    private Recipe GetRecipe(string className)
    {
        return Model.Recipes.Classes.Single(recipe => recipe.Product.Any(product => product.ItemClassName == className) && !recipe.IsAlternate);
    }
}