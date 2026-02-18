[Home](../../README.md) > [Guides](.) > Ref/Out Parameters

# Ref/Out Parameters

KnockOff generates custom delegate types for methods with `ref`/`out` parameters. Configuration uses natural C# lambda syntax with `ref`/`out` keywords — no special matchers or index-based access.

---

## The Comparison

### The scenario

A common `TryGet` pattern and a `ref` mutation:

<!-- snippet: refout-interfaces -->
```cs
public interface IOutParameterService
{
    bool TryGetValue(string key, out string value);
    void GetDefaults(out int width, out int height);
}

public interface IRefParameterService
{
    void Increment(ref int value);
    bool TryUpdate(string key, ref string value);
    void Swap(ref int a, ref int b);
}
```
<!-- endSnippet -->

### KnockOff

The generator creates custom delegates. Configuration uses standard C# lambda syntax:

**Out parameters:**

<!-- snippet: refout-tryget -->
```cs
var stub = new Stubs.IOutParameterService();

stub.TryGetValue.Call((string key, out string value) =>
{
    value = "found-" + key;
    return true;
});

IOutParameterService service = stub;
service.TryGetValue("myKey", out string result);
// result == "found-myKey"
```
<!-- endSnippet -->

**Backed by a dictionary (realistic scenario):**

<!-- snippet: refout-dictionary -->
```cs
var data = new Dictionary<string, string> { ["key1"] = "value1" };

stub.TryGetValue.Call((string key, out string value) =>
    data.TryGetValue(key, out value!));
```
<!-- endSnippet -->

**Void methods with multiple outs:**

<!-- snippet: refout-void-multiple-outs -->
```cs
stub.GetDefaults.Call((out int w, out int h) =>
{
    w = 1920;
    h = 1080;
});
```
<!-- endSnippet -->

**Ref parameters:**

<!-- snippet: refout-ref-simple -->
```cs
var stub = new Stubs.IRefParameterService();

stub.Increment.Call((ref int v) => v++);

IRefParameterService service = stub;
int value = 5;
service.Increment(ref value);
// value == 6
```
<!-- endSnippet -->

**Swap pattern:**

<!-- snippet: refout-ref-swap -->
```cs
stub.Swap.Call((ref int a, ref int b) => (a, b) = (b, a));
```
<!-- endSnippet -->

**Mixed normal + out + ref parameters:**

<!-- snippet: refout-mixed -->
```cs
stub.Process.Call((string input, out string output, ref int counter) =>
{
    output = input.ToUpperInvariant();
    counter++;
    return true;
});
```
<!-- endSnippet -->

Sequences, verification, and `LastArg` all work naturally:

<!-- snippet: refout-sequences -->
```cs
stub.TryGetValue
    .Call((string key, out string value) => { value = "first"; return true; })
    .ThenReturn((string key, out string value) => { value = "second"; return true; });
```
<!-- endSnippet -->

<!-- snippet: refout-verification -->
```cs
stub.TryGetValue.Verify(Called.Exactly(2));
var lastKey = stub.TryGetValue.LastArg; // captures non-out params only
```
<!-- endSnippet -->

### Moq

Moq requires `It.Ref<T>.IsAny` (different from `It.IsAny<T>()`) and has different syntax for Setup vs Callback:

```csharp
var mock = new Mock<IOutParameterService>();

// Out parameter setup
var outValue = "found";
mock.Setup(x => x.TryGetValue("myKey", out outValue))
    .Returns(true);

// For any key: requires It.Ref<T>.IsAny
mock.Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<string>.IsAny))
    .Returns(new TryGetValueDelegate((string key, out string value) =>
    {
        value = "found-" + key;
        return true;
    }));

// Ref parameter setup
mock.Setup(x => x.Increment(ref It.Ref<int>.IsAny))
    .Callback(new IncrementDelegate((ref int v) => v++));
```

**Pain points:**
- `It.Ref<T>.IsAny` is a different syntax from `It.IsAny<T>()` — easy to mix up
- Must define custom delegate types yourself for callbacks (KnockOff generates them)
- Out parameter setup with specific values requires a local variable
- Callback syntax differs from Returns syntax for ref/out methods

### NSubstitute

NSubstitute uses index-based access for out/ref parameters:

```csharp
var sub = Substitute.For<IOutParameterService>();

// Out parameter — configure via Returns + indexer assignment
sub.TryGetValue(Arg.Any<string>(), out Arg.Any<string>())
    .Returns(x =>
    {
        x[1] = "found-" + x.ArgAt<string>(0);  // index-based
        return true;
    });

// Ref parameter
sub.When(x => x.Increment(ref Arg.Any<int>()))
    .Do(x =>
    {
        x[0] = x.ArgAt<int>(0) + 1;  // index-based
    });
```

**Pain points:**
- `x[1] = "found"` — must count parameter positions manually
- No named parameters — less readable than a typed lambda
- Easy to use the wrong index, especially with mixed parameter kinds

---

## Summary

| Capability | KnockOff | Moq | NSubstitute |
|------------|----------|-----|-------------|
| Out parameters | `(string key, out string value) => { ... }` | `out It.Ref<string>.IsAny` + custom delegate | `x[1] = "value"` (index-based) |
| Ref parameters | `(ref int v) => v++` | `ref It.Ref<int>.IsAny` + custom delegate | `x[0] = newValue` (index-based) |
| Mixed params | Same lambda syntax | Different syntax per param kind | Count indexes across all params |
| Custom delegates | Generated automatically | Must define yourself | Not needed (index-based) |
| Sequences | `.ThenReturn(...)` / `.ThenCall(...)` | `.SetupSequence()` (limited ref/out support) | `.Returns(first, second)` |
| Verification | `stub.Method.Verify(Called.Once)` | `mock.Verify(...)` | `sub.Received()` |
