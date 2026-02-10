# Stub Overrides Reference

Stub overrides are `protected override` methods and properties you define in a standalone stub's partial class. They provide reusable default behavior that can be superseded per-test with `Return()`/`Call()`/`Get()`/`Set()`.

**Standalone patterns only.** Inline stubs (patterns 5-9) generate the entire class — no partial available for overrides.

---

## Base Class Pattern

KnockOff generates a base class with `virtual protected` members suffixed with underscore (`_`). You override these to provide defaults:

```csharp
// Generated base class (invisible):
// protected virtual string Process_(string input) => default!;
// protected virtual int Calculate_(int a, int b) => default!;

// Your overrides in the partial class:
[KnockOff]
public partial class MyStub : IService { }

public partial class MyStub
{
    protected override string Process_(string input) => $"[Processed: {input}]";
    protected override int Calculate_(int a, int b) => a + b;
}
```

The compiler enforces signature correctness — typos or wrong parameter types produce CS0115 errors.

---

## Method Stub Overrides

### Basic Usage

```csharp
var stub = new MyStub();
IService service = stub;

service.Process("hello"); // "[Processed: hello]" (from override)
service.Calculate(3, 4);  // 7 (from override)
```

### Return/Call Supersedes

`Return()` and `Call()` take priority over stub overrides per-test:

```csharp
// Default behavior from override
service.Process("hello"); // "[Processed: hello]"

// Supersede with Return for this test
stub.Process.Return(input => $"[Override: {input}]");
service.Process("hello"); // "[Override: hello]"

// Constant value
stub.Process.Return("constant");
service.Process("anything"); // "constant"

// Void methods: Call supersedes
stub.Execute.Call(cmd => callbackInvoked = true);
```

### Tracking

Interceptors use **clean names** (no underscore suffix):

```csharp
stub.Process.Verify(Called.Exactly(3));
stub.Process.LastArg;          // Last argument
stub.Calculate.LastArgs;       // Named tuple (int a, int b)
stub.Process.Reset();          // Clears tracking, preserves Return config
```

### When Chains

Stub override stubs support the full When chain API:

```csharp
stub.Process.When("special").Return("[SPECIAL]");

service.Process("special"); // "[SPECIAL]" (When matched)
service.Process("normal");  // Stub override result (When didn't match)
```

Priority: When > Sequences > Return/Call > **Stub Override**

---

## Property Stub Overrides

### Get-Only

```csharp
// Generated: protected virtual int Count_ => default!;
protected override int Count_ => _count;
```

### Set-Only

```csharp
// Generated: protected virtual string Setting_ { set { } }
protected override string Setting_ { set => _setting = value; }
```

### Get/Set

```csharp
// Generated: protected virtual string Name_ { get => default!; set { } }
protected override string Name_
{
    get => _name;
    set => _name = value;
}
```

### Get/Set Supersedes

```csharp
// Get supersedes stub override getter
stub.Count.Get(999);
service.Count; // 999 (Get wins, not stub override)

// Set supersedes stub override setter
stub.Name.Set(v => captured = v);
service.Name = "test"; // captured = "test" (Set wins)
```

### Property Tracking

```csharp
stub.Count.VerifyGet(Called.Exactly(3));
stub.Name.VerifySet(Called.Twice);
stub.Name.LastSetValue; // Last value passed to setter
```

---

## Overloaded Methods

Each overload gets its own virtual method in the base class:

```csharp
// Generated:
// protected virtual string Format_(string input) => default!;
// protected virtual string Format_(string input, bool uppercase) => default!;

protected override string Format_(string input) => input.ToUpper();
protected override string Format_(string input, bool uppercase) =>
    uppercase ? input.ToUpper() : input;
```

### Partial Overload Coverage

Override only some overloads — unoverridden ones use the interceptor path:

```csharp
// Only Format(string) is overridden
protected override string Format_(string input) => $"[User: {input}]";

// Format(string, bool) and Format(string, bool, int) are NOT overridden
// They use Return() or return default
```

---

## Mixed Stubs

Override some methods, configure others:

```csharp
public partial class MyStub
{
    protected override string WithOverride_(string input) => $"[User: {input}]";
    // WithoutOverride_ is NOT overridden
}

// Methods WITH override use it as default
service.WithOverride("test");    // "[User: test]"

// Methods WITHOUT override need configuration or return default
stub.WithoutOverride.Return((input) => $"[Configured: {input}]");
service.WithoutOverride("test"); // "[Configured: test]"
```

---

## Async Stub Overrides

```csharp
// Generated: protected virtual Task<string> ProcessAsync_(string input) => default!;
protected override async Task<string> ProcessAsync_(string input)
{
    await Task.Delay(1);
    return $"[Async: {input}]";
}

// Generated: protected virtual ValueTask<int> ComputeAsync_(int value) => default!;
protected override async ValueTask<int> ComputeAsync_(int value)
{
    await Task.Yield();
    return value * 2;
}
```

---

## Strict Mode

Stub overrides **bypass strict mode** — they ARE the configuration:

```csharp
[KnockOff(Strict = true)]
public partial class StrictStub : IService { }

public partial class StrictStub
{
    protected override string Process_(string input) => $"[Strict: {input}]";
    // Other methods will throw if called without Return/Call
}

service.Process("test"); // Works — override IS the config
// service.Calculate(1, 2); // Throws — no override, no Return
```

---

## Applicable Patterns

| Pattern | Stub Overrides? |
|---------|:-:|
| 1. Standalone | Yes |
| 2. Generic Standalone | Yes |
| 3. Standalone Class | Yes |
| 4. Generic Standalone Class | Yes |
| 5-9. Inline patterns | No |

---

## Not Supported

- **Generic methods** — excluded from base class pattern. Use `Of<T>()` instead.
- **Inline stubs** — entire class is generated, no partial for overrides.
- **Indexer overrides** — see separate design (not yet supported).

---

## Reset Behavior

`Reset()` clears tracking but **preserves** Return/Call/Get/Set configuration:

```csharp
stub.Calculate.Return((a, b) => a * b);
service.Calculate(3, 4); // 12
stub.Calculate.Reset();

service.Calculate(5, 6); // 30 (Return still active)
stub.Calculate.Verify(Called.Once); // Only 1 call after reset
```
