namespace SatisfactoryAccounting.Model;

public class SimpleOutputComputation(SatisfactoryModel model, ItemDescriptor item, int outputPerMinute)
{
    public List<ResourceSolution> SolveProduction()
    {
        var recipes = model.Recipes.Where(recipe =>
            recipe.ProductPerMinute.Any(product => product.ClassName == item.ClassName)).ToList();

        return [];
    }
}

public record ResourceSolution(string RecipeName, List<ItemRate> OutputPerMinute, double Efficiency, int NbrMachines);