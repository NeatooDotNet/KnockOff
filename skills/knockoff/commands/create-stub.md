---
description: Create a new KnockOff stub class
argument-hint: [interface-or-class-name]
allowed-tools: Read, Write, Glob, Grep, AskUserQuestion
---

[← Back to Commands](../README.md) | [KnockOff Usage](../skills/knockoff-usage/SKILL.md)

# Create KnockOff Stub

Create a KnockOff stub for testing. This command guides you through creating a stub using one of three patterns:

1. **Stand-Alone** - Dedicated stub class file (recommended for shared stubs)
2. **Inline Interface** - Attribute on test class for interface stubs
3. **Inline Class** - Attribute on test class for class stubs

Follow this workflow:

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
   - Creates a dedicated stub class file (e.g., `UserRepositoryStub.cs`)
   - Can be shared across multiple test files
   - Supports custom user methods for common test setup
   - Best when multiple tests need the same stub

2. **Inline Interface** (For test-local stubs)
   - Adds `[KnockOff<IInterface>]` to existing test class
   - Stub scoped to that test class only via nested `Stubs` class
   - No extra files needed
   - Best for quick, test-specific stubs
   - Access stub via `new Stubs.IInterfaceName()` within the test class

3. **Inline Class** (For stubbing classes)
   - Use when target is a class with virtual members
   - Adds `[KnockOff<ClassName>]` to test class
   - Generated stub nested within test class via `Stubs` class
   - Access via `stub.Object` property to get class instance
   - Best for testing code that depends on classes, not interfaces

**When to use each:**
- **Stand-Alone**: Multiple test files need the same stub, or you want custom setup methods
- **Inline Interface**: Single test class needs a simple interface stub
- **Inline Class**: Testing code that requires a class instance (not an interface)

## Step 4: Determine File Location

For Stand-Alone pattern:
- Use AskUserQuestion to ask where to create the stub file
- **Suggested locations:**
  - Same directory as test files
  - A dedicated `Stubs/` folder in the test project
  - `TestHelpers/` or `Fixtures/` folder
- **Default filename:** `{TypeName}Stub.cs`
  - Example: `IUserRepository` → `UserRepositoryStub.cs`
  - Remove the `I` prefix from interfaces
  - Append `Stub` suffix
- Ensure the namespace matches the test project conventions

For Inline patterns:
- Use AskUserQuestion to ask which test class to add the attribute to
- Search for existing test classes that might use this stub
- If no suitable test class exists, offer to create a new test class
- Ensure the test class is marked `partial` (or add it)

## Step 5: Generate the Stub

### Stand-Alone Pattern Template:

<!-- snippet: command-create-stub-standalone-pattern -->
```cs
[KnockOff]
public partial class DataServiceStub : IDataService { }
```
<!-- endSnippet -->

### Inline Interface Pattern:

Add attribute to existing test class:

<!-- snippet: command-create-stub-inline-interface-pattern -->
```cs
[KnockOff<INotificationService>]
public partial class CmdInlineInterfaceTests { }
```
<!-- endSnippet -->

Ensure the class is marked `partial`.

### Inline Class Pattern:

Add attribute to existing test class:

<!-- snippet: command-create-stub-inline-class-pattern -->
```cs
[KnockOff<PaymentProcessor>]
public partial class CmdInlineClassTests { }
```
<!-- endSnippet -->

Ensure the class is marked `partial`.

## Step 6: Show Usage Example

After creating the stub, show a usage example:

**Stand-Alone:**

<!-- snippet: command-create-stub-standalone-usage -->
```cs
[Fact]
public void StandAloneStub_Usage()
{
    // Instantiate the stub
    var stub = new DataServiceStub();

    // Configure return value
    stub.GetById.OnCall((id) => new User { Id = id, Name = "Test" });

    // Use through interface
    IDataService service = stub;
    var user = service.GetById(42);

    Assert.Equal("Test", user!.Name);
}
```
<!-- endSnippet -->

**Inline Interface:**

<!-- snippet: command-create-stub-inline-interface-usage -->
```cs
[Fact]
public void InlineInterfaceStub_Usage()
{
    // Instantiate via Stubs namespace
    var stub = new Stubs.INotificationService();

    // Configure behavior
    stub.Notify.OnCall((msg) => { });
    stub.IsEnabled.OnGet(true);

    // Use through interface
    INotificationService service = stub;
    service.Notify("Hello");

    Assert.True(service.IsEnabled);
}
```
<!-- endSnippet -->

**Inline Class:**

<!-- snippet: command-create-stub-inline-class-usage -->
```cs
[Fact]
public void InlineClassStub_Usage()
{
    // Instantiate via Stubs namespace
    var stub = new Stubs.PaymentProcessor();

    // Configure virtual member
    stub.ProcessPayment.OnCall((amount) => amount > 0);

    // Access class instance via .Object
    PaymentProcessor processor = stub.Object;
    var result = processor.ProcessPayment(100m);

    Assert.True(result);
}
```
<!-- endSnippet -->

## Advanced: Custom User Methods

For stand-alone stubs, you can add custom methods to provide default behavior or common test setup:

<!-- snippet: command-create-stub-user-methods -->
```cs
public partial class UserRepoCmdStub
{
    // User method provides default test data
    protected IEnumerable<User> FindAll()
    {
        return new[]
        {
            new User { Id = 1, Name = "Alice" },
            new User { Id = 2, Name = "Bob" }
        };
    }
}
```
<!-- endSnippet -->

When you define a user method that matches an interface member, KnockOff generates a tracking interceptor for it. The interceptor name includes a numeric suffix to avoid conflicts (e.g., `FindAll2` for the `FindAll` method). Use this interceptor to verify the user method was called.

## Step 7: Verify and Build

After creating the stub:
1. Inform the user of the file location and what was created
2. Suggest building the project to generate the stub implementation
3. Remind the user that generated code appears in `Generated/` folder

## Important Notes

- Always mark stub classes as `partial`
- For inline patterns, the test class must also be `partial`
- Include necessary `using` statements (especially `KnockOff` namespace)
- Generated code appears in `Generated/` folder after build
- Stand-alone stubs are reusable across test files
- Inline stubs are scoped to their containing test class

## Troubleshooting

If the stub doesn't generate:
- Verify the class is marked `partial`
- Check that the interface/class is accessible (public or internal with InternalsVisibleTo)
- Build the project to trigger source generation
- Check for compiler errors in the IDE

## Quick Reference

**Stand-Alone Pattern:**
- File: `{TypeName}Stub.cs`
- Attribute: `[KnockOff]`
- Implements: The interface directly
- Usage: `var stub = new UserRepositoryStub();`

**Inline Interface Pattern:**
- Attribute: `[KnockOff<IUserRepository>]`
- On: Test class (must be `partial`)
- Usage: `var stub = new Stubs.IUserRepository();`

**Inline Class Pattern:**
- Attribute: `[KnockOff<UserService>]`
- On: Test class (must be `partial`)
- Usage: `var stub = new Stubs.UserService(); var instance = stub.Object;`

## Sample Code Location

All code samples for this command are in:
`src/Tests/KnockOff.Documentation.Samples/CreateStubCommandSamples.cs`

Samples demonstrate:
- Stand-alone pattern template (file structure)
- Inline interface pattern (attribute usage)
- Inline class pattern (attribute usage)
- Usage examples for each pattern
- Custom user methods for stand-alone stubs

---

**UPDATED:** 2026-01-25
