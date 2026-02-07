using KnockOff;
using System.Threading.Tasks;
using Xunit;

namespace KnockOff.Tests;

/// <summary>
/// Tests for the base class user method pattern.
/// In this pattern, the generator creates a base class with virtual methods (suffixed with _),
/// and users provide overrides to define default behavior.
/// </summary>
public class BaseClassUserMethodTests
{
    #region 1. BaseClassGenerationTests - Verify base class file is generated correctly

    [Fact]
    public void BaseClass_ExistsForStandaloneStub()
    {
        // Arrange & Act - If this compiles, the base class was generated
        var stub = new StrictModeUserMethodStub();

        // Assert - Stub inherits from base class (verified at compile time)
        Assert.IsAssignableFrom<StrictModeUserMethodStubBase>(stub);
    }

    [Fact]
    public void BaseClass_HasVirtualMethods_ForInterfaceMembers()
    {
        // Verify that the base class virtual methods exist by calling them indirectly
        var stub = new StrictModeUserMethodStub();
        IStrictModeUserMethodTest service = stub;

        // If GetValue_ and DoSomething_ weren't generated, these would fail
        var result = service.GetValue(5);
        service.DoSomething();

        // The fact that this compiles and runs means the virtual methods exist
        Assert.Equal(50, result); // User override returns x * 10
    }

    [Fact]
    public void BaseClass_VirtualMethodsUseUnderscoreSuffix()
    {
        // This test verifies the naming convention by calling through the interface
        // which internally calls the _-suffixed virtual methods
        var stub = new StrictModeUserMethodStub();
        IStrictModeUserMethodTest service = stub;

        // Act - call through interface
        var result = service.GetValue(7);

        // Assert - user's override (GetValue_) was called, not the interface method name
        Assert.Equal(70, result); // 7 * 10
    }

    [Fact]
    public void BaseClass_NamedCorrectly_StubNamePlusBase()
    {
        // Verify base class naming convention: {ClassName}Base
        var stub = new StrictModeUserMethodStub();

        // Assert - inheritance chain is correct
        Assert.IsAssignableFrom<StrictModeUserMethodStubBase>(stub);

        // The base class type name should be "{ClassName}Base"
        var baseType = stub.GetType().BaseType;
        Assert.NotNull(baseType);
        Assert.Equal("StrictModeUserMethodStubBase", baseType.Name);
    }

    [Fact]
    public void BaseClass_StubAlsoImplementsIKnockOffStub()
    {
        // Verify that adding base class doesn't break IKnockOffStub implementation
        var stub = new StrictModeUserMethodStub();

        // Assert - stub implements IKnockOffStub for Strict mode support
        Assert.IsAssignableFrom<IKnockOffStub>(stub);
    }

    #endregion

    #region 2. UserOverrideDetectionTests - Verify syntactic detection works

    [Fact]
    public void UserOverride_DetectedByOverrideKeyword_CallsUserMethod()
    {
        // Arrange - StrictModeUserMethodStub has override methods
        var stub = new StrictModeUserMethodStub();
        IStrictModeUserMethodTest service = stub;

        // Act - call method that has user override
        var result = service.GetValue(3);

        // Assert - user override was called (x * 10)
        Assert.Equal(30, result);
    }

    [Fact]
    public void UserOverride_VoidMethod_DetectedAndCalled()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        IStrictModeUserMethodTest service = stub;

        // Act - should not throw because override exists
        service.DoSomething();

        // Assert - method was tracked
        stub.DoSomething.Verify(Times.Once);
    }

    [Fact]
    public void UserOverride_MultiParameter_DetectedAndCalled()
    {
        // Arrange
        var stub = new MultiParamUserMethodStub();
        IMultiParamUserMethodService service = stub;

        // Act
        var result = service.Calculate(10, 5);

        // Assert - user override adds parameters
        Assert.Equal(15, result);
    }

    [Fact]
    public async Task UserOverride_AsyncMethod_DetectedAndCalled()
    {
        // Arrange
        var stub = new AsyncUserMethodTestStub();
        IAsyncUserMethodTestService service = stub;

        // Act
        var result = await service.ProcessAsync("test");

        // Assert - user override was called
        Assert.Equal("[Async: test]", result);
    }

    #endregion

    #region 3. UserOverrideFallbackTests - Verify behavior when no override provided

    [Fact]
    public void NoUserOverride_WithoutOnCall_ReturnsDefault()
    {
        // Arrange - SampleKnockOff has legacy user method for GetValue but not DoSomething
        var stub = new NoOverrideStub();
        INoOverrideService service = stub;

        // Act - call method without user override or OnCall
        var result = service.GetValue(5);

        // Assert - returns default (0 for int)
        Assert.Equal(0, result);
    }

    [Fact]
    public void NoUserOverride_WithOnCall_CallsOnCall()
    {
        // Arrange
        var stub = new NoOverrideStub();
        stub.GetValue.Returns(x => x * 3);
        INoOverrideService service = stub;

        // Act
        var result = service.GetValue(5);

        // Assert - OnCall was used
        Assert.Equal(15, result);
    }

    [Fact]
    public void NoUserOverride_StrictMode_Throws()
    {
        // Arrange
        var stub = new NoOverrideStub().Strict();
        INoOverrideService service = stub;

        // Act & Assert
        Assert.Throws<StubException>(() => service.GetValue(5));
    }

    #endregion

    #region 4. OnCallSupersedesOverrideTests - Verify OnCall takes priority

    [Fact]
    public void OnCall_SupersedesUserOverride_NonVoid()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Returns(x => x * 100); // Override user method (which does x * 10)

        // Act
        IStrictModeUserMethodTest service = stub;
        var result = service.GetValue(5);

        // Assert - OnCall wins over user override
        Assert.Equal(500, result); // 5 * 100, not 5 * 10
    }

    [Fact]
    public void OnCall_SupersedesUserOverride_Void()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        var callbackInvoked = false;
        stub.DoSomething.Execute(() => callbackInvoked = true);

        // Act
        IStrictModeUserMethodTest service = stub;
        service.DoSomething();

        // Assert - OnCall was invoked, not user override
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void Returns_SupersedesUserOverride()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Returns(999);

        // Act
        IStrictModeUserMethodTest service = stub;
        var result = service.GetValue(5);

        // Assert - Returns wins over user override
        Assert.Equal(999, result);
    }

    [Fact]
    public void Reset_PreservesOnCallConfiguration_PerDesign()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Returns(x => x * 100);
        IStrictModeUserMethodTest service = stub;

        // Act - first call uses OnCall
        var result1 = service.GetValue(5);
        Assert.Equal(500, result1);

        // Reset clears tracking but preserves OnCall configuration per design
        stub.GetValue.Reset();

        // Assert - OnCall is still active after Reset
        var result2 = service.GetValue(5);
        Assert.Equal(500, result2); // Still uses OnCall, not user override
    }

    [Fact]
    public void UserOverride_StillCalledWhenNoOnCall()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        // No OnCall configured
        IStrictModeUserMethodTest service = stub;

        // Act
        var result = service.GetValue(5);

        // Assert - user override was called
        Assert.Equal(50, result);
    }

    [Fact]
    public void InterceptorNames_AreClean_NoTwoSuffix()
    {
        // The key benefit of the base class pattern: interceptor names are clean
        // Before: stub.GetValue2 (with '2' suffix to avoid collision with user method)
        // After: stub.GetValue (clean name - no suffix needed)
        var stub = new StrictModeUserMethodStub();

        // Assert - interceptors use clean names (compile-time verification)
        // If this compiles, the names are correct
        var getValueInterceptor = stub.GetValue;
        var doSomethingInterceptor = stub.DoSomething;

        Assert.NotNull(getValueInterceptor);
        Assert.NotNull(doSomethingInterceptor);
    }

    [Fact]
    public void Tracking_WorksWithUserOverride()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        IStrictModeUserMethodTest service = stub;

        // Act - call methods with user overrides
        service.GetValue(1);
        service.GetValue(2);
        service.GetValue(3);
        service.DoSomething();

        // Assert - tracking still works even with user overrides
        stub.GetValue.Verify(Times.Exactly(3));
        stub.DoSomething.Verify(Times.Once);
        Assert.Equal(3, stub.GetValue.LastArg);
    }

    [Fact]
    public void Verifiable_WorksWithUserOverride()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Verifiable();
        IStrictModeUserMethodTest service = stub;

        // Act - don't call the method

        // Assert - Verifiable() works with user overrides
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    #endregion

    #region 5. GenericStubBaseClassTests - Verify type parameters propagate

    [Fact]
    public void GenericStub_BaseClassHasTypeParameters()
    {
        // Arrange & Act - If this compiles, type parameters were propagated
        var userStub = new GenericRepositoryStub<User>();
        var orderStub = new GenericRepositoryStub<Order>();

        // Assert - both are valid
        Assert.NotNull(userStub);
        Assert.NotNull(orderStub);
    }

    [Fact]
    public void GenericStub_MethodsUseTypeParameter()
    {
        // Arrange
        var stub = new GenericRepositoryStub<User>();
        IGenericRepository<User> repo = stub;
        var user = new User { Id = 42, Name = "Test" };
        stub.Save.Execute(u => { });
        stub.GetById.Returns(id => user);

        // Act
        repo.Save(user);
        var retrieved = repo.GetById(42);

        // Assert
        Assert.Same(user, retrieved);
    }

    [Fact]
    public void GenericStub_MultipleTypeParameters_AllPropagate()
    {
        // Arrange
        var stub = new GenericKeyValueStoreStub<string, int>();
        IGenericKeyValueStore<string, int> store = stub;
        stub.Get.Returns(key => 42);

        // Act
        var result = store.Get("answer");

        // Assert
        Assert.Equal(42, result);
    }

    #endregion

    #region 6. ConstraintPreservationTests - Verify where constraints

    [Fact]
    public void Constraint_ClassConstraint_Preserved()
    {
        // Arrange - ConstrainedRepositoryStub<T> has where T : class
        var stub = new ConstrainedRepositoryStub<User>();

        // Act - use the stub
        IConstrainedRepository<User> repo = stub;

        // Assert - null can be returned (class constraint allows null)
        var result = repo.GetById(999);
        Assert.Null(result);
    }

    [Fact]
    public void Constraint_MultipleConstraints_Preserved()
    {
        // Arrange - ConstrainedGenericStub has where T : class, IComparable
        var stub = new ConstrainedGenericStub<ComparableEntity>();
        IConstrainedGenericService<ComparableEntity> service = stub;

        // Act
        var entity = new ComparableEntity { Name = "Test" };
        stub.Process.Returns(e => e);
        var result = service.Process(entity);

        // Assert
        Assert.Same(entity, result);
    }

    [Fact]
    public void GenericStub_InheritsFromGenericBaseClass()
    {
        // Verify that generic stubs inherit from generic base class
        var stub = new GenericRepositoryStub<User>();

        // Assert - base type should be GenericRepositoryStubBase<User>
        var baseType = stub.GetType().BaseType;
        Assert.NotNull(baseType);
        Assert.True(baseType.IsGenericType);
        Assert.Equal("GenericRepositoryStubBase`1", baseType.Name);
    }

    #endregion

    #region 7. OverloadedUserMethodTests - Verify overloads work with user overrides

    [Fact]
    public void Overload_UserOverride_OnSomeOverloads_Works()
    {
        // Arrange - stub with user override on only one overload
        var stub = new OverloadedUserMethodStub();
        IOverloadedUserMethodService service = stub;

        // Act - call overload WITH user override
        var result1 = service.Format("hello");

        // Assert - user override was called (adds "USER:")
        Assert.Equal("USER:hello", result1);
    }

    [Fact]
    public void Overload_NoUserOverride_ThrowsWithoutOnCall()
    {
        // Arrange - stub with user override on only one overload
        // After the fix, the non-overridden overload uses the regular interceptor path
        // which throws InvalidOperationException when no OnCall is configured
        var stub = new OverloadedUserMethodStub();
        IOverloadedUserMethodService service = stub;

        // Act & Assert - non-overridden overload throws because no OnCall configured
        // This is the expected behavior for regular method interceptors
        Assert.Throws<InvalidOperationException>(() => service.Format("hello", true));
    }

    [Fact]
    public void Overload_OnCall_SupersedesUserOverride()
    {
        // Arrange
        var stub = new OverloadedUserMethodStub();
        // User override overload uses Format (single-overload interceptor)
        stub.Format.Returns(input => "ONCALL:" + input);
        IOverloadedUserMethodService service = stub;

        // Act
        var result = service.Format("hello");

        // Assert - OnCall wins over user override
        Assert.Equal("ONCALL:hello", result);
    }

    [Fact]
    public void Overload_OnCall_OnNonOverriddenOverload_Works()
    {
        // Arrange
        var stub = new OverloadedUserMethodStub();
        // Non-overridden overload uses Format2 (separate interceptor after fix)
        stub.Format2.Returns((input, upper) => upper ? input.ToUpper() : input);
        IOverloadedUserMethodService service = stub;

        // Act
        var result = service.Format("hello", true);

        // Assert - OnCall was used for overload without user override
        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void Overload_MixedConfiguration_EachOverloadIndependent()
    {
        // Arrange - one overload uses user override, another uses OnCall
        var stub = new OverloadedUserMethodStub();
        // Each overload now has its own interceptor (Format for overridden, Format2 for non-overridden)
        stub.Format2.Returns((input, upper) => "ONCALL:" + (upper ? input.ToUpper() : input));
        IOverloadedUserMethodService service = stub;

        // Act
        var result1 = service.Format("hello");       // Uses user override
        var result2 = service.Format("world", true); // Uses OnCall

        // Assert
        Assert.Equal("USER:hello", result1);          // User override
        Assert.Equal("ONCALL:WORLD", result2);        // OnCall
    }

    [Fact]
    public void Overload_NoUserOverride_StrictModeThrows()
    {
        // Arrange - strict mode with non-overridden overload
        // This verifies the fix: before the fix, strict mode did NOT throw
        // because HasUserOverride was incorrectly true for ALL overloads
        var stub = new OverloadedUserMethodStub().Strict();
        IOverloadedUserMethodService service = stub;

        // Act & Assert - non-overridden overload should throw in strict mode
        // The first overload has user override, so it should NOT throw
        var result = service.Format("hello");
        Assert.Equal("USER:hello", result);

        // The second overload does NOT have user override, so it SHOULD throw
        Assert.Throws<StubException>(() => service.Format("hello", true));
    }

    #endregion
}

#region Supporting Test Types

/// <summary>Interface without any user method overrides.</summary>
public interface INoOverrideService
{
    int GetValue(int x);
    void DoSomething();
}

/// <summary>Stub without any user overrides - all methods use interceptor.</summary>
[KnockOff]
public partial class NoOverrideStub : INoOverrideService
{
}

/// <summary>Interface for testing constraints.</summary>
public interface IConstrainedGenericService<T> where T : class, IComparable
{
    T Process(T item);
}

/// <summary>Stub that preserves constraints.</summary>
[KnockOff]
public partial class ConstrainedGenericStub<T> : IConstrainedGenericService<T> where T : class, IComparable
{
}

/// <summary>Entity that implements IComparable for constraint testing.</summary>
public class ComparableEntity : IComparable
{
    public string Name { get; set; } = "";

    public int CompareTo(object? obj)
    {
        if (obj is ComparableEntity other)
            return string.Compare(Name, other.Name, System.StringComparison.Ordinal);
        return 1;
    }
}

/// <summary>Simple order class for generic tests.</summary>
public class Order
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
}

/// <summary>Interface with overloaded methods for user override tests.</summary>
public interface IOverloadedUserMethodService
{
    string Format(string input);
    string Format(string input, bool uppercase);
}

/// <summary>Stub with user override on only ONE overload to test independence.</summary>
[KnockOff]
public partial class OverloadedUserMethodStub : IOverloadedUserMethodService
{
}

public partial class OverloadedUserMethodStub
{
    // User override for the first overload only
    protected override string Format_(string input)
    {
        return "USER:" + input;
    }

    // NO override for Format_(string input, bool uppercase)
    // That overload uses the interceptor path
}

#endregion
