namespace KnockOff.Tests;

/// <summary>
/// Tests for multiple interface implementation scenarios.
/// With interface-scoped spy handlers, each interface has its own tracking.
/// </summary>
public class MultiInterfaceTests
{
	[Fact]
	public void MultiInterface_DifferentMethods_BothInterfacesWork()
	{
		var knockOff = new MultiInterfaceKnockOff();
		ILogger logger = knockOff;
		INotifier notifier = knockOff;

		logger.Log("test message");
		logger.Name = "Logger1";
		notifier.Notify("user@example.com");

		// Each interface has its own spy handlers
		Assert.True(knockOff.ILogger.Log.WasCalled);
		Assert.Equal("test message", knockOff.ILogger.Log.LastCallArg);
		Assert.Equal(1, knockOff.ILogger.Name.SetCount);
		Assert.Equal("Logger1", knockOff.ILogger.Name.LastSetValue);
		Assert.True(knockOff.INotifier.Notify.WasCalled);
		Assert.Equal("user@example.com", knockOff.INotifier.Notify.LastCallArg);
	}

	[Fact]
	public void MultiInterface_AsMethodsWork_ForBothInterfaces()
	{
		var knockOff = new MultiInterfaceKnockOff();

		ILogger logger = knockOff.AsLogger();
		INotifier notifier = knockOff.AsNotifier();

		logger.Log("via logger");
		notifier.Notify("via notifier");

		Assert.True(knockOff.ILogger.Log.WasCalled);
		Assert.True(knockOff.INotifier.Notify.WasCalled);
	}

	[Fact]
	public void MultiInterface_SameNameProperty_SeparateBacking()
	{
		// ILogger.Name is get/set, INotifier.Name is get-only
		// Each interface has its own backing field now
		var knockOff = new MultiInterfaceKnockOff();
		ILogger logger = knockOff;
		INotifier notifier = knockOff;

		logger.Name = "LoggerValue";

		var loggerName = logger.Name;
		var notifierName = notifier.Name;

		Assert.Equal("LoggerValue", loggerName);
		Assert.Equal("", notifierName); // Separate backing, default value
		Assert.Equal(1, knockOff.ILogger.Name.SetCount);
		Assert.Equal(1, knockOff.ILogger.Name.GetCount);
		Assert.Equal(1, knockOff.INotifier.Name.GetCount);
	}

	[Fact]
	public void SharedSignature_SameMethodSignature_SeparateTracking()
	{
		// With interface-scoped handlers, ILogger.Log and IAuditor.Log
		// have separate tracking even with the same signature
		var knockOff = new SharedSignatureKnockOff();
		ILogger logger = knockOff;
		IAuditor auditor = knockOff;

		logger.Log("logger message");
		auditor.Log("auditor message");

		// Each interface tracks separately
		Assert.Equal(1, knockOff.ILogger.Log.CallCount);
		Assert.Equal("logger message", knockOff.ILogger.Log.LastCallArg);

		Assert.Equal(1, knockOff.IAuditor.Log.CallCount);
		Assert.Equal("auditor message", knockOff.IAuditor.Log.LastCallArg);
	}

	[Fact]
	public void SharedSignature_UniqueMethodsStillTracked()
	{
		var knockOff = new SharedSignatureKnockOff();
		ILogger logger = knockOff;
		IAuditor auditor = knockOff;

		auditor.Audit("delete", 42);

		Assert.True(knockOff.IAuditor.Audit.WasCalled);
		var args = knockOff.IAuditor.Audit.LastCallArgs;
		Assert.NotNull(args);
		Assert.Equal("delete", args.Value.action);
		Assert.Equal(42, args.Value.userId);
		Assert.False(knockOff.ILogger.Log.WasCalled);
	}
}
