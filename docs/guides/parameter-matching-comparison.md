[Home](../../README.md) > [Guides](.) > Parameter Matching Comparison

# Parameter Matching Comparison

KnockOff uses standard C# lambdas for parameter matching — conditionals, named parameters, and tuple destructuring replace framework-specific matchers. No `It.Is<>`, no `Arg.Is<>`, no `x.ArgAt<>()`.

---

## The Comparison

### The scenario

A pricing service with mixed parameter types and a user search with same-typed parameters:

<!-- snippet: parammatch-interfaces -->
```cs
public interface IPricingService
{
    decimal GetPrice(string product, int quantity);
    void RecordSale(string product, int quantity);
}

public interface IUserSearch
{
    string? FindUser(string firstName, string lastName);
}
```
<!-- endSnippet -->

### KnockOff

**Conditional return — standard C# lambda:**

<!-- snippet: parammatch-conditional -->
```cs
// Standard C# conditional — no matchers needed
stub.GetPrice.Return((product, qty) => qty > 10 ? 8.99m : 9.99m);
```
<!-- endSnippet -->

No matchers. The `qty > 10` check is plain C#.

**Exact value matching with When():**

<!-- snippet: parammatch-specific-values -->
```cs
// Exact value matching — no Arg.Is<> or It.Is<>
stub.GetPrice.When(("widget", 5)).Return(49.95m);
stub.GetPrice.When(("gadget", 1)).Return(29.99m);
```
<!-- endSnippet -->

**Named parameters disambiguate same-typed args:**

<!-- snippet: parammatch-named-params -->
```cs
// Both params are string — names come from the lambda, not index math
stub.FindUser.Return((firstName, lastName) =>
    firstName == "Jane" && lastName == "Doe" ? "jane.doe" : null);
```
<!-- endSnippet -->

Both parameters are `string`. The lambda names `firstName` and `lastName` come from C# — no `x.ArgAt<string>(0)` or `x.ArgAt<string>(1)`.

**Built-in argument capture:**

<!-- snippet: parammatch-capture -->
```cs
// Built-in capture — no Callback<> or Arg.Do<> setup
var tracking = stub.RecordSale.Call((product, qty) => { });

IPricingService pricing = stub;
pricing.RecordSale("widget", 3);

// Tuple destructuring with named fields
var (product, quantity) = tracking.LastArgs;
```
<!-- endSnippet -->

No `Callback<>` pre-setup. No `Arg.Do<>`. The tracking object captures arguments automatically.

**When() with Return() fallback:**

<!-- snippet: parammatch-fallback -->
```cs
// When() for specifics, Return() as fallback
stub.GetPrice.When(("premium-widget", 1)).Return(99.99m);
stub.GetPrice.Return((product, qty) => qty * 9.99m);
```
<!-- endSnippet -->

When() matches specific values first. Unmatched calls fall through to Return().

**Predicate matching on multiple parameters:**

<!-- snippet: parammatch-predicate -->
```cs
// Predicate on multiple params — standard C# lambda
stub.GetPrice
    .When(args => args.quantity > 100).Return(7.99m)
    .ThenCall((product, qty) => qty > 10 ? 8.99m : 9.99m);
```
<!-- endSnippet -->

### Moq

```csharp
var mock = new Mock<IPricingService>();

// Conditional return — requires It.IsAny<> for each param, then Returns<T1, T2>
mock.Setup(x => x.GetPrice(It.IsAny<string>(), It.IsAny<int>()))
    .Returns<string, int>((product, qty) => qty > 10 ? 8.99m : 9.99m);

// Exact value matching
mock.Setup(x => x.GetPrice("widget", 5)).Returns(49.95m);
mock.Setup(x => x.GetPrice("gadget", 1)).Returns(29.99m);

// Named parameters — still need It.IsAny<> to match, then access via Returns<>
mock.Setup(x => x.FindUser(It.IsAny<string>(), It.IsAny<string>()))
    .Returns<string, string>((firstName, lastName) =>
        firstName == "Jane" && lastName == "Doe" ? "jane.doe" : null);

// Argument capture — requires pre-setup with Callback<>
string? capturedProduct = null;
int capturedQty = 0;
mock.Setup(x => x.RecordSale(It.IsAny<string>(), It.IsAny<int>()))
    .Callback<string, int>((product, qty) =>
    {
        capturedProduct = product;
        capturedQty = qty;
    });
mock.Object.RecordSale("widget", 3);
// capturedProduct == "widget", capturedQty == 3

// Predicate matching — It.Is<> per parameter
mock.Setup(x => x.GetPrice(It.IsAny<string>(), It.Is<int>(qty => qty > 100)))
    .Returns(7.99m);
```

**Pain points:**
- Every "match any" parameter needs `It.IsAny<T>()` — even when you only care about one parameter
- `.Returns<string, int>((product, qty) => ...)` duplicates the parameter types from the Setup
- Argument capture requires a separate `.Callback<T1, T2>(...)` setup with variables declared beforehand
- `It.Is<T>(predicate)` per parameter — can't write a single predicate across multiple parameters
- Mixing specific values with `It.IsAny<>` is awkward: `GetPrice("widget", It.IsAny<int>())`

### NSubstitute

```csharp
var pricing = Substitute.For<IPricingService>();

// Conditional return — Arg.Any<> for each param, then index-based access
pricing.GetPrice(Arg.Any<string>(), Arg.Any<int>())
    .Returns(x => x.ArgAt<int>(1) > 10 ? 8.99m : 9.99m);

// Exact value matching
pricing.GetPrice("widget", 5).Returns(49.95m);
pricing.GetPrice("gadget", 1).Returns(29.99m);

// Named parameters — index-based access for same-typed params
var search = Substitute.For<IUserSearch>();
search.FindUser(Arg.Any<string>(), Arg.Any<string>())
    .Returns(x => x.ArgAt<string>(0) == "Jane" && x.ArgAt<string>(1) == "Doe"
        ? "jane.doe" : null);

// Argument capture — requires Arg.Do<> setup per parameter
string? capturedProduct = null;
int capturedQty = 0;
pricing.RecordSale(
    Arg.Do<string>(x => capturedProduct = x),
    Arg.Do<int>(x => capturedQty = x));
pricing.RecordSale("widget", 3);
// capturedProduct == "widget", capturedQty == 3

// Predicate matching — Arg.Is<> per parameter
pricing.GetPrice(Arg.Any<string>(), Arg.Is<int>(qty => qty > 100))
    .Returns(7.99m);
```

**Pain points:**
- `x.ArgAt<string>(0)` vs `x.ArgAt<string>(1)` — index-based, no names. Easy to swap when both are `string`
- Argument capture needs `Arg.Do<T>()` for each parameter — one variable and one matcher per captured arg
- `Arg.Any<T>()` is required for every "don't care" parameter
- Single-parameter predicates only — can't write `(product, qty) => qty > 100` as one expression
- Ambient matcher state: `Arg.Is<>` calls must be in the right order and can conflict in complex setups

---

## Summary

| Capability | KnockOff | Moq | NSubstitute |
|------------|----------|-----|-------------|
| Conditional return | `Return((product, qty) => qty > 10 ? 8.99m : 9.99m)` | `It.IsAny<>` + `.Returns<T1, T2>(lambda)` | `Arg.Any<>` + `x.ArgAt<T>(index)` |
| Exact values | `When(("widget", 5)).Return(49.95m)` | `GetPrice("widget", 5).Returns(49.95m)` | `GetPrice("widget", 5).Returns(49.95m)` |
| Same-typed params | `(firstName, lastName) => ...` — named | `Returns<string, string>((a, b) => ...)` — named via Returns | `x.ArgAt<string>(0)`, `x.ArgAt<string>(1)` — index-based |
| Argument capture | `tracking.LastArgs` — automatic | `.Callback<T1, T2>(...)` — pre-setup | `Arg.Do<T>(x => ...)` — per parameter |
| Multi-param predicate | `When((product, qty) => qty > 100)` | `It.Is<T>()` per parameter only | `Arg.Is<T>()` per parameter only |
| Fallback behavior | `When(...).Return(...)` + `Return(fallback)` | Last `.Setup()` wins | Last `.Returns()` wins |
