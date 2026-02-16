using KnockOff;

namespace KnockOff.Documentation.Samples.ParameterMatchingComparison;

// =============================================================================
// Interfaces for Parameter Matching Comparison
// =============================================================================

#region parammatch-interfaces
public interface IPricingService
{
    decimal GetPrice(string product, int quantity);
    void RecordSale(string product, int quantity);
}

public interface IUserSearch
{
    string? FindUser(string firstName, string lastName);
}
#endregion

// =============================================================================
// Inline stubs
// =============================================================================

[KnockOff<IPricingService>]
[KnockOff<IUserSearch>]
public partial class ParameterMatchingComparisonTests { }

// =============================================================================
// Test Methods
// =============================================================================

public partial class ParameterMatchingComparisonTests
{
    [Fact]
    public void ConditionalReturn_StandardLambda()
    {
        var stub = new Stubs.IPricingService();

        #region parammatch-conditional
        // Standard C# conditional — no matchers needed
        stub.GetPrice.Return((product, qty) => qty > 10 ? 8.99m : 9.99m);
        #endregion

        IPricingService pricing = stub;
        Assert.Equal(8.99m, pricing.GetPrice("widget", 20));
        Assert.Equal(9.99m, pricing.GetPrice("widget", 5));
    }

    [Fact]
    public void SpecificValues_WhenReturn()
    {
        var stub = new Stubs.IPricingService();

        #region parammatch-specific-values
        // Exact value matching — no Arg.Is<> or It.Is<>
        stub.GetPrice.When(("widget", 5)).Return(49.95m);
        stub.GetPrice.When(("gadget", 1)).Return(29.99m);
        #endregion

        IPricingService pricing = stub;
        Assert.Equal(49.95m, pricing.GetPrice("widget", 5));
        Assert.Equal(29.99m, pricing.GetPrice("gadget", 1));
    }

    [Fact]
    public void NamedParameters_SameType()
    {
        var stub = new Stubs.IUserSearch();

        #region parammatch-named-params
        // Both params are string — names come from the lambda, not index math
        stub.FindUser.Return((firstName, lastName) =>
            firstName == "Jane" && lastName == "Doe" ? "jane.doe" : null);
        #endregion

        IUserSearch search = stub;
        Assert.Equal("jane.doe", search.FindUser("Jane", "Doe"));
        Assert.Null(search.FindUser("John", "Smith"));
    }

    [Fact]
    public void ArgumentCapture_BuiltIn()
    {
        var stub = new Stubs.IPricingService();

        #region parammatch-capture
        // Built-in capture — no Callback<> or Arg.Do<> setup
        var tracking = stub.RecordSale.Call((product, qty) => { });

        IPricingService pricing = stub;
        pricing.RecordSale("widget", 3);

        // Tuple destructuring with named fields
        var (product, quantity) = tracking.LastArgs!.Value;
        #endregion

        Assert.Equal("widget", product);
        Assert.Equal(3, quantity);
    }

    [Fact]
    public void SpecificValues_WithFallback()
    {
        var stub = new Stubs.IPricingService();

        #region parammatch-fallback
        // When() for specifics, Return() as fallback
        stub.GetPrice.When(("premium-widget", 1)).Return(99.99m);
        stub.GetPrice.Return((product, qty) => qty * 9.99m);
        #endregion

        IPricingService pricing = stub;

        // Specific match takes priority
        Assert.Equal(99.99m, pricing.GetPrice("premium-widget", 1));

        // Everything else falls through to Return()
        Assert.Equal(29.97m, pricing.GetPrice("widget", 3));
    }

    [Fact]
    public void PredicateMatching()
    {
        var stub = new Stubs.IPricingService();

        #region parammatch-predicate
        // Predicate on multiple params — standard C# lambda
        stub.GetPrice
            .When(args => args.quantity > 100).Return(7.99m)
            .ThenCall((product, qty) => qty > 10 ? 8.99m : 9.99m);
        #endregion

        IPricingService pricing = stub;
        Assert.Equal(7.99m, pricing.GetPrice("widget", 200));
        Assert.Equal(8.99m, pricing.GetPrice("widget", 20));
        Assert.Equal(9.99m, pricing.GetPrice("widget", 5));
    }
}
