using KnockOff;

namespace KnockOff.Tests;

/// <summary>
/// Interface for testing VerifyAll() vs Verify() behavior.
/// </summary>
public interface IVerifyAllTestService
{
	string GetData(int id);
	void SaveData(string data);
	string Name { get; set; }
}

/// <summary>
/// Tests to verify VerifyAll() behavior vs Verify() behavior.
///
/// EXPECTED BEHAVIOR (from KnockOff skill documentation):
/// - Verify()     - Checks only members marked with .Verifiable()
/// - VerifyAll()  - Checks ALL configured members (any member with OnCall, OnGet, etc.)
///
/// ACTUAL TEST RESULTS:
/// ✅ VerifyAll() DOES throw VerificationException when configured members aren't called
/// ✅ Verify() DOES only check members marked with .Verifiable()
/// ✅ Both behaviors match documentation
///
/// IMPLICATION:
/// If a test has configured stubs (OnCall, OnGet) but doesn't call them,
/// calling stub.VerifyAll() will FAIL with VerificationException.
/// This is the expected and correct behavior.
/// </summary>
[KnockOff<IVerifyAllTestService>]
public partial class VerifyAllBehaviorTests
{
	/// <summary>
	/// TEST: VerifyAll() should fail when a method is configured but not called.
	/// RESULT: ✅ PASSES - VerificationException is thrown with message "GetData: expected AtLeastOnce, actual 0 calls"
	/// </summary>
	[Fact]
	public void VerifyAll_ConfiguredButNotCalled_ShouldFail()
	{
		// Arrange
		var stub = new Stubs.IVerifyAllTestService().Strict();
		stub.GetData.Return((id) => $"Data-{id}");

		// Act - don't call anything

		// Assert - VerifyAll() should FAIL because GetData was configured but not called
		var exception = Assert.Throws<VerificationException>(() => stub.VerifyAll());
		Assert.Contains("GetData", exception.Message);
		Assert.Contains("expected AtLeastOnce, actual 0 calls", exception.Message);
	}

	/// <summary>
	/// TEST: VerifyAll() should pass when all configured members are called.
	/// RESULT: ✅ PASSES
	/// </summary>
	[Fact]
	public void VerifyAll_ConfiguredAndCalled_ShouldPass()
	{
		// Arrange
		var stub = new Stubs.IVerifyAllTestService().Strict();
		stub.GetData.Return((id) => $"Data-{id}");

		// Act - call the configured method
		IVerifyAllTestService service = stub;
		service.GetData(1);

		// Assert - This should pass because configured method was called
		stub.VerifyAll();
	}

	/// <summary>
	/// TEST: VerifyAll() should fail when multiple members are configured but only some are called.
	/// RESULT: ✅ PASSES - VerificationException is thrown with message "SaveData: expected AtLeastOnce, actual 0 calls"
	/// </summary>
	[Fact]
	public void VerifyAll_MultipleConfiguredOnlyOneCalled_ShouldFail()
	{
		// Arrange
		var stub = new Stubs.IVerifyAllTestService().Strict();
		stub.GetData.Return((id) => $"Data-{id}");
		stub.SaveData.Call((data) => { });

		// Act - only call GetData, not SaveData
		IVerifyAllTestService service = stub;
		service.GetData(1);

		// Assert - Should FAIL because SaveData was configured but not called
		var exception = Assert.Throws<VerificationException>(() => stub.VerifyAll());
		Assert.Contains("SaveData", exception.Message);
		Assert.Contains("expected AtLeastOnce, actual 0 calls", exception.Message);
	}

	/// <summary>
	/// TEST: VerifyAll() should fail when a property is configured but not accessed.
	/// RESULT: ✅ PASSES - VerificationException is thrown with message "Name: expected AtLeastOnce, actual 0 calls"
	/// </summary>
	[Fact]
	public void VerifyAll_PropertyConfiguredButNotAccessed_ShouldFail()
	{
		// Arrange
		var stub = new Stubs.IVerifyAllTestService().Strict();
		stub.Name.OnGet("TestName");

		// Act - don't access the property

		// Assert - Should FAIL because Name getter was configured but not accessed
		var exception = Assert.Throws<VerificationException>(() => stub.VerifyAll());
		Assert.Contains("Name", exception.Message);
		Assert.Contains("expected AtLeastOnce, actual 0 calls", exception.Message);
	}

	/// <summary>
	/// TEST: VerifyAll() should pass when nothing is configured.
	/// RESULT: ✅ PASSES
	/// </summary>
	[Fact]
	public void VerifyAll_NoConfiguration_ShouldPass()
	{
		// Arrange
		var stub = new Stubs.IVerifyAllTestService().Strict();

		// Act - nothing configured, nothing called

		// Assert - Should pass because there's nothing to verify
		stub.VerifyAll();
	}

	/// <summary>
	/// TEST: Verify() should fail when a method is marked Verifiable but not called.
	/// RESULT: ✅ PASSES - VerificationException is thrown
	/// </summary>
	[Fact]
	public void Verify_WithVerifiable_ConfiguredButNotCalled_ShouldFail()
	{
		// Arrange
		var stub = new Stubs.IVerifyAllTestService().Strict();
		stub.GetData.Return((id) => $"Data-{id}").Verifiable();

		// Act - don't call anything

		// Assert - Verify() should FAIL because GetData was marked Verifiable but not called
		var exception = Assert.Throws<VerificationException>(() => stub.Verify());
		Assert.Contains("GetData", exception.Message);
	}

	/// <summary>
	/// TEST: Verify() should pass when methods are configured but NOT marked Verifiable.
	/// This is the key difference between Verify() and VerifyAll().
	/// RESULT: ✅ PASSES
	/// </summary>
	[Fact]
	public void Verify_ConfiguredButNoVerifiable_ShouldPass()
	{
		// Arrange
		var stub = new Stubs.IVerifyAllTestService().Strict();
		stub.GetData.Return((id) => $"Data-{id}"); // No .Verifiable()

		// Act - don't call anything

		// Assert - Verify() should PASS because nothing was marked Verifiable
		stub.Verify();
	}
}
