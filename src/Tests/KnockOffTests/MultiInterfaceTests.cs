namespace KnockOff.Tests;

/// <summary>
/// Tests for multiple interface implementation scenarios.
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

		Assert.True(knockOff.Spy.Log.WasCalled);
		Assert.Equal("test message", knockOff.Spy.Log.LastCallArg);
		Assert.Equal(1, knockOff.Spy.Name.SetCount);
		Assert.Equal("Logger1", knockOff.Spy.Name.LastSetValue);
		Assert.True(knockOff.Spy.Notify.WasCalled);
		Assert.Equal("user@example.com", knockOff.Spy.Notify.LastCallArg);
	}

	[Fact]
	public void MultiInterface_AsMethodsWork_ForBothInterfaces()
	{
		var knockOff = new MultiInterfaceKnockOff();

		ILogger logger = knockOff.AsLogger();
		INotifier notifier = knockOff.AsNotifier();

		logger.Log("via logger");
		notifier.Notify("via notifier");

		Assert.True(knockOff.Spy.Log.WasCalled);
		Assert.True(knockOff.Spy.Notify.WasCalled);
	}

	[Fact]
	public void MultiInterface_SharedProperty_UsesSharedBacking()
	{
		var knockOff = new MultiInterfaceKnockOff();
		ILogger logger = knockOff;
		INotifier notifier = knockOff;

		logger.Name = "SharedValue";

		var loggerName = logger.Name;
		var notifierName = notifier.Name;

		Assert.Equal("SharedValue", loggerName);
		Assert.Equal("SharedValue", notifierName);
		Assert.Equal(1, knockOff.Spy.Name.SetCount);
		Assert.Equal(2, knockOff.Spy.Name.GetCount);
	}

	[Fact]
	public void SharedSignature_SameMethodSignature_SharesTracking()
	{
		var knockOff = new SharedSignatureKnockOff();
		ILogger logger = knockOff;
		IAuditor auditor = knockOff;

		logger.Log("logger message");
		auditor.Log("auditor message");

		Assert.Equal(2, knockOff.Spy.Log.CallCount);
		Assert.Equal("auditor message", knockOff.Spy.Log.LastCallArg);
		Assert.Equal(2, knockOff.Spy.Log.AllCalls.Count);
		Assert.Equal("logger message", knockOff.Spy.Log.AllCalls[0]);
		Assert.Equal("auditor message", knockOff.Spy.Log.AllCalls[1]);
	}

	[Fact]
	public void SharedSignature_UniqueMethodsStillTracked()
	{
		var knockOff = new SharedSignatureKnockOff();
		ILogger logger = knockOff;
		IAuditor auditor = knockOff;

		auditor.Audit("delete", 42);

		Assert.True(knockOff.Spy.Audit.WasCalled);
		var args = knockOff.Spy.Audit.LastCallArgs;
		Assert.NotNull(args);
		Assert.Equal("delete", args.Value.action);
		Assert.Equal(42, args.Value.userId);
		Assert.False(knockOff.Spy.Log.WasCalled);
	}
}
