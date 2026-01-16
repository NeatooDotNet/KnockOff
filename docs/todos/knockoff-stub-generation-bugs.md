# KnockOff Stub Generation Bugs

Two bugs discovered when generating stubs for Neatoo factory interfaces.

## Bug 1: Method Overloads with Different Return Types

**Severity:** Build-breaking

**Affected interfaces:** Any interface with method overloads where one returns `T` and another returns `Task<T>` (common in Neatoo factories)

**Example interface:**
```csharp
public interface ISymptomsAreaFactory
{
    // Async - takes id
    Task<ISymptomsArea?> Fetch(long id);

    // Sync - takes EF entity
    ISymptomsArea Fetch(PnSymptomsArea entity);
}
```

**Problem:** KnockOff generates ONE interceptor for both `Fetch` overloads, typed for the async version:

```csharp
public Func<Stubs.ISymptomsAreaFactory, long?, PnSymptomsArea?, Task<ISymptomsArea?>>? OnCall { get; set; }
```

When the sync method implementation calls `onCall()`, it tries to return `Task<ISymptomsArea?>` as `ISymptomsArea`:

```csharp
// Line 6034-6037 in generated code
ISymptomsArea ISymptomsAreaFactory.Fetch(PnSymptomsArea entity)
{
    Fetch.RecordCall(null, entity);
    if (Fetch.OnCall is { } onCall) return onCall(this, null, entity);  // ERROR: Task<T> returned as T
    ...
}
```

**Compiler error:**
```
error CS0266: Cannot implicitly convert type 'Task<ISymptomsArea?>' to 'ISymptomsArea'
```

**Expected behavior:** KnockOff should either:
1. Generate separate interceptors for overloads with different return types (e.g., `Fetch_Async` and `Fetch_Sync`)
2. Or detect the return type mismatch and generate appropriate conversion/separate callbacks

---

## Bug 2: Generic Interface Inheritance Type Mismatch

**Severity:** Build-breaking

**Affected interfaces:** Any interface that inherits from a generic interface which also has a non-generic base (e.g., `IRule<T> : IRule`)

**Example interface:**
```csharp
public interface IConsultationHistoryRule : IRule<IConsultationHistory> { }

// Where IRule<T> has:
public interface IRule<T> : IRule where T : IValidateBase
{
    Task<IRuleMessages> RunRule(T target, CancellationToken? token);
}

// And IRule (non-generic) has:
public interface IRule
{
    Task<IRuleMessages> RunRule(IValidateBase target, CancellationToken? token);
}
```

**Problem:** KnockOff generates one interceptor typed for the most specific type (`IConsultationHistory`):

```csharp
public Func<..., IConsultationHistory, ...>? OnCall { get; set; }
```

But when implementing the non-generic `IRule.RunRule` method, it passes `IValidateBase`:

```csharp
// Line 4467-4470 in generated code
Task<IRuleMessages> IRule.RunRule(IValidateBase target, CancellationToken? token)
{
    RunRule.RecordCall(target, token);  // ERROR: IValidateBase passed to method expecting IConsultationHistory
    if (RunRule.OnCall is { } onCall) return onCall(this, target, token);  // ERROR: same issue
    ...
}
```

**Compiler errors:**
```
error CS1503: Argument 1: cannot convert from 'IValidateBase' to 'IConsultationHistory'
error CS1503: Argument 2: cannot convert from 'IValidateBase' to 'IConsultationHistory'
```

**Expected behavior:** KnockOff should either:
1. Generate separate interceptors for each interface level in the hierarchy
2. Or use the least specific type (`IValidateBase`) for the shared interceptor and cast as needed
3. Or detect the inheritance pattern and handle it appropriately

---

## Reproduction

Add these KnockOff attributes to a test class:

```csharp
[KnockOff<ISymptomsAreaFactory>]  // Bug 1
[KnockOff<ISignsAreaFactory>]     // Bug 1
[KnockOff<IConsultationHistoryRule>]  // Bug 2
public partial class MyTests { }
```

Build fails with the errors described above.

## Workaround

Currently none - these interfaces cannot be stubbed with KnockOff until the bugs are fixed.
