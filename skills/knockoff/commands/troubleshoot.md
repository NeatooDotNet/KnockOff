---
description: Diagnose and fix common KnockOff issues
argument-hint: [file-path or issue description]
allowed-tools: Read, Edit, Glob, Grep, Bash, AskUserQuestion
---

[← Back to Commands](../README.md) | [KnockOff Usage](../skills/knockoff-usage/SKILL.md)

# Troubleshoot

Diagnose and fix KnockOff issues. This command helps identify and resolve common problems with stub generation, compilation errors, verification issues, and runtime behavior.

## Troubleshooting Workflow

Follow these steps systematically:

1. **Gather Information** - Understand the specific issue and context
2. **Check Common Issues** - Match symptoms to known problems
3. **Examine Generated Code** - Review actual generator output
4. **Check Build Output** - Look for diagnostics and errors
5. **Provide Solution** - Explain and apply fixes

---

## Step 1: Gather Information

**If $ARGUMENTS contains a file path:**
- Read that file using Read tool
- Look for KnockOff attributes and common error patterns

**If $ARGUMENTS describes an issue:**
- Note the specific symptoms
- Check for error codes (CS#### or KO###)

**Otherwise, use AskUserQuestion to ask:**
- "What issue are you experiencing?" with options:
  1. Build errors / compilation fails
  2. Stub not generating
  3. Method/property not found on stub
  4. Verification failing
  5. Performance issues (slow build, hangs)
  6. IntelliSense not showing generated members
  7. Other (describe)

**Gather context:**
- Which pattern is being used? (see [patterns.md](../skills/knockoff-usage/references/patterns.md))
- Error message if available
- File path if relevant

## Step 2: Check for Common Issues

### Issue: Missing `partial` Keyword

**Symptoms:**
- CS0102: Type already contains definition
- Duplicate member errors
- Generator output conflicts with manual code

**Diagnosis:**
Search for KnockOff attributes without partial using Grep: `"\[KnockOff"` in `*.cs` files. Check if matching classes are marked `partial`.

**Fix:**
Add `partial` keyword to class declaration:

<!-- snippet: troubleshoot-missing-partial-before -->
```cs
// ERROR: Without `partial`, you get CS0102 duplicate member errors
// public class MyStub : IUserService { }
```
<!-- endSnippet -->

<!-- snippet: troubleshoot-missing-partial-after -->
```cs
// CORRECT: Add `partial` keyword to class declaration
[KnockOff]
public partial class CorrectUserServiceStub : IUserService { }
```
<!-- endSnippet -->

### Issue: Wrong OnCall Signature

**Symptoms:**
- CS1593: Delegate does not take X arguments
- Cannot convert lambda expression
- OnCall callback doesn't compile

**Diagnosis:**
Compare the OnCall callback parameters with the interface method signature.

**Fix:**
Match callback parameters to method signature:

<!-- snippet: troubleshoot-oncall-signature-wrong -->
```cs
// Interface method: User GetUser(int id, bool includeDeleted)

// ERROR: Wrong - no parameters (CS1593)
// stub.GetUser.OnCall(() => new User());

// ERROR: Wrong - only one parameter (CS1593)
// stub.GetUser.OnCall((id) => new User());
```
<!-- endSnippet -->

<!-- snippet: troubleshoot-oncall-signature-correct -->
```cs
// CORRECT: Match all parameters from method signature
stub.GetUser.OnCall((int id, bool includeDeleted) =>
    new User { Id = id, Name = includeDeleted ? "All" : "Active" });
```
<!-- endSnippet -->

### Issue: Stub Not Generating

**Symptoms:**
- Stubs namespace doesn't exist
- Type 'Stubs.IInterface' could not be found
- No interceptor properties on stub

**Diagnosis:**
1. Check attribute is present and spelled correctly
2. Verify class is partial
3. Check for analyzer errors in build output
4. Look for Generated/ folder contents

**Fix:**
1. Ensure attribute is correct (see [patterns.md](../skills/knockoff-usage/references/patterns.md) for all nine patterns)
2. Clean and rebuild solution
3. Check Error List for analyzer diagnostics

### Issue: Interface Not Found for Inline Pattern

**Symptoms:**
- CS0246: Type or namespace 'IInterface' could not be found
- Generator error about missing type

**Diagnosis:**
Check if the interface is accessible from the test project.

**Fix:**
1. Add project reference to project containing interface
2. Add using statement for interface namespace
3. Ensure interface is public or accessible

### Issue: .Object Missing for Class Stubs

**Symptoms:**
- Cannot implicitly convert type 'Stubs.MyClass' to 'MyClass'
- Type mismatch when passing stub to constructor

**Diagnosis:**
Check if using Inline Class pattern without `.Object`.

**Fix:**
For class stubs (not interface stubs), use `.Object`:

<!-- snippet: troubleshoot-class-stub-wrong -->
```cs
// When stubbing a CLASS (not interface), assignment fails:
// var stub = new Stubs.EmailService();
// EmailService service = stub;  // ERROR: Cannot convert Stubs.EmailService to EmailService
```
<!-- endSnippet -->

<!-- snippet: troubleshoot-class-stub-correct -->
```cs
[Fact]
public void ClassStub_UseObjectProperty()
{
    var stub = new Stubs.EmailService();
    stub.Send.OnCall((to, subject) => true);

    // Use .Object to get the typed instance
    EmailService service = stub.Object;

    var result = service.Send("test@example.com", "Hello");
    Assert.True(result);
}
```
<!-- endSnippet -->

### Issue: Delegate Stub - Cannot Use Func/Action

**Symptoms:**
- Compiler error when using `[KnockOff<Func<int, int>>]`
- "The type 'Func<>' cannot be used as a type argument"

**Diagnosis:**
KnockOff only supports named delegate types, not built-in `Func<>` or `Action<>`.

**Fix:**
Define a named delegate type:

<!-- snippet: skill-mistake-func-action -->
```cs
// WRONG: KnockOff doesn't support generic delegates
// [KnockOff<Func<int, string>>]  // Won't work

// RIGHT: Define a named delegate
public delegate string SkillNamedOperation(int value);
[KnockOff<SkillNamedOperation>]
public partial class SkillNamedDelegateHost { }
```
<!-- endSnippet -->

### Issue: Delegate Stub - Using Wrong Access Pattern

**Symptoms:**
- "Stubs.MyDelegate does not contain a definition for 'MethodName'"
- Trying to use `stub.MethodName` on a delegate stub

**Diagnosis:**
Delegate stubs use `stub.Interceptor` instead of named member properties.

**Fix:**
Use `stub.Interceptor` for all delegate configuration:

<!-- snippet: delegate-api-access-pattern -->
```cs
var stub = new Stubs.ArithmeticOperation();

// All configuration goes through stub.Interceptor
stub.Interceptor.Returns(42);
stub.Interceptor.OnCall((a, b) => a + b);

// Implicit conversion to delegate type
ArithmeticOperation op = stub;
var result = op(2, 3);
```
<!-- endSnippet -->

### Issue: Async Method Returns Wrong Type

**Symptoms:**
- Cannot convert Task<T> to T
- OnCall expects different return type

**Diagnosis:**
Check if returning raw value instead of Task, or if you could use `Returns()` which auto-wraps.

**Fix:**
Use `Returns()` for simple values (auto-wraps), or explicit `Task.FromResult` for `OnCall`:

<!-- snippet: troubleshoot-async-return-wrong -->
```cs
// Interface: Task<User?> GetUserAsync(int id)

// ERROR: Returning unwrapped value (CS0029)
// stub.GetUserAsync.OnCall((id) => new User());
```
<!-- endSnippet -->

<!-- snippet: troubleshoot-async-return-correct -->
```cs
// CORRECT: Return Task.FromResult for async methods
stub.GetUserAsync.OnCall((int id) =>
    Task.FromResult<User?>(new User { Id = id }));

// For Task (void async), use Task.CompletedTask:
stub.SaveAsync.OnCall((user) => Task.CompletedTask);
```
<!-- endSnippet -->

**Simpler alternatives using auto-wrapping:**

<!-- snippet: troubleshoot-async-simpler-alternatives -->
```cs
// Returns() auto-wraps in Task.FromResult
stub.GetUserAsync.Returns(new User { Id = 1, Name = "Alice" });

// Simplified OnCall also auto-wraps
stub.GetUserAsync.OnCall((id) => new User { Id = id });
```
<!-- endSnippet -->

### Issue: Verification Fails Unexpectedly

**Symptoms:**
- Verify throws "Expected X calls but received Y"
- Verifiable members report not called

**Diagnosis:**
1. Check if OnCall was set up BEFORE the action
2. Verify the stub is the same instance used in test
3. Check if calling through interface (not stub directly)

**Fix:**
1. Set up OnCall before acting:

<!-- snippet: troubleshoot-verification-setup-order -->
```cs
[Fact]
public void Verification_SetupBeforeAct()
{
    var stub = new UserServiceStub();

    // ARRANGE: Configure OnCall with Verifiable BEFORE acting
    stub.GetUserAsync.OnCall((id) =>
        Task.FromResult<User?>(new User { Id = id }))
        .Verifiable();

    IUserService service = stub;

    // ACT: Call the method
    service.GetUserAsync(42).Wait();

    // ASSERT: Verify the call was made
    stub.Verify();
}
```
<!-- endSnippet -->

2. Ensure using same instance:

<!-- snippet: troubleshoot-verification-same-instance -->
```cs
[Fact]
public void Verification_SameInstanceThroughout()
{
    // Create stub once
    var stub = new UserServiceStub();

    // Configure the stub
    stub.GetUserAsync.OnCall((id) =>
        Task.FromResult<User?>(new User { Id = id }))
        .Verifiable();

    // Pass same stub to service constructor
    var service = new NotificationService(stub);

    // Act via the service (which uses the stub)
    service.NotifyUser(1).Wait();

    // Verify on the original stub instance
    stub.Verify();
}
```
<!-- endSnippet -->

### Issue: Generic Method Type Arguments

**Symptoms:**
- OnCall doesn't work for generic methods
- Type inference fails

**Diagnosis:**
Generic methods may need explicit type handling.

**Fix:**
Check the generated code in Generated/ folder to see the exact interceptor API for generic methods. Generic methods may have specialized overloads.

### Issue: Multiple Interfaces with Same Method Names

**Symptoms:**
- Unclear which interface's method is being tracked
- Need to verify calls separately per interface

**Diagnosis:**
Check if stub implements multiple interfaces with identical method signatures.

**Fix:**
When multiple interfaces share the same method signature, KnockOff generates a single shared interceptor. All calls route through this interceptor regardless of which interface you call through:

<!-- snippet: troubleshoot-multiple-interfaces -->
```cs
[Fact]
public void MultipleInterfaces_SharedInterceptor()
{
    var stub = new BothStub();

    // When multiple interfaces share the same method signature,
    // KnockOff generates a single shared interceptor
    stub.DoWork.OnCall(() => { }).Verifiable();

    // Calls through either interface use the same interceptor
    IFoo foo = stub;
    foo.DoWork();

    IBar bar = stub;
    bar.DoWork();

    // Verify tracks calls from both interfaces combined
    stub.DoWork.Verify(Times.Exactly(2));
}
```
<!-- endSnippet -->

### Issue: Properties Not Generating Set Interceptor

**Symptoms:**
- No Set interceptor for property
- Cannot verify property writes

**Diagnosis:**
Check if property is read-only (no setter in interface).

**Fix:**
Read-only properties only generate Get interceptors. To verify writes, the interface property must have a setter:

<!-- snippet: troubleshoot-property-readonly -->
```cs
[Fact]
public void Property_ReadOnlyVsReadWrite()
{
    // Read-only property (get only in interface): Only OnGet available
    var readOnlyStub = new ReadOnlyConfigStub();
    readOnlyStub.Version.OnGet("1.0.0");

    IReadOnlyConfig readOnlyConfig = readOnlyStub;
    Assert.Equal("1.0.0", readOnlyConfig.Version);

    // Read-write property ({ get; set; } in interface): OnGet AND OnSet available
    var readWriteStub = new ReadWriteConfigStub();
    readWriteStub.Version.OnGet("2.0.0");

    IReadWriteConfig readWriteConfig = readWriteStub;

    // Read the property (triggers get)
    var version = readWriteConfig.Version;
    Assert.Equal("2.0.0", version);

    // Write the property (triggers set)
    readWriteConfig.Version = "3.0.0";

    // Can verify both get and set
    readWriteStub.Version.VerifyGet(Times.Once);
    readWriteStub.Version.VerifySet(Times.Once);
}
```
<!-- endSnippet -->

### Issue: OutOfMemoryException or Build Hangs

**Symptoms:**
- Build never completes
- OutOfMemoryException during compilation
- Visual Studio freezes

**Diagnosis:**
Check for circular references or very large interface hierarchies.

**Fix:**
1. Check if interface inherits from many base interfaces
2. Look for recursive generic type parameters
3. Simplify interface hierarchy if possible
4. Report issue if encountered with specific scenario

### Issue: InternalsVisibleTo Not Working

**Symptoms:**
- Cannot access internal interfaces from test project
- Inline pattern fails with internal types

**Diagnosis:**
Check if InternalsVisibleTo is configured correctly.

**Fix:**
Add to source project's .csproj or AssemblyInfo.cs:

<!-- snippet: troubleshoot-internals-visible-to -->
```cs
// In your SOURCE project (not test project), add to AssemblyInfo.cs or .csproj:
//
// AssemblyInfo.cs:
// [assembly: InternalsVisibleTo("YourTestProject")]
//
// Or in .csproj:
// <ItemGroup>
//   <InternalsVisibleToSuffix Include="YourTestProject" />
// </ItemGroup>
//
// Then internal interfaces can be stubbed in your test project:
// internal interface IInternalService { }
//
// [KnockOff]
// public partial class InternalServiceStub : IInternalService { }
```
<!-- endSnippet -->

### Issue: IntelliSense Not Showing Generated Members

**Symptoms:**
- Red squiggles under interceptor properties
- IDE shows error but build succeeds
- Generated members don't appear in autocomplete

**Diagnosis:**
Source generator output not being picked up by IDE.

**Fix:**
1. **Rebuild the project**: Use `dotnet build` or IDE rebuild
2. **Restart OmniSharp** (VS Code): Command Palette → "OmniSharp: Restart OmniSharp"
3. **Restart Visual Studio**: Close and reopen solution
4. **Check generated files exist**: Look in Generated/ folder
5. **Verify generator runs**: Check build output for source generator messages

**Note:** This is a known limitation of source generators in some IDEs. The code will compile correctly even if IntelliSense shows errors.

## Step 3: Check Generated Code

If issue persists, examine the generated code:

1. Use Glob to find Generated/ folder in test project: `**/Generated/**/*.g.cs`
2. Use Grep to search for files matching the stub name or interface name
3. Read the generated file to review actual generated API
4. Compare expected interceptors vs actual generated interceptors

**What to look for in generated code:**
- Interface member names → Interceptor property names
- Method signatures → OnCall delegate signatures
- Return types → Expected callback return types
- Generic type parameters → Specialized overloads

The generated code is the source of truth for the actual API surface.

## Step 4: Check Build Output

Run build and check for:
- Analyzer warnings (KO001, KO002, etc.)
- Generator errors
- Missing references

Use Bash to run:
```bash
dotnet build --verbosity normal
```

Look for lines containing "KnockOff" in output.

## Step 5: Provide Solution

After diagnosis:
1. Explain the root cause
2. Show the specific fix
3. Apply the fix using Edit tool if possible
4. Suggest rebuilding to verify

## Quick Diagnostic Checklist

Run through these checks systematically:

**Basic Setup:**
- [ ] Class marked `partial`? (Standalone patterns only)
- [ ] Attribute spelled correctly? (`[KnockOff]` or `[KnockOff<T>]`)
- [ ] Interface/class accessible from test project?
- [ ] Using statements present?
- [ ] Project references correct?

**Build Issues:**
- [ ] Clean build performed recently? (`dotnet clean && dotnet build`)
- [ ] Check Error List for KO### diagnostics
- [ ] Generated/ folder contains expected files?

**Runtime Issues:**
- [ ] OnCall signature matches method signature (all parameters)?
- [ ] Async methods returning Task/Task<T>?
- [ ] Class stubs using .Object property?
- [ ] Verification setup before action?
- [ ] Same stub instance used throughout test?

**Advanced Issues:**
- [ ] Generic methods - checked generated code for exact API?
- [ ] Multiple interfaces - shared interceptor for same method name?
- [ ] Properties - does interface define setter?
- [ ] InternalsVisibleTo configured for internal types?

## Common Error Code Reference

**CS0102** - Type already contains definition → Missing `partial` keyword
**CS1593** - Delegate parameter mismatch → Wrong OnCall signature
**CS0246** - Type not found → Missing reference or using statement
**CS0029** - Cannot convert type → Wrong pattern (interface vs class stub)

**KO001** - Interface not found → Check type accessibility
**KO002** - Multiple candidates → Disambiguate type reference
**KO003** - Unsupported member type → Check diagnostics for details
**KO0200** - Standalone stub cannot have user-defined base class → Remove base class or use Inline pattern

---

**UPDATED:** 2026-02-05
