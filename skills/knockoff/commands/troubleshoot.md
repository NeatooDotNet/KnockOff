---
description: Diagnose and fix common KnockOff issues
argument-hint: [file-path or issue description]
allowed-tools: Read, Edit, Glob, Grep, Bash, AskUserQuestion
---

Diagnose and fix KnockOff issues. Follow this workflow:

## Step 1: Gather Information

If $ARGUMENTS contains a file path, read that file.
If $ARGUMENTS describes an issue, note it.
Otherwise, use AskUserQuestion to ask:
- "What issue are you experiencing?" with options:
  1. Build errors / compilation fails
  2. Stub not generating
  3. Method/property not found on stub
  4. Verification failing
  5. Other (describe)

## Step 2: Check for Common Issues

### Issue: Missing `partial` Keyword

**Symptoms:**
- CS0102: Type already contains definition
- Duplicate member errors
- Generator output conflicts with manual code

**Diagnosis:**
Search for KnockOff attributes without partial:
```
Grep: "\[KnockOff" in *.cs files
```
Check if matching classes are marked `partial`.

**Fix:**
Add `partial` keyword to class declaration:
```csharp
// Before
[KnockOff]
public class MyStub : IInterface { }

// After
[KnockOff]
public partial class MyStub : IInterface { }
```

### Issue: Wrong OnCall Signature

**Symptoms:**
- CS1593: Delegate does not take X arguments
- Cannot convert lambda expression
- OnCall callback doesn't compile

**Diagnosis:**
Compare the OnCall callback parameters with the interface method signature.

**Fix:**
Match callback parameters to method signature:
```csharp
// Interface: User GetUser(int id, bool includeDeleted)

// Wrong
stub.GetUser.OnCall(() => user);
stub.GetUser.OnCall((id) => user);

// Correct
stub.GetUser.OnCall((id, includeDeleted) => user);
```

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
1. Ensure attribute is correct:
   - Stand-Alone: `[KnockOff]` on class implementing interface
   - Inline: `[KnockOff<IInterface>]` on test class
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
```csharp
// Wrong
MyClass instance = new Stubs.MyClass();

// Correct
var stub = new Stubs.MyClass();
MyClass instance = stub.Object;
```

### Issue: Async Method Returns Wrong Type

**Symptoms:**
- Cannot convert Task<T> to T
- OnCall expects different return type

**Diagnosis:**
Check if returning raw value instead of Task.

**Fix:**
Return Task-wrapped values for async methods:
```csharp
// Wrong
stub.GetUserAsync.OnCall((id) => user);

// Correct
stub.GetUserAsync.OnCall((id) => Task.FromResult(user));

// For void async
stub.SaveAsync.OnCall((data) => Task.CompletedTask);
```

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
```csharp
// Setup FIRST
stub.GetUser.OnCall((id) => user).Verifiable();

// Then act
repo.GetUser(1);

// Then verify
stub.Verify();
```

2. Ensure using same instance:
```csharp
var stub = new UserRepoStub();
var service = new UserService(stub);  // Pass stub
service.DoWork();
stub.Verify();  // Verify same stub
```

### Issue: Generic Method Type Arguments

**Symptoms:**
- OnCall doesn't work for generic methods
- Type inference fails

**Diagnosis:**
Generic methods may need explicit type handling.

**Fix:**
Check the generated code in Generated/ folder to see the exact interceptor API for generic methods. Generic methods may have specialized overloads.

## Step 3: Check Generated Code

If issue persists, examine the generated code:

1. Find Generated/ folder in test project
2. Look for files matching the stub name
3. Review actual generated API

The generated code shows exactly what interceptors and methods are available.

## Step 4: Check Build Output

Run build and check for:
- Analyzer warnings (KO001, KO002, etc.)
- Generator errors
- Missing references

```
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

Run through these checks:
- [ ] Class marked `partial`?
- [ ] Attribute spelled correctly? (`[KnockOff]` or `[KnockOff<T>]`)
- [ ] Interface/class accessible from test project?
- [ ] Using statements present?
- [ ] Project references correct?
- [ ] Clean build performed recently?
- [ ] OnCall signature matches method signature?
- [ ] Async methods returning Task?
- [ ] Class stubs using .Object?
