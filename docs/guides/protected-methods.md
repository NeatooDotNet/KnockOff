[Home](../../README.md) > [Guides](.) > Protected Methods

# Protected Methods

KnockOff generates public interceptors for protected members on class stubs. You configure, verify, and sequence protected methods using the same API as public methods — fully compile-time safe.

This applies to **class stubs only** (Patterns 3, 4, 6, 9). Interface stubs have no access modifiers.

---

## The Comparison

### The scenario

A base class with a protected abstract method used by a public template method:

<!-- snippet: protected-base-class -->
```cs
public abstract class ServiceBase
{
    protected virtual string Tag { get; set; } = "";
    protected abstract string GetInternalId();
    protected virtual string FormatTag() => $"[{Tag}]";

    public string GetDescription() => $"{GetInternalId()}: {FormatTag()}";
}
```
<!-- endSnippet -->

### KnockOff

The stub wrapper exposes all interceptors — including protected ones — as public properties. Same `Return`/`Call`/`Verify` API:

<!-- snippet: protected-configure-verify -->
```cs
var stub = new Stubs.ServiceBase();

// Configure protected abstract method via public interceptor
stub.GetInternalId.Return("ID-42");

ServiceBase service = stub.Object;
var desc = service.GetDescription();
// desc == "ID-42: []"

// Verify it was called
stub.GetInternalId.Verify(Called.Once);
```
<!-- endSnippet -->

Protected virtual methods fall back to the base implementation when unconfigured:

<!-- snippet: protected-virtual-fallback -->
```cs
var stub = new Stubs.ServiceBase();
stub.Tag.Get("myTag");
stub.GetInternalId.Return("id");

// FormatTag is NOT configured — falls back to base: $"[{Tag}]"
ServiceBase service = stub.Object;
var desc = service.GetDescription();
// desc == "id: [myTag]"
```
<!-- endSnippet -->

Configure to override the base:

<!-- snippet: protected-override-base -->
```cs
stub.FormatTag.Return("custom-format");
```
<!-- endSnippet -->

Sequences work too:

<!-- snippet: protected-sequences -->
```cs
stub.GetInternalId
    .Return(() => "first-id")
    .ThenReturn(() => "second-id");
```
<!-- endSnippet -->

All member types are supported: methods, properties, indexers, and events.

### Moq

Moq provides `Mock.Protected()` which uses string-based method names:

```csharp
var mock = new Mock<ServiceBase>();

// String-based — no compile-time safety
mock.Protected()
    .Setup<string>("GetInternalId")
    .Returns("ID-42");

// Different matcher syntax: ItExpr instead of It
mock.Protected()
    .Setup<string>("FormatTag")
    .Returns("custom-format");

ServiceBase service = mock.Object;
var desc = service.GetDescription();

// Verify with string name
mock.Protected()
    .Verify("GetInternalId", Times.Once());
```

**Pain points:**
- Method names are strings — rename the method and tests still compile but fail at runtime
- Must use `ItExpr` instead of `It` for argument matching
- Alternatively, define a mapping interface and use `mock.Protected().As<IMapping>()` — extra boilerplate

### NSubstitute

NSubstitute has **no built-in support** for protected members. The workaround is a manual test subclass:

```csharp
// Manual workaround: create a test subclass
public class TestServiceBase : ServiceBase
{
    public string GetInternalIdResult { get; set; } = "";

    protected override string GetInternalId() => GetInternalIdResult;
}

// Usage
var testService = new TestServiceBase { GetInternalIdResult = "ID-42" };
var desc = testService.GetDescription();

// No built-in verification — you'd need to add your own tracking
```

**Pain points:**
- Must create a manual subclass for every base class you want to test
- No verification support
- No sequencing
- Each protected member needs manual plumbing

---

## Summary

| Capability | KnockOff | Moq | NSubstitute |
|------------|----------|-----|-------------|
| Configure protected methods | `stub.GetInternalId.Return(...)` | `mock.Protected().Setup<T>("name")` | Manual subclass |
| Compile-time safety | Yes — interceptor is typed | No — string method names | N/A |
| Verification | `stub.GetInternalId.Verify(Called.Once)` | `mock.Protected().Verify("name", Times.Once())` | Not available |
| Sequences | `.Return(...).ThenReturn(...)` | Limited (Moq 4.18+, string-based) | Not available |
| Virtual base fallback | Automatic when unconfigured | Must configure explicitly | Manual in subclass |
| Properties / Indexers / Events | Same API | Limited Protected() support | Manual in subclass |
