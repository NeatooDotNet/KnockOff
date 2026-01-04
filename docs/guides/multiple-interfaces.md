# Multiple Interfaces

KnockOff supports implementing multiple interfaces in a single stub class. Each interface gets its own spy class with separate handlers.

## Basic Usage

<!-- snippet: docs:multiple-interfaces:basic-usage -->
```csharp
public interface IMiLogger
{
    void Log(string message);
    string Name { get; set; }
}

public interface IMiNotifier
{
    void Notify(string recipient);
    string Name { get; }  // Same name, different accessor
}

[KnockOff]
public partial class MiLoggerNotifierKnockOff : IMiLogger, IMiNotifier { }
```
<!-- /snippet -->

## Interface Spy Classes

Each interface gets its own spy class and property:

```csharp
var knockOff = new LoggerNotifierKnockOff();

// Each interface has its own spy class
knockOff.ILogger.Log.CallCount;
knockOff.ILogger.Name.GetCount;

knockOff.INotifier.Notify.CallCount;
knockOff.INotifier.Name.GetCount;
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

## Separate Tracking

Each interface's members are tracked independently, even for same-named members:

<!-- snippet: docs:multiple-interfaces:shared-method -->
```csharp
public interface IMiLoggerSame
{
    void Log(string message);
}

public interface IMiAuditor
{
    void Log(string message);  // Same signature
    void Audit(string action, int userId);
}

[KnockOff]
public partial class MiLoggerAuditorKnockOff : IMiLoggerSame, IMiAuditor { }
```
<!-- /snippet -->

Usage:

```csharp
var knockOff = new LoggerAuditorKnockOff();
ILogger logger = knockOff;
IAuditor auditor = knockOff;

logger.Log("from logger");
auditor.Log("from auditor");

// Each interface tracks its own calls
Assert.Equal(1, knockOff.ILogger.Log.CallCount);
Assert.Equal("from logger", knockOff.ILogger.Log.LastCallArg);

Assert.Equal(1, knockOff.IAuditor.Log.CallCount);
Assert.Equal("from auditor", knockOff.IAuditor.Log.LastCallArg);

// Audit is only on IAuditor
Assert.Equal(0, knockOff.IAuditor.Audit.CallCount);
```

## Separate Backing Fields

Each interface gets its own backing fields:

```csharp
ILogger logger = knockOff;
INotifier notifier = knockOff;

// Each interface has its own backing
logger.Name = "LoggerValue";

// INotifier.Name has a separate backing
Assert.Equal("LoggerValue", knockOff.ILogger_NameBacking);
Assert.Equal("", knockOff.INotifier_NameBacking);  // Still default

// Access via interface uses its own backing
Assert.Equal("LoggerValue", logger.Name);
Assert.Equal("", notifier.Name);  // Different value!
```

## Callbacks

Set callbacks using the interface spy class:

```csharp
// ILogger.Log callback
knockOff.ILogger.Log.OnCall = (ko, message) =>
{
    Console.WriteLine($"[Logger] {message}");
};

// IAuditor.Log callback (separate from ILogger.Log)
knockOff.IAuditor.Log.OnCall = (ko, message) =>
{
    Console.WriteLine($"[Auditor] {message}");
};

// IAuditor.Audit callback
knockOff.IAuditor.Audit.OnCall = (ko, action, userId) =>
{
    Console.WriteLine($"[Audit] {action} by user {userId}");
};
```

## Common Patterns

### Repository + Unit of Work

<!-- snippet: docs:multiple-interfaces:repo-uow -->
```csharp
public interface IMiRepository
{
    MiUser? GetById(int id);
    void Add(MiUser user);
}

public interface IMiUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

[KnockOff]
public partial class MiDataContextKnockOff : IMiRepository, IMiUnitOfWork { }
```
<!-- /snippet -->

```csharp
// Usage
var knockOff = new MiDataContextKnockOff();

knockOff.IUnitOfWork.SaveChangesAsync.OnCall = (ko, ct) =>
    Task.FromResult(ko.IRepository.Add.CallCount);  // Return count of adds

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
knockOff.IDisposable.Dispose.OnCall = (ko) =>
{
    Assert.True(ko.ILogger.Log.WasCalled, "Should log before disposing");
};
```

### Multiple Repositories

<!-- snippet: docs:multiple-interfaces:multiple-repos -->
```csharp
[KnockOff]
public partial class MiCompositeRepositoryKnockOff : IMiUserRepository, IMiOrderRepository { }
```
<!-- /snippet -->

```csharp
// Configure each interface independently
knockOff.IMiUserRepository.GetUser.OnCall = (ko, id) => new MiUser { Id = id };
knockOff.IMiOrderRepository.GetOrder.OnCall = (ko, id) => new MiOrder { Id = id };
```

## Same-Named Members

When interfaces have members with the same name (even with identical signatures), each interface gets its own handler:

```csharp
public interface IStringProcessor
{
    void Process(string input);
}

public interface IIntProcessor
{
    void Process(int input);  // Different parameter type
}

[KnockOff]
public partial class DualProcessorKnockOff : IStringProcessor, IIntProcessor { }
```

Each interface has its own handler accessed via its spy class:

```csharp
knockOff.IStringProcessor.Process.OnCall = (ko, input) =>
    Console.WriteLine($"String: {input}");

knockOff.IIntProcessor.Process.OnCall = (ko, input) =>
    Console.WriteLine($"Int: {input}");

// Track separately
Assert.Equal(1, knockOff.IStringProcessor.Process.CallCount);
Assert.Equal(1, knockOff.IIntProcessor.Process.CallCount);
```
