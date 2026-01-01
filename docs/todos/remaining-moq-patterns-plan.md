# Plan: Remaining Moq Patterns Coverage

## Current Status (After Phase 10)

| Pattern | Count | Status |
|---------|-------|--------|
| `.Setup().Returns()` | 235 | ✅ COVERED (OnCall) |
| `It.IsAny<T>()` | 211 | ✅ N/A (implicit in callbacks) |
| `.ReturnsAsync()` | 168 | ✅ COVERED (OnCall returns Task) |
| `.Verify(Times.X)` | 51 | ✅ Via CallCount (manual assertion) |
| `.As<T>()` | 19 | ✅ Multi-interface + AsXYZ() + Indexers |
| `.Callback<T>()` | 4 | ✅ COVERED (OnCall + AllCalls) |
| `It.Is<T>(predicate)` | 1 | ✅ Manual check in callback |
| Indexer `this[key]` | 19 | ✅ COVERED (Phase 10) |

**Coverage: 100% of patterns fully supported**

## Remaining Gaps

### 1. Indexer Properties ✅ COMPLETE (Phase 10)

Interfaces with indexer properties are now fully supported:
```csharp
interface IEntityBase
{
    IEntityProperty this[string propertyName] { get; }
    bool IsNew { get; }
}

// KnockOff Usage:
knockOff.Spy.StringIndexer.OnGet = (ko, propertyName) =>
{
    return propertyName switch
    {
        "FirstName" => MockPropertyInfo.Create(true),
        "LastName" => MockPropertyInfo.Create(false),
        _ => null
    };
};

// Or pre-populate the backing dictionary:
knockOff.StringIndexerBacking["FirstName"] = MockPropertyInfo.Create(true);
knockOff.StringIndexerBacking["LastName"] = MockPropertyInfo.Create(false);
```

### 2. Verify Helpers (LOW PRIORITY - CONVENIENCE)

**Problem:** Current verification requires manual assertions:
```csharp
Assert.Equal(1, knockOff.Spy.Add.CallCount); // Times.Once
Assert.True(knockOff.Spy.Add.CallCount >= 1); // Times.AtLeastOnce
```

**Proposed Solution:** Add fluent extension methods:
```csharp
knockOff.Spy.Add.VerifyCalledOnce();
knockOff.Spy.Add.VerifyNeverCalled();
knockOff.Spy.Add.VerifyCalledTimes(3);
knockOff.Spy.Add.VerifyCalledAtLeast(1);
```

### 3. ReturnsValue Property (LOW PRIORITY - CONVENIENCE)

**Problem:** Simple static returns require verbose callback:
```csharp
knockOff.Spy.GetValue.OnCall = (ko, id) => 42; // Verbose for static value
```

**Proposed Solution:** Add `ReturnsValue` for simpler cases:
```csharp
knockOff.Spy.GetValue.ReturnsValue = 42; // Simpler!
// Generated code checks ReturnsValue before OnCall
```

### 4. Sequential Returns (VERY LOW PRIORITY)

**Problem:** Returning different values on successive calls:
```csharp
// Moq
mock.SetupSequence(x => x.GetNext()).Returns(1).Returns(2).Returns(3);
```

**Workaround with current KnockOff:**
```csharp
var returnValues = new Queue<int>(new[] { 1, 2, 3 });
knockOff.Spy.GetNext.OnCall = (ko) => returnValues.Dequeue();
```

**No dedicated feature needed** - workaround is simple.

---

## Implementation Plan

### Phase 10: Indexer Support (HIGH)

**Goal:** Support interfaces with indexer properties (`this[T key]`)

#### 10.1 Detection
- [ ] Detect indexer properties in interface (property with `IsIndexer = true`)
- [ ] Extract indexer key type and return type
- [ ] Handle multiple indexers (rare but possible)

#### 10.2 Handler for Indexers
- [ ] Generate `IndexerHandler` class
- [ ] Track: `GetCount`, `AllCalls` (list of keys accessed)
- [ ] Callback: `Func<TKnockOff, TKey, TReturn>? OnCall`

#### 10.3 Implementation Generation
- [ ] Generate backing dictionary: `Dictionary<TKey, TReturn>`
- [ ] Generate explicit indexer implementation
- [ ] Check OnCall first, then backing dictionary, then default

#### 10.4 Verification
- [ ] Test: Indexer get is tracked
- [ ] Test: OnCall callback works with different keys
- [ ] Test: Multiple indexers on same interface

**Estimated complexity:** Medium

### Phase 11: Verify Helpers (LOW)

**Goal:** Add fluent verification methods for cleaner assertions

#### 11.1 Instance Methods on Handler
- [ ] Add `VerifyCalledOnce()` - throws if CallCount != 1
- [ ] Add `VerifyNeverCalled()` - throws if CallCount != 0
- [ ] Add `VerifyCalledTimes(int expected)` - throws if mismatch
- [ ] Add `VerifyCalledAtLeast(int minimum)` - throws if CallCount < minimum

#### 11.2 Property-Specific Verify
- [ ] Add `VerifyGetterCalledOnce()` for properties
- [ ] Add `VerifySetterCalledOnce()` for properties
- [ ] Add `VerifySetWith(TValue expected)` - checks LastSetValue

#### 11.3 Implementation
- [ ] Generate methods directly in Handler classes
- [ ] Use descriptive exception messages

**Estimated complexity:** Low

### Phase 12: ReturnsValue Shorthand (LOW)

**Goal:** Simplify static return value configuration

#### 12.1 Property on Handler
- [ ] Add `TReturn? ReturnsValue { get; set; }` for methods with returns
- [ ] Add to property getter Handler as well

#### 12.2 Generated Code Priority
```csharp
TReturn IInterface.Method(TArg arg)
{
    Spy.Method.RecordCall(arg);
    if (Spy.Method.ReturnsValue is { } value)
        return value;
    if (Spy.Method.OnCall is { } callback)
        return callback(this, arg);
    // ... existing fallback
}
```

#### 12.3 Reset Behavior
- [ ] Reset() clears ReturnsValue along with other state

**Estimated complexity:** Low

---

## Priority Matrix

| Phase | Feature | Priority | Effort | NeatooATM Coverage |
|-------|---------|----------|--------|-------------------|
| 10 | Indexer Support | ✅ COMPLETE | Medium | All files unblocked |
| 11 | Verify Helpers | LOW | Low | Convenience only |
| 12 | ReturnsValue | LOW | Low | Convenience only |

## Migration Coverage After Each Phase

| Phase | Files Migratable | Coverage |
|-------|------------------|----------|
| 9 (Callbacks) | 18/20 | 90% |
| 10 (Indexers) ✅ | 20/20 | 100% |
| 11 (Verify) | 20/20 | 100% + better DX |
| 12 (ReturnsValue) | 20/20 | 100% + better DX |

## Current Status

**Phase 10 (Indexers) is COMPLETE** - All 20 NeatooATM test files can now be migrated from Moq to KnockOff.

Phases 11 and 12 are convenience features that improve developer experience but don't block any migrations. They can be deferred or implemented based on user feedback.

---

## Appendix: NeatooATM Files Requiring Indexer Support

1. `ATM.DomainModels.Tests/Rules/Employee/EmployeeUniqueRuleTests.cs`
   - Uses `mock.As<IEntityBase>().Setup(e => e["FirstName"])` etc.

2. `ATM.DomainModels.Tests/Rules/Employee/EmployeeUniqueIdRuleTests.cs`
   - Uses `mock.As<IEntityBase>().Setup(e => e["Id"])`

3. `ATM.DomainModels.Tests/Rules/Building/BuildingUniqueIdRuleTests.cs`
   - Uses `mock.As<IEntityBase>().Setup(e => e["Id"])`

4. `ATM.DomainModels.Tests/Rules/Shift/ShiftOverlapRuleTests.cs`
   - Uses `mock.As<IEntityBase>().Setup(e => e["EmployeeId"])` etc.

All use the same pattern: accessing `IEntityBase[propertyName]` to get `IEntityProperty` with `IsModified` state.
