# Moq to KnockOff Migration Analysis

## Summary

This document analyzes Moq usage patterns in NeatooATM to determine what features KnockOff needs to support a complete migration.

## NeatooATM Test Project Inventory

| Project | Files with Moq | Focus |
|---------|----------------|-------|
| `NeatooATM/tests/ATM.DomainModels.Tests` | 10 files | Domain rule testing |
| `OpenDDD/tests/ATM.Application.Tests` | 7 files | Actions & domain services |
| `MsDdd/tests/ATM.Application.Tests` | 5 files | Command handlers & validators |

**Total: 20 files using Moq**

## Moq Usage Pattern Inventory

### Pattern Frequency

| Pattern | Occurrences | KnockOff Support |
|---------|-------------|------------------|
| `.Setup().Returns()` | 235 | ✅ Phase 9 (OnCall) |
| `It.IsAny<T>()` | 211 | ✅ N/A (implicit in callbacks) |
| `.ReturnsAsync()` | 168 | ✅ Phase 9 (OnCall returns Task) |
| `.Verify(Times.Once/Never)` | 51 | ✅ Via CallCount |
| `.As<T>()` interface cast | 19 | ✅ Multi-interface + Phase 10 for indexers |
| `.Callback<T>()` | 4 | ✅ Phase 9 (OnCall + AllCalls) |
| `It.Is<T>(predicate)` | 1 | ✅ Manual check in callback |

### Pattern Details

#### 1. Setup with Returns (235 occurrences) - **CRITICAL**
```csharp
// Moq
_repositoryMock.Setup(x => x.UnitOfWork).Returns(_unitOfWorkMock.Object);
_repositoryMock.Setup(x => x.Add(It.IsAny<Employee>())).Returns<Employee>(e => e);

// KnockOff equivalent needed
// Option A: Per-method callback
knockOff.Spy.Add.OnCall = (entity) => entity; // Returns the input
knockOff.Spy.UnitOfWork.ReturnsValue = _unitOfWorkKnockOff;

// Option B: User method (current approach - limited)
protected Employee Add(Employee entity) => entity; // Works but static
```

#### 2. ReturnsAsync (168 occurrences) - **CRITICAL**
```csharp
// Moq
_uniqueIdCheckerMock.Setup(x => x.IsAvailableAsync(100, null, null, It.IsAny<CancellationToken>()))
    .ReturnsAsync(true);

// KnockOff equivalent needed
knockOff.Spy.IsAvailableAsync.OnCall = (id, excludeId, buildingId, ct) =>
    Task.FromResult(true);
```

#### 3. It.IsAny<T>() (211 occurrences) - **CRITICAL**
```csharp
// Moq - matches any value
mock.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
mock.Setup(x => x.Add(It.IsAny<Employee>())).Returns<Employee>(e => e);

// KnockOff - callbacks receive actual args, so this is implicit
// All calls are tracked in AllCalls, callback receives actual values
```

#### 4. Verify with Times (51 occurrences) - **IMPORTANT**
```csharp
// Moq
_repositoryMock.Verify(x => x.Add(It.IsAny<Employee>()), Times.Once);
_unitOfWorkMock.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

// KnockOff equivalent (exists!)
Assert.Equal(1, knockOff.Spy.Add.CallCount);  // Times.Once
Assert.Equal(0, knockOff.Spy.Add.CallCount);  // Times.Never
Assert.True(knockOff.Spy.Add.CallCount >= 1); // Times.AtLeastOnce
```

#### 5. Mock.As<T>() Interface Casting (19 occurrences) - **PARTIAL SUPPORT**
```csharp
// Moq - setup same mock as different interface
mock.As<IEntityBase>().Setup(e => e.IsNew).Returns(isNew);
mock.As<IEntityBase>().Setup(e => e["FirstName"]).Returns(MockPropertyInfoFactory.Create(firstNameModified));

// KnockOff - works if class implements both interfaces
// [KnockOff] class EntityKnockOff : IEmployeeEdit, IEntityBase { }
// However, indexer properties need explicit handling
```

#### 6. Callback for Argument Capture (4 occurrences) - **PARTIAL SUPPORT**
```csharp
// Moq
Employee? capturedEmployee = null;
_repositoryMock.Setup(x => x.Add(It.IsAny<Employee>()))
    .Callback<Employee>(e => capturedEmployee = e)
    .Returns<Employee>(e => e);

// KnockOff equivalent (exists via AllCalls!)
var knockOff = new EmployeeRepositoryKnockOff();
// ... call code under test ...
var capturedEmployee = knockOff.Spy.Add.LastCallArg;
// or for all calls:
var allCaptured = knockOff.Spy.Add.AllCalls;
```

#### 7. It.Is<T>(predicate) (1 occurrence) - **LOW PRIORITY**
```csharp
// Moq - conditional matching
mock.Setup(x => x.Find(It.Is<Employee>(e => e.BusinessId.Value == 100))).Returns(entity);

// KnockOff - would need predicate-based callback selection
// Not needed for migration - can use callback with manual checks
```

## Feature Gap Analysis

### Already Supported by KnockOff

| Feature | KnockOff Implementation |
|---------|------------------------|
| Track call count | `Spy.Method.CallCount` |
| Track if called | `Spy.Method.WasCalled` |
| Capture last argument | `Spy.Method.LastCallArg` |
| Capture all arguments | `Spy.Method.AllCalls` |
| Property get tracking | `Spy.Property.GetCount` |
| Property set tracking | `Spy.Property.SetCount`, `LastSetValue` |
| Interface casting | `AsInterfaceName()` methods |
| Multiple interfaces | Single KnockOff implementing multiple interfaces |
| Async methods | Task/ValueTask with proper returns |
| Generic interfaces | `IRepository<T>` works correctly |
| Interface inheritance | Base interface members included |

### Needed for Moq Migration

#### Phase 9: Callbacks (CRITICAL) - Current Plan

The planned Phase 9 callback support will address the most common pattern:

```csharp
// Handler gets OnCall callback
public Func<TArg1, TArg2, TReturn>? OnCall { get; set; }

// Generated interface implementation checks callback:
TReturn IInterface.Method(TArg1 arg1, TArg2 arg2)
{
    Spy.Method.RecordCall(arg1, arg2);
    if (Spy.Method.OnCall is { } callback)
        return callback(arg1, arg2);
    // else existing behavior (user method or default/throw)
}
```

This covers:
- `.Setup().Returns()` - 235 uses
- `.ReturnsAsync()` - 168 uses
- `.Callback<T>()` - 4 uses (OnCall can capture args)

**Coverage after Phase 9: ~95% of Moq usage patterns**

#### Phase 10: Verify Helpers (NICE TO HAVE)

Add convenience methods for common verification patterns:

```csharp
// Generated extension methods or assertions
public void VerifyCalledOnce() => Assert.Equal(1, CallCount);
public void VerifyNeverCalled() => Assert.Equal(0, CallCount);
public void VerifyCalledTimes(int expected) => Assert.Equal(expected, CallCount);
```

However, these are easily done manually with existing `CallCount`:
```csharp
Assert.Equal(1, knockOff.Spy.Add.CallCount); // Times.Once
```

### Not Needed

| Moq Feature | Why Not Needed |
|-------------|----------------|
| `It.IsAny<T>()` | Callbacks receive actual args - matching is implicit |
| `It.Is<T>(predicate)` | Rare (1 use), can check args in callback |
| `SetupSequence` | Not used in NeatooATM |
| `ThrowsAsync` | Callback can throw if needed |
| `VerifyNoOtherCalls` | Not used in NeatooATM |
| Strict/Loose mock modes | KnockOff always tracks all calls |

## Migration Effort Estimate

### With Current KnockOff (No Callbacks)

Files fully migratable: **~5 files** (tracking-only tests)
Files requiring workarounds: **15 files** (need protected user methods for each return value)

### With Phase 9 Callbacks

Files fully migratable: **~18 files**
Files requiring additional work: **2 files** (complex `As<T>()` setups with indexer properties)

## Recommendations

1. **Complete Phase 9 (Callbacks)** - This is the critical missing feature
   - Covers 95%+ of Moq usage patterns
   - Signature: `Func<TKnockOff, TArgs..., TReturn>` gives access to knockoff instance

2. **Indexer Property Support** - Consider for interfaces with indexers
   - `IEntityBase["PropertyName"]` pattern used in Neatoo
   - Could generate backing dictionary with tracking

3. **Consider: Returns Value Property** - Simpler than callback for static returns
   ```csharp
   public TReturn? ReturnsValue { get; set; }
   // Generated code checks ReturnsValue before OnCall
   ```

4. **Documentation** - Create migration guide showing Moq → KnockOff translations

## Migration Priority

1. **High**: `ATM.Application.Tests` (MsDdd, OpenDDD) - Standard mocking patterns
2. **Medium**: `ATM.DomainModels.Tests` - Some complex `As<T>()` patterns
3. **Low**: Files using indexer properties - Need additional feature

## Appendix: Sample Migration

### Before (Moq)
```csharp
public class CreateEmployeeCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public CreateEmployeeCommandHandlerTests()
    {
        _repositoryMock = new Mock<IEmployeeRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _repositoryMock.Setup(x => x.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Handle_ShouldAddEmployeeToRepository()
    {
        _repositoryMock.Setup(x => x.Add(It.IsAny<Employee>())).Returns<Employee>(e => e);
        // ... test code ...
        _repositoryMock.Verify(x => x.Add(It.IsAny<Employee>()), Times.Once);
    }
}
```

### After (KnockOff with Phase 9 Callbacks)
```csharp
[KnockOff]
public partial class EmployeeRepositoryKnockOff : IEmployeeRepository { }

[KnockOff]
public partial class UnitOfWorkKnockOff : IUnitOfWork { }

public class CreateEmployeeCommandHandlerTests
{
    private readonly EmployeeRepositoryKnockOff _repository;
    private readonly UnitOfWorkKnockOff _unitOfWork;

    public CreateEmployeeCommandHandlerTests()
    {
        _repository = new EmployeeRepositoryKnockOff();
        _unitOfWork = new UnitOfWorkKnockOff();
        _repository.Spy.UnitOfWork.ReturnsValue = _unitOfWork.AsUnitOfWork();
        _unitOfWork.Spy.SaveEntitiesAsync.OnCall = (ct) => Task.FromResult(true);
    }

    [Fact]
    public async Task Handle_ShouldAddEmployeeToRepository()
    {
        _repository.Spy.Add.OnCall = (entity) => entity; // Returns input
        // ... test code ...
        Assert.Equal(1, _repository.Spy.Add.CallCount); // Times.Once
        var addedEmployee = _repository.Spy.Add.LastCallArg;
    }
}
```
