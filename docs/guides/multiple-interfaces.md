# Multiple Interfaces

KnockOff supports implementing multiple interfaces in a single stub class.

## Basic Usage

```csharp
public interface ILogger
{
    void Log(string message);
    string Name { get; set; }
}

public interface INotifier
{
    void Notify(string recipient);
    string Name { get; }  // Same name, different accessor
}

[KnockOff]
public partial class LoggerNotifierKnockOff : ILogger, INotifier { }
```

## AsXYZ() Helper Methods

KnockOff generates a helper method for each interface:

```csharp
var knockOff = new LoggerNotifierKnockOff();

// Get typed references
ILogger logger = knockOff.AsLogger();
INotifier notifier = knockOff.AsNotifier();

// Or cast directly
ILogger logger2 = knockOff;
INotifier notifier2 = knockOff;
```

## Shared Members

### Same Property Name

When multiple interfaces have properties with the same name, they share a backing field:

```csharp
ILogger logger = knockOff;
INotifier notifier = knockOff;

// Set via ILogger (which has setter)
logger.Name = "SharedValue";

// Both interfaces see the same value
Assert.Equal("SharedValue", logger.Name);
Assert.Equal("SharedValue", notifier.Name);

// Tracking accumulates from all interfaces
Assert.Equal(2, knockOff.Spy.Name.GetCount);  // One get from each
Assert.Equal(1, knockOff.Spy.Name.SetCount);
```

### Same Method Signature

When multiple interfaces have methods with identical signatures, they share tracking:

```csharp
public interface ILogger
{
    void Log(string message);
}

public interface IAuditor
{
    void Log(string message);  // Same signature
    void Audit(string action, int userId);
}

[KnockOff]
public partial class LoggerAuditorKnockOff : ILogger, IAuditor { }
```

Usage:

```csharp
var knockOff = new LoggerAuditorKnockOff();
ILogger logger = knockOff;
IAuditor auditor = knockOff;

logger.Log("from logger");
auditor.Log("from auditor");

// Shared handler tracks both calls
Assert.Equal(2, knockOff.Spy.Log.CallCount);
Assert.Equal("from auditor", knockOff.Spy.Log.LastCallArg);

// AllCalls contains both
Assert.Equal(2, knockOff.Spy.Log.AllCalls.Count);
Assert.Equal("from logger", knockOff.Spy.Log.AllCalls[0]);
Assert.Equal("from auditor", knockOff.Spy.Log.AllCalls[1]);
```

## Distinct Members

Unique members from each interface get their own handlers:

```csharp
logger.Log("message");
auditor.Audit("delete", 42);

Assert.True(knockOff.Spy.Log.WasCalled);
Assert.True(knockOff.Spy.Audit.WasCalled);

var auditArgs = knockOff.Spy.Audit.LastCallArgs;
Assert.Equal("delete", auditArgs?.action);
Assert.Equal(42, auditArgs?.userId);
```

## Callbacks

Set callbacks for any member regardless of which interface defines it:

```csharp
// Shared member
knockOff.Spy.Log.OnCall = (ko, message) =>
{
    Console.WriteLine($"[Log] {message}");
};

// IAuditor-specific member
knockOff.Spy.Audit.OnCall = (ko, args) =>
{
    var (action, userId) = args;
    Console.WriteLine($"[Audit] {action} by user {userId}");
};
```

## Common Patterns

### Repository + Unit of Work

```csharp
public interface IRepository
{
    User? GetById(int id);
    void Add(User user);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

[KnockOff]
public partial class DataContextKnockOff : IRepository, IUnitOfWork { }

// Usage
var knockOff = new DataContextKnockOff();

knockOff.Spy.SaveChangesAsync.OnCall = (ko, ct) =>
    Task.FromResult(ko.Spy.Add.CallCount);  // Return count of adds

IRepository repo = knockOff.AsRepository();
IUnitOfWork uow = knockOff.AsUnitOfWork();

repo.Add(new User { Name = "New" });
repo.Add(new User { Name = "Another" });
var saved = await uow.SaveChangesAsync();

Assert.Equal(2, saved);
```

### Logger + Disposable

```csharp
public interface ILogger
{
    void Log(string message);
}

public interface IDisposable
{
    void Dispose();
}

[KnockOff]
public partial class DisposableLoggerKnockOff : ILogger, IDisposable { }

// Verify cleanup
knockOff.Spy.Dispose.OnCall = (ko) =>
{
    Assert.True(ko.Spy.Log.WasCalled, "Should log before disposing");
};
```

### Multiple Repositories

```csharp
public interface IUserRepository
{
    User? GetUser(int id);
}

public interface IOrderRepository
{
    Order? GetOrder(int id);
}

[KnockOff]
public partial class CompositeRepositoryKnockOff : IUserRepository, IOrderRepository { }

// Configure each independently
knockOff.Spy.GetUser.OnCall = (ko, id) => new User { Id = id };
knockOff.Spy.GetOrder.OnCall = (ko, id) => new Order { Id = id };
```

## Conflicting Signatures

If interfaces have members with the same name but **different** signatures, both are tracked separately:

```csharp
public interface IStringProcessor
{
    void Process(string input);
}

public interface IIntProcessor
{
    void Process(int input);
}

[KnockOff]
public partial class DualProcessorKnockOff : IStringProcessor, IIntProcessor { }
```

Each gets its own handler because the signatures differ.
