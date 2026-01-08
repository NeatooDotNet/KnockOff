# Delegate Stubs

KnockOff can generate stubs for named delegate types using `KnockOffAttribute<TDelegate>`. This enables testing code that accepts delegates for validation rules, factories, callbacks, and similar patterns.

## Basic Usage

Define delegates and stub them with `[KnockOff<TDelegate>]`:

<!-- snippet: docs:delegates:basic-delegate -->
```csharp
// Named delegate types can be stubbed with [KnockOff<TDelegate>]
public delegate bool IsUniqueRule(string value);

public delegate DelUser UserFactory(int id);

public delegate void LogAction(string message);
```
<!-- /snippet -->

<!-- snippet: docs:delegates:basic-usage -->
```csharp
[KnockOff<IsUniqueRule>]
[KnockOff<UserFactory>]
public partial class DelegateTests
{
	// The [KnockOff<TDelegate>] attribute generates:
	// - Stubs.IsUniqueRule with Interceptor property
	// - Stubs.UserFactory with Interceptor property
	// Each has implicit conversion to the delegate type
}

// Usage in test:
// var uniqueStub = new DelegateTests.Stubs.IsUniqueRule();
// uniqueStub.Interceptor.OnCall = (ko, value) => value != "duplicate";
//
// // Implicit conversion to delegate
// IsUniqueRule rule = uniqueStub;
// Assert.True(rule("unique"));
// Assert.False(rule("duplicate"));
//
// // Verify calls
// Assert.Equal(2, uniqueStub.Interceptor.CallCount);
// Assert.Equal("duplicate", uniqueStub.Interceptor.LastCallArg);
```
<!-- /snippet -->

## Void Delegates

Delegates that return void track calls and arguments:

<!-- snippet: docs:delegates:void-delegate -->
```csharp
[KnockOff<LogAction>]
public partial class VoidDelegateTests
{
}

// Usage:
// var logStub = new VoidDelegateTests.Stubs.LogAction();
// var messages = new List<string>();
// logStub.Interceptor.OnCall = (ko, msg) => messages.Add(msg);
//
// LogAction logger = logStub;
// logger("Hello");
// logger("World");
//
// Assert.Equal(2, logStub.Interceptor.CallCount);
// Assert.Equal(["Hello", "World"], messages);
```
<!-- /snippet -->

## Implicit Conversion

Stub classes convert implicitly to their delegate type:

<!-- snippet: docs:delegates:implicit-conversion -->
```csharp
// Stub classes have implicit conversion operators to the delegate type.
// This enables seamless passing to methods that expect the delegate:

// public class Validator
// {
//     private readonly IsUniqueRule _rule;
//     public Validator(IsUniqueRule rule) => _rule = rule;
//     public bool Validate(string value) => _rule(value);
// }

// var stub = new DelegateTests.Stubs.IsUniqueRule();
// stub.Interceptor.OnCall = (ko, v) => v.Length > 3;
//
// // Implicit conversion - no cast needed
// var validator = new Validator(stub);
// Assert.True(validator.Validate("test"));
```
<!-- /snippet -->

## Interceptor API

<!-- snippet: docs:delegates:interceptor-api -->
```csharp
// Delegate stub interceptor properties:
// stub.Interceptor.CallCount      // int - number of invocations
// stub.Interceptor.WasCalled      // bool - invoked at least once
// stub.Interceptor.LastCallArg    // T? - last argument (single param delegates)
// stub.Interceptor.LastCallArgs   // (T1, T2, ...)? - tuple (multi-param delegates)
// stub.Interceptor.OnCall         // Func/Action - callback for custom behavior
// stub.Interceptor.Reset()        // Clear all tracking and callback
```
<!-- /snippet -->

| Delegate Type | Interceptor Properties |
|--------------|-------------------|
| `void D()` | `CallCount`, `WasCalled`, `OnCall`, `Reset()` |
| `void D(T arg)` | Above + `LastCallArg` |
| `void D(T1 a, T2 b)` | Above (use `LastCallArgs` tuple) |
| `R D()` | `CallCount`, `WasCalled`, `OnCall` (returns R), `Reset()` |
| `R D(T arg)` | Above + `LastCallArg` |
| `R D(T1 a, T2 b)` | Above (use `LastCallArgs` tuple) |

## Closed Generic Delegates

Generic delegates must be closed with concrete type arguments:

<!-- snippet: docs:delegates:closed-generics -->
```csharp
[KnockOff<Factory<DelUser>>]
[KnockOff<Converter<int, string>>]
public partial class GenericDelegateTests
{
	// Closed generic delegates generate stubs with simple names:
	// - Stubs.Factory (for Factory<DelUser>)
	// - Stubs.Converter (for Converter<int, string>)
}

// Usage:
// var factoryStub = new GenericDelegateTests.Stubs.Factory();
// factoryStub.Interceptor.OnCall = (ko) => new DelUser { Id = 1, Name = "Created" };
//
// Factory<DelUser> factory = factoryStub;
// var user = factory();
// Assert.Equal("Created", user.Name);
//
// var converterStub = new GenericDelegateTests.Stubs.Converter();
// converterStub.Interceptor.OnCall = (ko, num) => $"Number: {num}";
//
// Converter<int, string> converter = converterStub;
// Assert.Equal("Number: 42", converter(42));
```
<!-- /snippet -->

## Common Patterns

### Validation Rules

<!-- snippet: docs:delegates:validation-pattern -->
```csharp
[KnockOff<IsUniqueRule>]
public partial class ValidationPatternTests
{
}

// Use delegates for validation rules in domain entities:
// var uniqueCheck = new ValidationPatternTests.Stubs.IsUniqueRule();
//
// // Default: always valid
// uniqueCheck.Interceptor.OnCall = (ko, value) => true;
//
// var entity = new Entity(uniqueCheck);
// entity.Name = "anything";  // Passes validation
//
// // Test invalid scenario
// uniqueCheck.Interceptor.Reset();
// uniqueCheck.Interceptor.OnCall = (ko, value) => false;
//
// entity.Name = "duplicate";  // Triggers validation error
// Assert.True(entity.HasErrors);
```
<!-- /snippet -->

### Factory Callbacks

```csharp
var factoryStub = new Tests.Stubs.UserFactory();
factoryStub.Interceptor.OnCall = (ko, id) => new User
{
    Id = id,
    Name = $"User{id}",
    CreatedAt = DateTime.UtcNow
};

// Inject into system under test
var service = new UserService(factoryStub);
var user = service.CreateUser(42);

Assert.Equal(42, user.Id);
Assert.True(factoryStub.Interceptor.WasCalled);
```

### Capturing Multiple Calls

```csharp
var logStub = new Tests.Stubs.LogAction();
var capturedMessages = new List<string>();

logStub.Interceptor.OnCall = (ko, msg) => capturedMessages.Add(msg);

// Run code that logs multiple times
service.Process();

Assert.Equal(["Starting", "Processing", "Complete"], capturedMessages);
Assert.Equal(3, logStub.Interceptor.CallCount);
```

## Limitations

### Open Generic Delegates

Open generic delegates cannot be stubbed:

```csharp
// NOT supported - open generic
[KnockOff<Func<T>>]  // Error: T is not resolved

// Supported - closed generic
[KnockOff<Func<int>>]  // OK: concrete type
```

### Ref/Out Parameters

Delegates with `ref` or `out` parameters cannot be stubbed because the generated callback signature uses `Func<>` or `Action<>` which don't support by-reference parameters:

```csharp
// NOT supported
public delegate bool TryParse(string input, out int result);

// Workaround: Use a wrapper interface
public interface ITryParser
{
    bool TryParse(string input, out int result);
}
```

### Anonymous Delegates

Only named delegate types can be stubbed. For `Func<>` or `Action<>`, define a named delegate:

```csharp
// Instead of: Func<int, string>
public delegate string IntToStringConverter(int value);

[KnockOff<IntToStringConverter>]
public partial class Tests { }
```
