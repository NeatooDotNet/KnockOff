# Compile-Time Safety

KnockOff stubs are real C# classes. When interfaces change, the compiler catches problems immediately — not your CI pipeline, not production.

## The Problem with Runtime Mocking

Moq uses runtime reflection to create mock implementations. This means interface changes don't cause compile errors:

<!-- snippet: compile-time-moq-runtime-problem -->
```cs
public static void MoqRuntimeProblem()
{
    var mock = new Mock<ICtsUserService>();
    mock.Setup(x => x.GetUser(1)).Returns(new CtsUser());

    // If interface adds a new method, this compiles but may fail at runtime
    // if Moq strict mode is enabled
}
```
<!-- endSnippet -->

The problem: **Your build is green, but tests explode when you run them.**

## What KnockOff Catches

### 1. New Interface Members

When you add a method to an interface:

<!-- pseudo:compile-time-new-member -->
```csharp
public interface IUserService
{
    User GetUser(int id);
    Task<User> GetUserAsync(int id);  // NEW
}

[KnockOff]
public partial class UserServiceStub : IUserService { }
```
<!-- /snippet -->

**Compiler error immediately:**

<!-- pseudo:compile-time-new-member-error -->
```
CS0535: 'UserServiceStub' does not implement interface member 'IUserService.GetUserAsync(int)'
```
<!-- /snippet -->

The generator adds the implementation automatically on next build, but you're alerted that your stub's API has changed.

### 2. Renamed Methods

When you rename an interface method:

<!-- pseudo:compile-time-renamed-method -->
```csharp
public interface IUserService
{
    User FindUser(int id);  // Renamed from GetUser
}

[KnockOff]
public partial class UserServiceStub : IUserService
{
    // User method no longer matches
    protected User GetUser(int id) => new User { Id = id };
}
```
<!-- /snippet -->

**Your user method silently stops being called.** The test starts returning `default` instead of your custom implementation.

KnockOff catches this through test failure, not at compile time — but importantly, Moq would also fail silently here, with `Setup` targeting a method that no longer exists (which Moq allows without error).

### 3. Changed Parameter Types

When a parameter type changes:

<!-- pseudo:compile-time-changed-param -->
```csharp
public interface IOrderService
{
    void ProcessOrder(Guid orderId);  // Changed from int
}

[KnockOff]
public partial class OrderServiceStub : IOrderService
{
    // User method signature mismatch
    protected void ProcessOrder(int orderId) { }
}
```
<!-- /snippet -->

**The user method no longer matches.** You'll see different behavior and likely a failing test.

### 4. Callback Signature Mismatches

When you set a callback with the wrong signature:

<!-- invalid:compile-time-wrong-callback -->
```csharp
stub.GetUser.OnCall = (ko, name) => new User();  // Wrong: expects int, not string
```
<!-- /snippet -->

**Compiler error:**

<!-- pseudo:compile-time-callback-error -->
```
CS1661: Cannot convert lambda expression to type 'Func<UserServiceStub, int, User>'
because the parameter types do not match the delegate parameter types
```
<!-- /snippet -->

Moq catches this too (at compile time for `Returns`, but often at runtime for `Callback`).

### 5. Return Type Changes

When a return type changes:

<!-- invalid:compile-time-wrong-return -->
```csharp
public interface IUserService
{
    UserDto GetUser(int id);  // Changed from User
}

// Your test code:
stub.GetUser.OnCall = (ko, id) => new User { Id = id };  // Wrong return type
```
<!-- /snippet -->

**Compiler error:**

<!-- pseudo:compile-time-return-error -->
```
CS0029: Cannot implicitly convert type 'User' to 'UserDto'
```
<!-- /snippet -->

## IDE Benefits

Because KnockOff generates real C# code, you get full IDE support:

### Rename Refactoring

Rename `GetUser` to `FindUser` in the interface → IDE updates:
- All `stub.GetUser` references become `stub.FindUser`
- All `OnCall` assignments update automatically

With Moq, you'd manually update every `Setup(x => x.GetUser(...))`.

### Find All References

"Find all usages of `IUserService.GetUser`" includes:
- Interface definition
- Production code
- Test stub usages (`stub.GetUser.OnCall`)
- User methods in stub classes

### IntelliSense

Autocomplete shows:
- Available interceptors (`stub.` → `GetUser`, `Save`, etc.)
- Interceptor properties (`stub.GetUser.` → `OnCall`, `CallCount`, `LastCallArg`)
- Correct callback signatures

## Practical Example

Before your change:

<!-- snippet: compile-time-practical-before -->
```cs
public static void PracticalExampleBefore()
{
    var stub = new CtsEmailServiceKnockOff();
    ICtsEmailService service = stub;

    stub.SendEmail.OnCall = (ko, to, subject, body) => { };

    service.SendEmail("user@example.com", "Subject", "Body");

    Assert.Equal("user@example.com", stub.SendEmail.LastCallArgs?.to);
}
```
<!-- endSnippet -->

After adding a parameter:

<!-- pseudo:compile-time-practical-after -->
```csharp
public interface IEmailService
{
    void SendEmail(string to, string subject, string body, bool isHtml);  // NEW PARAM
}
```
<!-- /snippet -->

**Compiler errors:**

<!-- pseudo:compile-time-practical-errors -->
```
CS1593: Delegate 'Action<EmailServiceStub, string, string, string, bool>'
does not take 4 arguments

CS1061: '(string to, string subject, string body)?' does not contain
a definition for 'to' (tuple structure changed)
```
<!-- /snippet -->

The compiler guides you to every place that needs updating.

## Summary

| Scenario | Moq | KnockOff |
|----------|-----|----------|
| New interface member | Runtime error | Compile error |
| Renamed method | Silent Setup ignore | Test failure (user method) |
| Changed parameter type | Runtime error | Compile error |
| Changed return type | Runtime error | Compile error |
| Wrong callback signature | Sometimes compile, sometimes runtime | Compile error |
| Rename refactoring | Manual | Automatic |
| Find all references | Partial | Complete |

## Next

- [Readability](readability.md) — Less ceremony than Moq
- [The Duality Pattern](duality-pattern.md) — Two ways to customize behavior
