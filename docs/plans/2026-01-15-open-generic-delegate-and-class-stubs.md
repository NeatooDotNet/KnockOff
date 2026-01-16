# Open Generic Delegate and Class Stubs Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add support for `[KnockOff(typeof(Factory<>))]` and `[KnockOff(typeof(Repository<>))]` syntax to generate open generic inline stubs for delegates and classes.

**Architecture:** The infrastructure for open generics already exists from v10.18 (interface support). Both DelegateInfo and ClassStubInfo already have `IsOpenGeneric` and `TypeParameters` fields. The main gap is in the transform phase: we need to use `OriginalDefinition` when extracting members from unbound generic types, mirroring what's already done for interfaces.

**Tech Stack:** Roslyn Source Generator, C# 13, xUnit

---

## Task 1: Add Failing Tests for Open Generic Delegates

**Files:**
- Modify: `src/Tests/KnockOffTests/OpenGenericInlineStubTests.cs`

**Step 1.1: Add test delegate and test class**

Add after the existing interface tests (~line 50):

```csharp
// ============================================================================
// Open Generic Delegate Tests
// ============================================================================

public delegate T OGFactory<T>();
public delegate TResult OGConverter<TIn, TOut, TResult>(TIn input) where TResult : class;

[KnockOff(typeof(OGFactory<>))]
[KnockOff(typeof(OGConverter<,,>))]
public partial class OpenGenericDelegateTests
{
    [Fact]
    public void SingleTypeParam_CanInstantiateWithDifferentTypes()
    {
        var stringFactory = new Stubs.OGFactory<string>();
        var intFactory = new Stubs.OGFactory<int>();

        Assert.NotNull(stringFactory);
        Assert.NotNull(intFactory);
    }

    [Fact]
    public void SingleTypeParam_InterceptorTracksInvocations()
    {
        var stub = new Stubs.OGFactory<string>();
        stub.Interceptor.OnCall = (ko) => "test-value";

        OGFactory<string> factory = stub;
        var result = factory();

        Assert.Equal("test-value", result);
        Assert.Equal(1, stub.Interceptor.CallCount);
    }

    [Fact]
    public void MultipleTypeParams_PreservesConstraints()
    {
        // TResult has 'class' constraint - string satisfies it
        var stub = new Stubs.OGConverter<int, bool, string>();
        stub.Interceptor.OnCall = (ko, input) => input.ToString();

        OGConverter<int, bool, string> converter = stub;
        var result = converter(42);

        Assert.Equal("42", result);
    }
}
```

**Step 1.2: Run tests to verify they fail (build error expected)**

Run: `dotnet build src/Tests/KnockOffTests/KnockOffTests.csproj`

Expected: Build error - `Stubs.OGFactory<T>` does not exist (generator doesn't produce it yet)

**Step 1.3: Commit failing tests**

```bash
git add src/Tests/KnockOffTests/OpenGenericInlineStubTests.cs
git commit -m "test: add failing tests for open generic delegate stubs"
```

---

## Task 2: Fix Transform Phase for Open Generic Delegates

**Files:**
- Modify: `src/Generator/KnockOffGenerator.Transform.cs`

**Step 2.1: Locate delegate extraction in TransformInlineStubClass**

Find the delegate handling section (~line 115-130). Currently it extracts DelegateInfo but doesn't pass open generic info.

**Step 2.2: Update delegate extraction to handle open generics**

Find this code pattern:
```csharp
if (typeArg.TypeKind == TypeKind.Delegate)
{
    var delInfo = DelegateInfo.Extract(typeArg);
```

Replace with:
```csharp
if (typeArg.TypeKind == TypeKind.Delegate)
{
    // For open generic delegates, use OriginalDefinition to get members
    var delegateSource = isOpenGeneric ? typeArg.OriginalDefinition : typeArg;
    var delInfo = DelegateInfo.Extract(
        delegateSource,
        isOpenGeneric,
        openGenericTypeParams);
```

**Step 2.3: Run build to check for compilation errors**

Run: `dotnet build src/Generator/KnockOff.Generator.csproj`

Expected: PASS (no compilation errors)

**Step 2.4: Commit**

```bash
git add src/Generator/KnockOffGenerator.Transform.cs
git commit -m "fix: pass open generic info to DelegateInfo.Extract"
```

---

## Task 3: Run Tests and Verify Delegate Stubs Work

**Step 3.1: Build the full solution**

Run: `dotnet build`

Expected: PASS

**Step 3.2: Run the open generic delegate tests**

Run: `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj --filter "OpenGenericDelegateTests"`

Expected: All 3 tests PASS

**Step 3.3: Inspect generated code (optional verification)**

Check `src/Tests/KnockOffTests/Generated/KnockOff.Generator/` for the generated `OpenGenericDelegateTests.g.cs` file to verify it contains:
- `public sealed class OGFactory<T>` (generic stub class)
- `public static implicit operator global::OGFactory<T>(OGFactory<T> stub)`
- Interceptor with OnCall callback

**Step 3.4: Commit if tests pass**

```bash
git add .
git commit -m "feat: open generic delegate stubs now working"
```

---

## Task 4: Add Failing Tests for Open Generic Classes

**Files:**
- Modify: `src/Tests/KnockOffTests/OpenGenericInlineStubTests.cs`

**Step 4.1: Add test class and test methods**

Add after the delegate tests:

```csharp
// ============================================================================
// Open Generic Class Tests
// ============================================================================

public abstract class OGRepository<T> where T : class
{
    public abstract T? GetById(int id);
    public abstract void Save(T entity);
    public virtual string Name => "DefaultRepo";
}

public abstract class OGCache<TKey, TValue>
    where TKey : notnull
    where TValue : new()
{
    public abstract TValue Get(TKey key);
    public abstract void Set(TKey key, TValue value);
}

[KnockOff(typeof(OGRepository<>))]
[KnockOff(typeof(OGCache<,>))]
public partial class OpenGenericClassTests
{
    [Fact]
    public void SingleTypeParam_CanInstantiateWithDifferentTypes()
    {
        var userRepo = new Stubs.OGRepository<User>();
        var orderRepo = new Stubs.OGRepository<Order>();

        Assert.NotNull(userRepo);
        Assert.NotNull(orderRepo);
    }

    [Fact]
    public void SingleTypeParam_InterceptorTracksInvocations()
    {
        var stub = new Stubs.OGRepository<User>();
        stub.GetById.OnCall = (ko, id) => new User { Id = id };

        OGRepository<User> repo = stub.Object;
        var user = repo.GetById(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
        Assert.Equal(1, stub.GetById.CallCount);
    }

    [Fact]
    public void VirtualMember_CallsBaseByDefault()
    {
        var stub = new Stubs.OGRepository<User>();

        OGRepository<User> repo = stub.Object;
        var name = repo.Name;

        Assert.Equal("DefaultRepo", name);
    }

    [Fact]
    public void MultipleTypeParams_PreservesConstraints()
    {
        // TKey: notnull, TValue: new()
        var stub = new Stubs.OGCache<string, List<int>>();
        stub.Get.OnCall = (ko, key) => new List<int> { 1, 2, 3 };

        OGCache<string, List<int>> cache = stub.Object;
        var result = cache.Get("test");

        Assert.Equal(3, result.Count);
    }

    // Helper types for tests
    public class User { public int Id { get; set; } }
    public class Order { public int Id { get; set; } }
}
```

**Step 4.2: Run tests to verify they fail (build error expected)**

Run: `dotnet build src/Tests/KnockOffTests/KnockOffTests.csproj`

Expected: Build error - `Stubs.OGRepository<T>` does not exist

**Step 4.3: Commit failing tests**

```bash
git add src/Tests/KnockOffTests/OpenGenericInlineStubTests.cs
git commit -m "test: add failing tests for open generic class stubs"
```

---

## Task 5: Fix Transform Phase for Open Generic Classes

**Files:**
- Modify: `src/Generator/KnockOffGenerator.Transform.cs`

**Step 5.1: Locate class extraction in TransformInlineStubClass**

Find the class handling section (~line 150-165). Currently it extracts ClassStubInfo but doesn't pass open generic info.

**Step 5.2: Update class extraction to handle open generics**

Find this code pattern:
```csharp
else if (typeArg.TypeKind == TypeKind.Class)
{
    var clsInfo = ExtractClassInfo(typeArg, ...);
```

Replace with:
```csharp
else if (typeArg.TypeKind == TypeKind.Class)
{
    // For open generic classes, use OriginalDefinition to get members
    var classSource = isOpenGeneric ? typeArg.OriginalDefinition : typeArg;
    var clsInfo = ExtractClassInfo(
        classSource,
        isOpenGeneric,
        openGenericTypeParams,
        ...);
```

**Step 5.3: Update ExtractClassInfo signature and implementation**

Find `ExtractClassInfo` method (~line 380). Update signature to accept open generic parameters:

```csharp
private static ClassStubInfo? ExtractClassInfo(
    INamedTypeSymbol classType,
    bool isOpenGeneric,
    EquatableArray<TypeParameterInfo> openGenericTypeParams,
    // ... other existing parameters
)
```

Inside the method, set the open generic fields:

```csharp
return new ClassStubInfo(
    // ... existing fields
    IsOpenGeneric: isOpenGeneric,
    TypeParameters: openGenericTypeParams
);
```

**Step 5.4: Run build to check for compilation errors**

Run: `dotnet build src/Generator/KnockOff.Generator.csproj`

Expected: PASS (may have warnings about unused parameters initially)

**Step 5.5: Commit**

```bash
git add src/Generator/KnockOffGenerator.Transform.cs
git commit -m "fix: pass open generic info to ExtractClassInfo"
```

---

## Task 6: Run Tests and Verify Class Stubs Work

**Step 6.1: Build the full solution**

Run: `dotnet build`

Expected: PASS

**Step 6.2: Run the open generic class tests**

Run: `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj --filter "OpenGenericClassTests"`

Expected: All 4 tests PASS

**Step 6.3: Inspect generated code (optional verification)**

Check generated file for:
- `public class OGRepository<T> : global::OGRepository<T>` (generic stub class)
- `where T : class` constraint preserved
- `.Object` property returning the typed instance
- Interceptors for abstract members

**Step 6.4: Commit if tests pass**

```bash
git add .
git commit -m "feat: open generic class stubs now working"
```

---

## Task 7: Run Full Test Suite

**Step 7.1: Run all KnockOff tests**

Run: `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj`

Expected: All tests PASS (no regressions)

**Step 7.2: Run all tests in solution**

Run: `dotnet test`

Expected: All tests PASS

**Step 7.3: Commit any generated file changes**

```bash
git add .
git commit -m "chore: update generated files after open generic support"
```

---

## Task 8: Update Documentation

**Files:**
- Modify: `docs/release-notes/v10.18.0.md` (update "not yet supported" notes)
- Modify: `docs/todos/open-generic-stubs-tracker.md` (mark as complete)
- Move: `docs/todos/generic-delegate-support.md` → `docs/todos/completed/`
- Move: `docs/todos/generic-inline-class-support.md` → `docs/todos/completed/`

**Step 8.1: Update v10.18.0 release notes**

In `docs/release-notes/v10.18.0.md`, change lines 70-71 from:
```markdown
- Open generic class stubs (`[KnockOff(typeof(SomeClass<>))]`) are not yet supported
- Open generic delegate stubs (`[KnockOff(typeof(Factory<>))]`) are not yet supported
```

To:
```markdown
- Open generic class stubs (`[KnockOff(typeof(SomeClass<>))]`) - added in v10.19
- Open generic delegate stubs (`[KnockOff(typeof(Factory<>))]`) - added in v10.19
```

**Step 8.2: Update open-generic-stubs-tracker.md**

Update the support matrix to show ✅ for delegates and classes.

**Step 8.3: Move completed todos**

```bash
mv docs/todos/generic-delegate-support.md docs/todos/completed/
mv docs/todos/generic-inline-class-support.md docs/todos/completed/
```

**Step 8.4: Update status in moved files**

Add `**Status:** Complete` header to both files.

**Step 8.5: Commit documentation updates**

```bash
git add docs/
git commit -m "docs: update for open generic delegate and class support"
```

---

## Task 9: Create Release Notes for v10.19.0

**Files:**
- Create: `docs/release-notes/v10.19.1.md`

**Step 9.1: Create release notes file**

```markdown
# v10.19.1 - Open Generic Delegate and Class Stubs

**Release Date:** 2026-01-15

## Summary

This release completes open generic inline stub support by adding delegates and classes to the existing interface support from v10.18.

## New Features

### Open Generic Delegate Stubs

Use `typeof()` with an unbound generic delegate:

```csharp
public delegate T Factory<T>();

[KnockOff(typeof(Factory<>))]
public partial class MyTests { }

// Use with any type argument:
var stringFactory = new Stubs.Factory<string>();
var intFactory = new Stubs.Factory<int>();

stringFactory.Interceptor.OnCall = (ko) => "hello";
Factory<string> factory = stringFactory;
Assert.Equal("hello", factory());
```

### Open Generic Class Stubs

Use `typeof()` with an unbound generic class:

```csharp
public abstract class Repository<T> where T : class
{
    public abstract T? GetById(int id);
}

[KnockOff(typeof(Repository<>))]
public partial class MyTests { }

// Use with any type argument:
var userRepo = new Stubs.Repository<User>();
userRepo.GetById.OnCall = (ko, id) => new User { Id = id };

Repository<User> repo = userRepo.Object;
var user = repo.GetById(42);
```

### Type Constraints Preserved

Both delegate and class stubs preserve type constraints:

```csharp
public delegate T Factory<T>() where T : new();

[KnockOff(typeof(Factory<>))]
public partial class MyTests { }

// Constraint enforced at compile time:
var stub = new Stubs.Factory<List<int>>();  // OK - List<int> has new()
// var stub = new Stubs.Factory<string>();  // Compile error - string has no new()
```

## Complete Open Generic Support Matrix

|  | **Standalone** | **Inline** |
|--|----------------|------------|
| **Interface** | ✅ v10.14 | ✅ v10.18 |
| **Class** | ❌ N/A | ✅ v10.19 |
| **Delegate** | ❌ N/A | ✅ v10.19 |

Note: Standalone open generic stubs for classes and delegates are not possible due to C# language constraints (cannot inherit from sealed delegate types, standalone pattern requires interface implementation).

## Breaking Changes

None. This is a purely additive release.
```

**Step 9.2: Update release notes index**

Add v10.19.1 to `docs/release-notes/index.md`.

**Step 9.3: Commit release notes**

```bash
git add docs/release-notes/
git commit -m "docs: add v10.19.1 release notes for open generic delegate and class stubs"
```

---

## Task 10: Final Verification

**Step 10.1: Run full build**

Run: `dotnet build`

Expected: PASS with no warnings

**Step 10.2: Run full test suite**

Run: `dotnet test`

Expected: All tests PASS

**Step 10.3: Verify generated files are committed**

Run: `git status`

Expected: Clean working directory (all generated files committed)

**Step 10.4: Create final commit if needed**

```bash
git add .
git commit -m "chore: final cleanup for open generic support"
```

---

## Troubleshooting

### If delegate tests fail with "Invoke method not found"

The delegate's `OriginalDefinition` might not expose the invoke method. Check that `DelegateInfo.Extract` is looking at `delegateType.DelegateInvokeMethod` which should work for both bound and unbound delegates.

### If class tests fail with "no constructors"

Unbound generic classes may not expose constructors via `InstanceConstructors`. The fix is to use `OriginalDefinition.InstanceConstructors` in `ExtractClassInfo`.

### If type constraints are missing

Ensure `openGenericTypeParams` is being passed through the entire chain: Transform → Build → Render. The `TypeParameterInfo` records should already contain constraint strings.

### If generated code has wrong type parameter names

Check `SymbolHelpers.ReplaceUnboundGeneric()` is being called to convert `<>` or `<,>` syntax to actual type parameter names like `<T>` or `<TKey, TValue>`.
