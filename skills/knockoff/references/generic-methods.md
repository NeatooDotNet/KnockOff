# Generic Methods Reference

Generic methods (methods with their own type parameters like `T Method<T>()`) use the `Of<T>()` pattern to configure and verify behavior per type argument.

---

## Of<T>() Pattern

Generic methods don't use `Return()` directly. You must first access a typed handler via `Of<T>()`:

```csharp
// WRONG: Cannot Return directly on a generic method handler
// stub.Convert.Return(42);

// CORRECT: Access typed handler first
stub.Convert.Of<int>().Return((value) => 42);
stub.Convert.Of<string>().Return((value) => "converted");
```

---

## Configuration

### Return(callback) — Per-Type Behavior

```csharp
stub.Convert.Of<int>().Return((value) => 42);
stub.Convert.Of<string>().Return((value) => value.ToString()!);

var intResult = service.Convert<int>("anything");   // 42
var strResult = service.Convert<string>("anything"); // "anything"
```

### Return(value) — Constant Per-Type

```csharp
stub.Create.Of<List<int>>().Return(() => new List<int> { 1, 2, 3 });
```

### Call(callback) — Void Generic Methods

```csharp
stub.Register.Of<string>().Call(() => called = true);
service.Register<string>(); // called == true
```

---

## Multiple Type Parameters

Methods with multiple type parameters use `Of<T1, T2>()`:

```csharp
stub.Transform.Of<string, List<int>>().Return((input) => new List<int> { input.Length });

var result = service.Transform<string, List<int>>("hello");
// result == [5]
```

---

## Verification

### Per-Type Verification

```csharp
service.Convert<int>("a");
service.Convert<string>("b");

stub.Convert.Of<int>().Verify(Called.Once);
stub.Convert.Of<string>().Verify(Called.Once);
```

### Aggregate Verification (All Types)

```csharp
stub.Convert.Verify(Called.Exactly(2)); // Total calls across ALL type arguments
```

---

## CalledTypeArguments

Track which type arguments were used at runtime:

```csharp
service.Convert<int>("a");
service.Convert<string>("b");

var calledTypes = stub.Convert.CalledTypeArguments;
// calledTypes contains typeof(int) and typeof(string)
// calledTypes.Count == 2
```

---

## Mixed Overloads (Generic + Non-Generic Same Name)

When a class has both a non-generic and generic method with the same name, they get **separate interceptors**:

```csharp
// Given: void Process(string label) + void Process<T>(T item, string label)

// Non-generic: stub.Process
stub.Process.Call((label) => { });

// Generic: stub.ProcessGeneric.Of<T>()
stub.ProcessGeneric.Of<int>().Call((item, label) => { });

service.Process("non-generic");     // Uses stub.Process
service.Process(42, "generic");      // Uses stub.ProcessGeneric
```

The generic overload gets a `Generic` suffix on the interceptor name.

---

## Reset

`Reset()` clears **all** typed handlers, call counts, and CalledTypeArguments:

```csharp
service.Convert<int>("a");
service.Convert<string>("b");

stub.Convert.Reset();

stub.Convert.Verify(Called.Never);
Assert.Empty(stub.Convert.CalledTypeArguments);
```

---

## Unconfigured Behavior

| Context | Behavior |
|---------|----------|
| Interface stub | Returns `default(T)` |
| Class stub (virtual) | Falls back to `base.Method<T>(args)` |
| Class stub (abstract) | Returns `default(T)` |
| Strict mode | Throws `StubException` |

---

## Stub Overrides NOT Supported

Generic methods are **excluded** from the stub override pattern (underscore-suffix base class). Use `Of<T>()` for all generic method configuration:

```csharp
// NO stub override for generic methods:
// protected override T Convert_<T>(object value) => ...  // NOT generated

// Use Of<T>() instead:
stub.Convert.Of<int>().Return((value) => 42);
```

---

## Quick Reference

| Task | Code |
|------|------|
| Configure return | `stub.Method.Of<T>().Return((args) => result)` |
| Configure void | `stub.Method.Of<T>().Call(() => { })` |
| Verify per-type | `stub.Method.Of<T>().Verify(Called.Once)` |
| Verify all types | `stub.Method.Verify(Called.Exactly(n))` |
| Track types used | `stub.Method.CalledTypeArguments` |
| Multi-type params | `stub.Method.Of<T1, T2>().Return(...)` |
| Reset all types | `stub.Method.Reset()` |
