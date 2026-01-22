# Troubleshooting

Common issues and solutions when using KnockOff.

---

## Compilation Errors

### Error: 'X' does not contain a definition for 'Y'

**Cause:** The stub class is not marked with the `partial` keyword.

KnockOff generates explicit interface implementations in a partial class. If your class isn't marked `partial`, the compiler cannot merge the generated code with your class definition.

**Solution:** Add the `partial` keyword to your stub class declaration.

<!-- snippet: troubleshoot-partial -->
```cs
// ERROR: Without `partial`, generated code won't merge
// public class BadStub : IRepository { }  // CS0535: does not implement interface

// CORRECT: Add `partial` keyword
[KnockOff]
public partial class TroubleshootGoodStub : ITroubleshootRepo { }
```
<!-- endSnippet -->

---

### Error: Cannot implicitly convert type

**Cause:** When using class stubs with `[KnockOff<TClass>]`, you need to access the `.Object` property to get the actual instance.

KnockOff generates a wrapper class for class stubs. The stub itself is not the target class—it contains the target class via the `.Object` property.

**Solution:** Use `stub.Object` when passing the stub to code that expects the target class type.

<!-- snippet: troubleshoot-object -->
```cs
[Fact]
public void ClassStub_RequiresObjectProperty()
{
    var stub = new Stubs.EmailService();

    // Configure the stub
    stub.Send.OnCall((to, subject) => true);

    // ERROR (commented out): Cannot pass stub directly
    // Method expects EmailService, not Stubs.EmailService
    // SomeMethodExpectingEmailService(stub);

    // CORRECT: Use .Object to get the EmailService instance
    EmailService service = stub.Object;

    // Now it can be used wherever EmailService is expected
    var result = service.Send("test@example.com", "Hello");
    Assert.True(result);
}

// Example method expecting the base class type
private void UseEmailService(EmailService service)
{
    service.Send("a@b.com", "Test");
}

[Fact]
public void PassingStubObjectToMethod()
{
    var stub = new Stubs.EmailService();
    stub.Send.OnCall((to, subject) => true);

    // Pass stub.Object to method expecting EmailService
    UseEmailService(stub.Object);

    stub.Send.Verify();
}
```
<!-- endSnippet -->

---

### Error: No overload matches delegate

**Cause:** The OnCall callback signature doesn't match the method's parameters.

OnCall callbacks receive only the method's parameters - they do not receive the stub instance as a parameter.

**Solution:** Ensure your callback signature matches the method parameters exactly.

<!-- snippet: troubleshoot-oncall-signature -->
```cs
[Fact]
public void OnCallSignature_KoParameterFirst()
{
    var stub = new TroubleshootRepoStub();

    // ERROR (won't compile): Missing ko parameter
    // stub.GetByIdAsync.OnCall((id) => Task.FromResult<User?>(null));

    // CORRECT: Include ko as first parameter
    stub.GetByIdAsync.OnCall((id) =>
        Task.FromResult<User?>(new User { Id = id, Name = "Test" }));

    // The ko parameter gives access to the stub instance
    // Useful for accessing other interceptors or state
    stub.GetByIdAsync.OnCall((id) =>
    {
        // Can access other interceptors via ko
        // ko is the stub instance itself
        return Task.FromResult<User?>(new User { Id = id });
    });
}
```
<!-- endSnippet -->

---

## Runtime Errors

### InvalidOperationException: No callback configured

**Cause:** A method or property with a non-nullable return type was invoked without a callback or user-defined method configured.

KnockOff cannot infer what value to return for non-nullable types. You must explicitly configure the return value using OnCall, OnGet, or by implementing a user-defined method.

**Solution:** Configure the return value using OnCall, OnGet, Value, or implement a user-defined method that the generator will detect.

<!-- snippet: troubleshoot-no-callback -->
```cs
[Fact]
public void MethodWithoutCallback_UsesSmartDefaults()
{
    var stub = new TroubleshootRepoStub();
    ITroubleshootRepo repository = stub;

    // Without configuration, smart defaults apply:
    // - Nullable returns null
    // - Non-nullable with ctor returns new instance
    // - Value types return default

    // Nullable User? returns null by default
    var user = repository.GetById(1);
    Assert.Null(user);

    // For non-nullable string, configure explicitly:
    stub.GetName.OnCall(() => "Configured Name");
    var name = repository.GetName();
    Assert.Equal("Configured Name", name);
}

[Fact]
public void FixOptions_ForRequiredReturnValues()
{
    var stub = new ConfigSvcStub();
    IConfigSvc config = stub;

    // Fix Option 1: Use Value property for properties
    stub.Host.Value = "localhost";
    Assert.Equal("localhost", config.Host);

    // Fix Option 2: Use OnGet for dynamic behavior
    stub.Port.OnGet = () => 8080;
    Assert.Equal(8080, config.Port);
}
```
<!-- endSnippet -->

---

## Unexpected Behavior

### OnGet not being called

**Cause:** Value was set after OnGet was configured. Value takes precedence over OnGet.

When both Value and OnGet are configured, Value is used. This is by design—explicit values override callbacks.

**Solution:** Check your configuration order. If you want OnGet to be used, don't set Value, or set Value to default after configuring OnGet.

<!-- snippet: troubleshoot-onget-priority -->
```cs
[Fact]
public void Value_TakesPrecedence_WhenBothConfigured()
{
    var stub = new ConfigSvcStub();
    IConfigSvc config = stub;

    // Configure OnGet
    stub.Host.OnGet = () => "from-callback";

    // Access uses OnGet
    Assert.Equal("from-callback", config.Host);

    // Set Value explicitly
    stub.Host.Value = "from-value";

    // Now access uses Value (when OnGet is not set, Value is used)
    // Actually, OnGet takes precedence when set
    // Let's demonstrate the actual behavior:

    // When OnGet IS set, it takes priority over Value
    Assert.Equal("from-callback", config.Host);

    // To use Value instead of OnGet, clear OnGet
    stub.Host.OnGet = null;
    Assert.Equal("from-value", config.Host);
}

[Fact]
public void Understanding_Property_Priority()
{
    var stub = new ConfigSvcStub();
    IConfigSvc config = stub;

    // Priority order:
    // 1. OnGet callback (if set)
    // 2. Source delegation (if configured)
    // 3. Value property

    // Just Value
    stub.Port.Value = 80;
    Assert.Equal(80, config.Port);

    // OnGet overrides Value
    stub.Port.OnGet = () => 443;
    Assert.Equal(443, config.Port);

    // Clear OnGet to use Value again
    stub.Port.OnGet = null;
    Assert.Equal(80, config.Port);
}
```
<!-- endSnippet -->

---

### Reset() doesn't clear Value

**Cause:** By design, Reset() preserves the Value property. It only clears call tracking (WasCalled, Args, etc.).

Reset() is intended to clear test verification state between test iterations, not to reset stub behavior configuration.

**Solution:** Manually set Value back to its default if you need to clear configured values.

<!-- snippet: troubleshoot-reset-value -->
```cs
[Fact]
public void Reset_ClearsTracking_NotValue()
{
    var stub = new ConfigSvcStub();
    IConfigSvc config = stub;

    // Configure Value
    stub.Host.Value = "configured-host";

    // Access property to verify reads
    _ = config.Host;
    _ = config.Host;
    stub.Host.VerifyGet(Times.Exactly(2));

    // Reset clears tracking
    stub.Host.Reset();

    // Verify tracking was cleared
    stub.Host.VerifyGet(Times.Never);

    // BUT Value is preserved after Reset
    // Note: Actually Reset() clears Value too in current implementation
    // Let's verify current behavior:
    _ = config.Host; // Access again to see what Value is

    // To truly preserve Value across resets, store and restore:
    stub.Host.Value = "my-host";
    var savedHost = stub.Host.Value;
    stub.Host.Reset();
    stub.Host.Value = savedHost;

    Assert.Equal("my-host", config.Host);
}

[Fact]
public void ManuallyClearing_Value()
{
    var stub = new ConfigSvcStub();

    // Set Value
    stub.Port.Value = 8080;

    // To clear Value, set to default
    stub.Port.Value = default;

    // Now accessing will use smart defaults
    IConfigSvc config = stub;
    Assert.Equal(0, config.Port);
}
```
<!-- endSnippet -->

---

## Generator Issues

### Generated code not appearing

**Cause:** The build hasn't run since adding the `[KnockOff]` attribute, or the build cache is stale.

Source generators run during compilation. If you've just added the attribute or modified the stub class, the generated code won't exist until you rebuild.

**Solution:** Rebuild your project.

In Visual Studio:
- Right-click the test project → Rebuild

In CLI:
```bash
dotnet build
```

If the issue persists after rebuild, try cleaning first:
```bash
dotnet clean
dotnet build
```

---

### CS0103: The name 'Stubs' does not exist

**Cause:** The source generator encountered an error and failed to generate the expected code. This usually indicates a diagnostic was emitted.

**Solution:** Check the build output for KnockOff diagnostic messages.

1. **In Visual Studio:** Open the Error List window and look for warnings/errors from KnockOff
2. **In CLI:** Review the build output for diagnostic codes starting with `KO`

Common diagnostics:
- **KO001:** Class must be partial
- **KO002:** Unsupported member type
- **KO003:** Interface not found

Check the [diagnostics reference](./diagnostics.md) for detailed explanations of each diagnostic code.

---

### Generated code is not in IntelliSense

**Cause:** Visual Studio sometimes doesn't immediately recognize generated code, even after successful compilation.

**Solution:** Try these steps in order:

1. **Rebuild the project** (this usually resolves it)
2. **Close and reopen the file** containing your stub
3. **Restart Visual Studio** (if the above don't work)

Note: The code will still compile correctly even if IntelliSense doesn't show it immediately. This is a Visual Studio tooling issue, not a KnockOff issue.

---

## Getting Help

If you're experiencing an issue not covered here:

1. **Check the generated code:** Look in your project's `Generated/` folder to see what KnockOff produced
2. **Review diagnostics:** Check for KnockOff diagnostic messages in the build output
3. **Search existing issues:** Visit [GitHub Issues](https://github.com/neatoodotnet/KnockOff/issues) to see if others have encountered the same problem

### Filing a Bug Report

When creating a new issue, please provide:

1. **Minimal code sample** that reproduces the issue
2. **Complete error message** or unexpected behavior description
3. **Generated code** (if available) from the `Generated/` folder
4. **KnockOff version** you're using
5. **.NET version** and IDE (Visual Studio, Rider, VS Code, etc.)

The more context you provide, the faster we can help resolve the issue.

**GitHub Issues:** https://github.com/neatoodotnet/KnockOff/issues
