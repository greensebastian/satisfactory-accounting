using FluentAssertions;
using SatisfactoryAccounting.Model;

namespace SatisfactoryAccounting.Tests;

public class UnitTest1(ModelFixture fixture) : IClassFixture<ModelFixture>
{
    [Fact]
    public async Task CanLoadModel()
    {
        var model = await fixture.GetModel();

        model.Should().NotBeNull();
    }
    
    [Fact]
    public async Task CanSolveIronRod()
    {
        var model = await fixture.GetModel();

        var solution = new BasicSolution(model, [new ItemRate(model.FindItemDescriptor("Iron Rod")!.ClassName, 60)]);
        solution.Components.Should().HaveCount(2);
    }
    
    [Fact]
    public async Task CanSolveHeavyModularFrame()
    {
        var model = await fixture.GetModel();

        var solution = new BasicSolution(model, [new ItemRate(model.FindItemDescriptor("Heavy Modular Frame")!.ClassName, 1)]);
        solution.Should().NotBeNull();
    }
    
    [Fact]
    public async Task CanSolveRotor()
    {
        var model = await fixture.GetModel();

        var solution = new BasicSolution(model, [new ItemRate(model.FindItemDescriptor("Rotor")!.ClassName, 1)]);
        solution.Should().NotBeNull();
    }
    
    [Fact]
    public async Task CanSolveUraniumFuelRod()
    {
        var model = await fixture.GetModel();

        var solution = new BasicSolution(model, [new ItemRate(model.FindItemDescriptor("Encased Uranium Cell")!.ClassName, 1)]);
        solution.Should().NotBeNull();
    }
}