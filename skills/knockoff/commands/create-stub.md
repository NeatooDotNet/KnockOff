---
description: Create a new KnockOff stub class
argument-hint: [interface-or-class-name]
allowed-tools: Read, Write, Glob, Grep, AskUserQuestion
---

Create a KnockOff stub for testing. Follow this workflow:

## Step 1: Determine Target Type

If $ARGUMENTS is provided, use it as the target type name.
Otherwise, use AskUserQuestion to ask:
- "What interface or class do you want to stub?" with text input

## Step 2: Find the Target Type

Search the codebase for the interface or class definition:
- Use Glob to find files that might contain the type
- Use Grep to locate the exact definition
- Read the file to understand the type's members

If the type cannot be found, inform the user and ask for the file path.

## Step 3: Choose Pattern

Use AskUserQuestion to ask which pattern:

**Options:**
1. **Stand-Alone** (Recommended for reusable stubs)
   - Creates a dedicated stub class file
   - Can be shared across test files
   - Supports custom user methods

2. **Inline Interface** (For test-local stubs)
   - Adds `[KnockOff<IInterface>]` to existing test class
   - Stub scoped to that test class
   - No extra files needed

3. **Inline Class** (For stubbing classes)
   - Use when target is a class with virtual members
   - Adds `[KnockOff<ClassName>]` to test class
   - Access via `stub.Object`

## Step 4: Determine File Location

For Stand-Alone pattern:
- Ask where to create the stub file
- Suggest: same directory as tests, or a `Stubs/` folder
- Default filename: `{TypeName}Stub.cs`

For Inline patterns:
- Ask which test class to add the attribute to
- Find existing test classes or offer to create new one

## Step 5: Generate the Stub

### Stand-Alone Pattern Template:

```csharp
using KnockOff;

namespace {Namespace};

[KnockOff]
public partial class {TypeName}Stub : {InterfaceName}
{
    // KnockOff generates all interface implementations
    // Add optional user methods below for default behavior
}
```

### Inline Interface Pattern:

Add attribute to existing test class:
```csharp
[KnockOff<{InterfaceName}>]
public partial class {TestClassName}
```

Ensure the class is marked `partial`.

### Inline Class Pattern:

Add attribute to existing test class:
```csharp
[KnockOff<{ClassName}>]
public partial class {TestClassName}
```

Ensure the class is marked `partial`.

## Step 6: Show Usage Example

After creating the stub, show a usage example:

**Stand-Alone:**
```csharp
var stub = new {TypeName}Stub();
stub.{MethodName}.OnCall(({params}) => {returnValue});
{InterfaceName} instance = stub;
```

**Inline Interface:**
```csharp
var stub = new Stubs.{InterfaceName}();
stub.{MethodName}.OnCall(({params}) => {returnValue});
{InterfaceName} instance = stub;
```

**Inline Class:**
```csharp
var stub = new Stubs.{ClassName}();
stub.{MethodName}.OnCall(({params}) => {returnValue});
{ClassName} instance = stub.Object;
```

## Important Notes

- Always mark stub classes as `partial`
- For inline patterns, the test class must also be `partial`
- Include necessary `using` statements
- Generated code appears in `Generated/` folder after build
