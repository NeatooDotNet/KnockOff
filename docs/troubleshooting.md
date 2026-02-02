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
// Use .Object to get the typed instance for class stubs
EmailService service = stub.Object;
```
<!-- endSnippet -->

---

### Error: No overload matches delegate

**Cause:** The OnCall callback signature doesn't match the method's parameters.

OnCall callbacks receive only the method's parameters. The callback must match the parameter types exactly.

**Solution:** Ensure your callback signature matches the method parameters exactly.

<!-- snippet: troubleshoot-oncall-signature -->
```cs
// OnCall signature must match method parameters exactly
stub.GetByIdAsync.OnCall((int id) =>
    Task.FromResult<User?>(new User { Id = id, Name = "Test" }));
```
<!-- endSnippet -->

---

### Using Returns with Static Values

**When to use:** You want to return the same value for every call without writing a callback function.

KnockOff provides `Returns(value)` for methods and `OnGet(value)` for properties, allowing you to configure a static return value directly.

**Solution:** Use `Returns(value)` instead of a callback when the return value is constant.

<!-- snippet: troubleshoot-oncall-value -->
```cs
// Use Returns(value) when the return doesn't depend on parameters
stub.GetById.Returns(new User { Id = 999, Name = "Static User" });
```
<!-- endSnippet -->

**Available on:**
- **Methods**: `stub.MethodName.Returns(value)` - Returns the same value for every call
- **Properties**: `stub.PropertyName.OnGet(value)` - Returns the same value for every get
- **Sequences**: `stub.MethodName.OnCall(callback).ThenCall(callback)` - Each callback in sequence

**Key difference from callbacks:**
- **Returns(value)**: Simple, concise for constant returns
- **OnCall(callback)**: Dynamic behavior based on parameters or state

---

## Runtime Errors

### InvalidOperationException: No callback configured

**Cause:** A method with a non-nullable reference return type (like `string`, non-nullable `User`) was invoked without configuration.

KnockOff throws this exception for methods returning non-nullable reference types when no callback is configured. Properties and nullable types use default values instead.

**Solution:** Configure the return value using `OnCall` for methods or `OnGet` for properties.

<!-- snippet: troubleshoot-no-callback -->
```cs
// Configure required (non-nullable) return values explicitly
stub.GetName.OnCall(() => "Configured Name");
```
<!-- endSnippet -->

---

## Unexpected Behavior

### OnGet not being called

**Cause:** OnGet was overridden or reconfigured after initial setup.

Each call to `OnGet` replaces the previous configuration. The most recent OnGet call determines the property's behavior.

**Solution:** Ensure you don't accidentally override OnGet configuration. Check that subsequent OnGet calls are intentional.

<!-- snippet: troubleshoot-onget-priority -->
```cs
// Most recent OnGet configuration wins
stub.Host.OnGet("from-value");
```
<!-- endSnippet -->

---

### Reset() doesn't clear OnGet configuration

**Cause:** By design, Reset() clears tracking counters but preserves `OnGet` and `OnSet` configuration.

Reset() is intended to clear test verification state between test phases, not to reconfigure stub behavior.

**Solution:** If you need to clear configured values, manually call OnGet with a default value or reconfigure the stub.

<!-- snippet: troubleshoot-reset-value -->
```cs
// Reset() clears tracking but preserves OnGet configuration
stub.Host.Reset();
stub.Host.VerifyGet(Times.Never);  // Tracking cleared
Assert.Equal("configured-host", config.Host);  // Config preserved
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

<!-- snippet: troubleshoot-build-commands -->
```cs
// Rebuild to trigger source generator:
// dotnet build

// If issues persist, clean first:
// dotnet clean
// dotnet build
```
<!-- endSnippet -->

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

Review the build output messages for specific guidance on resolving each diagnostic.

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

---

**UPDATED:** 2026-01-25
