# Type Safety: KnockOff vs Moq vs NSubstitute

Moq and NSubstitute are *nearly* type-safe — lambda expressions and generic constraints catch most errors at compile time. But there are gaps where code compiles and fails at runtime. KnockOff eliminates these gaps.

---

## The Partial Setup Trap

The most common trap: you set up a method but forget `.Returns()`. With Moq and NSubstitute, this compiles without complaint. With KnockOff, the mistake is impossible — there's no two-step process where you can complete step 1 and forget step 2.

**Moq Strict mode — throws at runtime as if the method was never set up:**

<!-- snippet: partial-setup-moq-strict -->
```cs
// Moq Strict — Setup without .Returns() throws at runtime
var mock = new Mock<IPartialSetupCalc>(MockBehavior.Strict);

// You set the method up...
mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));
// ...but forgot .Returns()

// MockException at RUNTIME — Moq acts as if the method was never set up
IPartialSetupCalc calc = mock.Object;
Assert.Throws<MockException>(() => calc.Calculate(1, 2));
```
<!-- endSnippet -->

**Moq Loose mode — silently returns `default(T)`:**

<!-- snippet: partial-setup-moq-loose -->
```cs
// Moq Loose — Setup without .Returns() silently returns default
var mock = new Mock<IPartialSetupCalc>();

mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));
// No .Returns() — no error, no warning

IPartialSetupCalc calc = mock.Object;
var result = calc.Calculate(1, 2);
Assert.Equal(0, result); // silently returns 0 instead of a meaningful value
```
<!-- endSnippet -->

**NSubstitute — same silent default, no strict mode to catch it:**

<!-- snippet: partial-setup-nsub-silent -->
```cs
// NSubstitute — no strict mode, silently returns default
var calc = Substitute.For<IPartialSetupCalc>();

// No .Returns() configured — returns default(int) = 0
// No error, no warning — your test may pass for the wrong reason
var result = calc.Calculate(1, 2);
Assert.Equal(0, result);
```
<!-- endSnippet -->

**KnockOff — `Return` and `Call` are each a single, complete call. There is no second step to forget:**

<!-- snippet: partial-setup-knockoff-oncall -->
```cs
// KnockOff — Return IS the setup AND the return value
var stub = new PartialSetupCalcStub();

// One call does both: configures the method AND defines the return value
// There is no second step to forget
stub.Calculate.Return((a, b) => a + b);

IPartialSetupCalc calc = stub;
Assert.Equal(3, calc.Calculate(1, 2));
```
<!-- endSnippet -->

<!-- snippet: partial-setup-knockoff-returns -->
```cs
// KnockOff — Return is also a single complete call
var stub = new PartialSetupCalcStub();

stub.Calculate.Return(42);

IPartialSetupCalc calc = stub;
Assert.Equal(42, calc.Calculate(1, 2));
```
<!-- endSnippet -->

---

## The Gap: Callback Type Parameters (Moq)

Moq's `.Returns<T1, T2>()` and `.Callback<T1, T2>()` accept manually specified type parameters that are not checked against the method signature at compile time. If you get them wrong, the code compiles but throws at runtime.

**Wrong type parameters on `.Returns<T1, T2>()`:**

<!-- snippet: type-safety-moq-returns-mismatch -->
```cs
// Moq — .Returns<T1, T2> type parameters are NOT checked at compile time
var mock = new Mock<ITypeSafeCalc>();

// Add(int, int) but we specify <string, string> — COMPILES FINE
mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>()))
    .Returns<string, string>((a, b) => 0);

// ArgumentException at RUNTIME: "Object of type 'Int32' cannot be converted to type 'String'"
ITypeSafeCalc calc = mock.Object;
Assert.Throws<ArgumentException>(() => calc.Add(1, 2));
```
<!-- endSnippet -->

**Wrong type parameters on `.Callback<T1, T2>()`:**

<!-- snippet: type-safety-moq-callback-mismatch -->
```cs
// Moq — .Callback<T1, T2> type parameters are NOT checked at compile time
var mock = new Mock<ITypeSafeValidator>();
var setup = mock.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()));

// Validate(string, string) but we specify <int, string> — COMPILES FINE
// ArgumentException at RUNTIME: "Invalid callback. Setup on method with parameters
// (string, string) cannot invoke callback with parameters (int, string)."
Assert.Throws<ArgumentException>(() => setup.Callback<int, string>((a, b) => { }));
```
<!-- endSnippet -->

Both examples compile without errors. The mismatch between the method signature and the type parameters is only caught at runtime.

---

## The Gap: Untyped CallInfo (NSubstitute)

NSubstitute's `callInfo[index]` returns `object`, so any cast compiles — even wrong ones. And `callInfo.Arg<T>()` throws when multiple parameters share the same type.

**Wrong cast on `callInfo[0]`:**

<!-- snippet: type-safety-nsub-callinfo-cast -->
```cs
// NSubstitute — callInfo[0] returns object, cast is unchecked at compile time
var calc = Substitute.For<ITypeSafeCalc>();

// Add(int, int) but we cast callInfo[0] to string — COMPILES FINE
calc.Add(Arg.Any<int>(), Arg.Any<int>())
    .Returns(callInfo =>
    {
        var a = (string)callInfo[0]; // InvalidCastException at RUNTIME
        return 0;
    });

Assert.Throws<InvalidCastException>(() => calc.Add(1, 2));
```
<!-- endSnippet -->

**Ambiguous `Arg<T>()` with same-typed parameters:**

<!-- snippet: type-safety-nsub-arg-ambiguous -->
```cs
// NSubstitute — Arg<T>() is ambiguous when multiple parameters share the same type
var validator = Substitute.For<ITypeSafeValidator>();

// Validate(string, string) — both params are string
validator.Validate(Arg.Any<string>(), Arg.Any<string>())
    .Returns(callInfo =>
    {
        // callInfo.Arg<string>() throws because there are TWO string params
        var name = callInfo.Arg<string>(); // AmbiguousArgumentsException at RUNTIME
        return true;
    });

Assert.Throws<AmbiguousArgumentsException>(() => validator.Validate("Jane", "Doe"));
```
<!-- endSnippet -->

NSubstitute does provide `ArgAt<T>(index)` to disambiguate, but the index is unchecked — using the wrong index is another runtime error. The fundamental issue is that parameter access goes through an untyped intermediary.

---

## KnockOff: Fully Typed

KnockOff's generated interceptors match the method signature exactly. Return/Call and When lambdas receive typed, named parameters -- no manual type specifications, no casts, no index lookups.

**Returns with typed parameters:**

<!-- snippet: type-safety-knockoff-oncall-typed -->
```cs
// KnockOff — Return parameters are generated from the method signature
var stub = new TypeSafeCalcStub();

// (int a, int b) — types and names come from Add(int a, int b)
// Wrong types here cause a COMPILE error, not a runtime error
stub.Add.Return((a, b) => a + b);

ITypeSafeCalc calc = stub;
Assert.Equal(3, calc.Add(1, 2));
```
<!-- endSnippet -->

**When with typed parameters:**

<!-- snippet: type-safety-knockoff-when-typed -->
```cs
// KnockOff — When predicate parameters are generated from the method signature
var stub = new TypeSafeValidatorStub();

// (string firstName, string lastName) — both parameters are named and typed
// No ambiguity: each parameter is a separate lambda argument
stub.Validate.When((firstName, lastName) => firstName.Length > 0).Return(true);

ITypeSafeValidator validator = stub;
Assert.True(validator.Validate("Jane", "Doe"));
```
<!-- endSnippet -->

If you try to use the wrong types in a KnockOff lambda, you get a compile error — not a runtime exception.

---

## Summary

| | Moq | NSubstitute | KnockOff |
|---|---|---|---|
| **Partial setup (forgot `.Returns()`)** | Strict: runtime error. Loose: silent `default(T)` | Silent `default(T)` | Impossible -- `Return`/`Call` are each complete in one call |
| **Lambda setup** | Typed (compile-time safe) | Typed (compile-time safe) | Typed (compile-time safe) |
| **Callback/Returns type params** | Manual `<T1, T2>` — unchecked | N/A | Generated — compile-time safe |
| **Argument access in callbacks** | Via `.Returns<T1,T2>((a,b) => ...)` — manual types | Via `callInfo[i]` (untyped) or `.Arg<T>()` (ambiguous) | Via lambda params `(a, b) => ...` — typed and named |
| **Same-typed parameter disambiguation** | Manual type params (error-prone) | `ArgAt<T>(index)` — index unchecked | Separate lambda params — no disambiguation needed |

Back to [README](../README.md).
