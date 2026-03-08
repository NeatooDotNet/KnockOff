# Stub Overrides Reference

Stub overrides are `protected override` methods and properties you define in a standalone stub's partial class. They provide reusable default behavior that can be superseded per-test with `Return()`/`Call()`/`Get()`/`Set()`.

**Standalone patterns only.** Inline stubs (patterns 5-9) generate the entire class -- no partial available for overrides.

---

## Base Class Pattern

KnockOff generates a base class with `virtual protected` members suffixed with underscore (`_`). You override these to provide defaults:

<!-- snippet: stub-overrides-basic -->
```cs
[KnockOff]
public partial class StubOverridesRepoStub : IStubOverridesRepo
{
    // Protected override method with underscore suffix
    // This is the fallback when no Return is configured
    protected override User? GetUserById_(int id)
    {
        return new User { Id = id, Name = "Default User" };
    }

    protected override bool IsActive_(int userId)
    {
        return true; // Default: users are active
    }

    protected override decimal GetBalance_(int userId)
    {
        return 100.00m; // Default test balance
    }
}
```
<!-- endSnippet -->

The compiler enforces signature correctness -- typos or wrong parameter types produce CS0115 errors.

---

## Property Stub Overrides

Property overrides use the same underscore suffix convention. KnockOff generates virtual properties in the base class that you override (get-only, get/set, set-only):

<!-- snippet: stub-override-properties-interface-and-stub -->
```cs
public interface ISkillUserSvc
{
    int Count { get; }
    string Name { get; set; }
    string Setting { set; }
}

[KnockOff]
public partial class SkillUserSvcStub : ISkillUserSvc
{
    private int _count;
    private string _name = "";
    private string _setting = "";

    // Get-only property override
    protected override int Count_ => _count;

    // Get/set property override
    protected override string Name_
    {
        get => _name;
        set => _name = value;
    }

    // Set-only property override
    protected override string Setting_
    {
        set => _setting = value;
    }

    // Public methods for test setup
    public void SetCount(int value) => _count = value;
}
```
<!-- endSnippet -->

### Get/Set Supersedes Property Overrides

`Get()` and `Set()` take priority over property overrides per-test, just like `Return()`/`Call()` do for methods:

<!-- snippet: skill-stub-override-property-onget -->
```cs
var stub = new SkUserPropServiceStub();
stub.SetCount(42);
IUserService service = stub;

// Without Get: user property is called
var count1 = service.Count;  // Returns 42 (from Count_ override)

// With Get: Get supersedes user property (clean interceptor name)
stub.Count.Get(999);
var count2 = service.Count;  // Returns 999 (Get wins)
```
<!-- endSnippet -->

Priority: Get()/Set() > **Property Override** > Smart default

---

## Real-World Example: Standalone Stub with Constructor

Standalone stubs combine constructor parameters with stub overrides to create reusable, configurable stubs. Use constructors for required values and stub overrides for default behavior:

<!-- snippet: skill-stub-override-constructor -->
```cs
[KnockOff]
public partial class CurrentUserStub : ICurrentUser
{
    private long _userId;
    private string _role = "";

    public CurrentUserStub(long userId, string role) : this()
    {
        _userId = userId;
        _role = role;
    }

    protected override long UserId_ => _userId;
    protected override string Role_ => _role;
    protected override string DisplayName_ => $"Test {_role}";
    protected override bool IsInRole_(string role) => role == _role;
}
```
<!-- endSnippet -->

Usage in tests:

<!-- snippet: skill-stub-override-constructor-usage -->
```cs
var stub = new CurrentUserStub(42L, "ROLE_PROVIDER");
ICurrentUser currentUser = stub;

Assert.Equal(42L, currentUser.UserId);
Assert.Equal("ROLE_PROVIDER", currentUser.Role);
Assert.Equal("Test ROLE_PROVIDER", currentUser.DisplayName);
Assert.True(currentUser.IsInRole("ROLE_PROVIDER"));
Assert.False(currentUser.IsInRole("ROLE_ADMIN"));

// Per-test override when needed
stub.IsInRole.Call((role) => true);
Assert.True(currentUser.IsInRole("ROLE_ADMIN"));  // Now returns true for all
```
<!-- endSnippet -->

---

## Anti-Pattern: Factory Methods with `.Get()`

Do not use `.Get()` in factory methods to set reusable defaults. `.Get()` is the per-test interceptor API:

<!-- snippet: skill-anti-pattern-factory-get -->
```cs
// WRONG: Using .Get() in a factory method to set reusable defaults
private CurrentUserBareStub CreateCurrentUser(
    long userId = 0,
    string role = "User")
{
    var stub = new CurrentUserBareStub();
    stub.UserId.Get(userId);
    stub.Role.Get(role);
    stub.DisplayName.Get($"Test {role}");
    return stub;
}
```
<!-- endSnippet -->

Use constructor parameters with stub overrides instead (see Real-World Example above).

---

## Method Stub Overrides

### Return/Call Supersedes

`Return()` and `Call()` take priority over stub overrides per-test:

<!-- snippet: stub-overrides-ref-return-supersedes -->
```cs
// Default behavior from override
service.GetUserById(1); // "Default User"

// Supersede with Return for this test
stub.GetUserById.Call(id => new User { Id = id, Name = "Override" });
service.GetUserById(1); // "Override"
```
<!-- endSnippet -->

### When Chains

Stub override stubs support the full When chain API:

<!-- snippet: stub-overrides-ref-when -->
```cs
stub.GetUserById.When(42).Return(new User { Id = 42, Name = "SPECIAL" });

service.GetUserById(42); // "SPECIAL" (When matched)
service.GetUserById(1);  // "Default User" (stub override)
```
<!-- endSnippet -->

Priority: When > Sequences > Return/Call > **Stub Override**

---

## Mixed Stubs

Override some methods, configure others:

<!-- snippet: stub-overrides-ref-mixed -->
```cs
// Methods WITH override use it as default
service.WithOverride("test");    // "[User: test]"

// Methods WITHOUT override need configuration or return default
stub.WithoutOverride.Call((input) => $"[Configured: {input}]");
service.WithoutOverride("test"); // "[Configured: test]"
```
<!-- endSnippet -->

---

## Overloaded Methods

Each overload gets its own virtual method in the base class:

<!-- snippet: stub-overrides-overloads -->
```cs
[KnockOff]
public partial class StubOverrideFormatterStub : IFormatter
{
    // Override only the overloads you need
    protected override string Format_(string input) => input.ToUpperInvariant();

    // Override other overloads with custom logic
    protected override string Format_(string input, bool uppercase)
        => uppercase ? input.ToUpperInvariant() : input.ToLowerInvariant();
}
```
<!-- endSnippet -->

Override only some overloads -- unoverridden ones use the interceptor path (Return/default).

---

## Strict Mode

Stub overrides **bypass strict mode** -- they ARE the configuration:

<!-- snippet: stub-overrides-ref-strict -->
```cs
var stub = new StubOverridesRepoStub();
stub.Strict = true;

IStubOverridesRepo service = stub;

// Stub overrides bypass strict mode -- they ARE the configuration
var user = service.GetUserById(1); // Works -- override IS the config
Assert.Equal("Default User", user!.Name);
```
<!-- endSnippet -->

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

- **Generic methods** -- excluded from base class pattern. Use `Of<T>()` instead.
- **Inline stubs** -- entire class is generated, no partial for overrides.
- **Indexer overrides** -- see separate design (not yet supported).

---

## Reset Behavior

`Reset()` clears tracking but **preserves** Return/Call/Get/Set configuration:

<!-- snippet: stub-overrides-reset -->
```cs
// Reset clears tracking state but preserves Return configuration
stub.GetBalance.Reset();
stub.GetBalance.Verify(Called.Never);
```
<!-- endSnippet -->

---

## Quick Reference

| Task | Code |
|------|------|
| Method override | `protected override string Process_(string input) => result;` |
| Property override (get-only) | `protected override int Count_ => _count;` |
| Property override (get/set) | `protected override string Name_ { get => _name; set => _name = value; }` |
| Property override (set-only) | `protected override string Path_ { set => _path = value; }` |
| Supersede with Return | `stub.Process.Return(value)` |
| Supersede with Get | `stub.Count.Get(42)` |
| Supersede with callback | `stub.Process.Call((input) => result)` |
| When chain override | `stub.Process.When("x").Return("y")` |
| Strict mode bypass | Override IS the configuration |
| Reset | `stub.Process.Reset()` clears counts, preserves config |
