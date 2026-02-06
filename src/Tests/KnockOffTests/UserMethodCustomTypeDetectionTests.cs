using KnockOff;
using Xunit;

namespace KnockOff.Tests;

/// <summary>
/// Tests for user method detection when interface methods use custom (non-primitive) type parameters.
///
/// BUG: User method override detection fails for custom type parameters because the detection
/// side (BuildOverrideSignatureKey in Helpers.cs) uses syntax-based p.Type?.ToString() which
/// returns the type as written in source (e.g., "User"), while the matching side
/// (BuildOverrideSignatureKey in SymbolHelpers.cs) uses the fully qualified name from the
/// semantic model (e.g., "KnockOff.Tests.User").
///
/// Detection key: "Update_(User)"
/// Matching key:  "Update_(KnockOff.Tests.User)"
/// Result: No match -> user method not detected -> interceptor generates without user method fallback
///
/// For primitive types (int, string) both sides normalize to C# keywords, so keys match.
/// For custom types they don't match, so the user method is silently ignored.
///
/// All tests in this file are EXPECTED TO FAIL until the bug is fixed.
/// </summary>
public class UserMethodCustomTypeDetectionTests
{
    #region Pattern 1: Standalone Interface - Custom Type Parameter (Non-Void)

    /// <summary>
    /// BUG: User method for FindUser(UserQuery) is NOT detected because:
    /// - Detection key: "FindUser_(UserQuery)" (syntax-based, short name)
    /// - Matching key:  "FindUser_(KnockOff.Tests.UserQuery)" (semantic, fully qualified)
    /// - Keys don't match, so the generated interceptor Invoke() doesn't call the user method.
    ///
    /// Expected: User method is called as fallback, returning the user from the query.
    /// Actual: Interceptor falls through to _source (null) or default, NOT calling user method.
    /// </summary>
    [Fact]
    public void Standalone_UserMethod_CustomTypeParam_NonVoid_IsCalledAsFallback()
    {
        // Arrange
        var stub = new CustomTypeUserMethodStub();
        var query = new UserQuery { Id = 42, Name = "Alice" };

        // Act - No OnCall configured, should fall to user method
        ICustomTypeUserMethodService service = stub;
        var result = service.FindUser(query);

        // Assert - User method should return "[FOUND: 42-Alice]"
        // FAILS: user method not detected, so interceptor returns default (null) instead
        Assert.NotNull(result);
        Assert.Equal("[FOUND: 42-Alice]", result);
    }

    #endregion

    #region Pattern 1: Standalone Interface - Custom Type Parameter (Void)

    /// <summary>
    /// BUG: User method for SaveUser(UserRecord) is NOT detected because:
    /// - Detection key: "SaveUser_(UserRecord)" (syntax-based, short name)
    /// - Matching key:  "SaveUser_(KnockOff.Tests.UserRecord)" (semantic, fully qualified)
    ///
    /// Expected: User method is called, recording the save in LastSavedRecord.
    /// Actual: Interceptor falls to _source or no-op, user method never invoked.
    /// </summary>
    [Fact]
    public void Standalone_UserMethod_CustomTypeParam_Void_IsCalledAsFallback()
    {
        // Arrange
        var stub = new CustomTypeUserMethodStub();
        var record = new UserRecord { Id = 7, Email = "bob@test.com" };

        // Act - No OnCall configured, should fall to user method
        ICustomTypeUserMethodService service = stub;
        service.SaveUser(record);

        // Assert - User method should have recorded the save
        // FAILS: user method not detected, so SaveUser_ is never called
        Assert.NotNull(stub.LastSavedRecord);
        Assert.Equal(7, stub.LastSavedRecord!.Id);
    }

    #endregion

    #region Pattern 1: Standalone Interface - Mixed Primitive and Custom Type Parameters

    /// <summary>
    /// BUG: User method for UpdateUser(int, UserRecord) is NOT detected because the
    /// UserRecord parameter causes key mismatch, even though int would match on its own.
    /// - Detection key: "UpdateUser_(int,UserRecord)"
    /// - Matching key:  "UpdateUser_(int,KnockOff.Tests.UserRecord)"
    ///
    /// Expected: User method is called, recording the update.
    /// Actual: Interceptor falls to default behavior, user method never invoked.
    /// </summary>
    [Fact]
    public void Standalone_UserMethod_MixedPrimitiveAndCustomTypeParams_IsCalledAsFallback()
    {
        // Arrange
        var stub = new CustomTypeUserMethodStub();
        var record = new UserRecord { Id = 3, Email = "carol@test.com" };

        // Act
        ICustomTypeUserMethodService service = stub;
        var result = service.UpdateUser(3, record);

        // Assert - User method should return true
        // FAILS: user method not detected due to UserRecord parameter
        Assert.True(result);
        Assert.NotNull(stub.LastUpdatedRecord);
        Assert.Equal(3, stub.LastUpdatedRecord!.Id);
    }

    #endregion

    #region Contrast: Primitive Parameters Work Correctly

    /// <summary>
    /// This test PASSES because the user method uses only primitive parameters (int).
    /// Both detection and matching normalize "int" to "int", so keys match.
    /// Included as a control to demonstrate the bug is specifically about custom types.
    /// </summary>
    [Fact]
    public void Standalone_UserMethod_PrimitiveParam_IsCalledAsFallback()
    {
        // Arrange
        var stub = new CustomTypeUserMethodStub();

        // Act - No OnCall configured, should fall to user method
        ICustomTypeUserMethodService service = stub;
        var result = service.GetById(42);

        // Assert - User method should return "[ID: 42]"
        // PASSES: "int" normalizes correctly on both sides
        Assert.Equal("[ID: 42]", result);
    }

    #endregion

    #region Verification Consequence: Unconfigured Custom-Type User Methods Appear in VerifyAll

    /// <summary>
    /// BUG CONSEQUENCE: Because the user method is not detected, the interceptor for
    /// FindUser does NOT have the user-method flag set. This means:
    /// 1. The interceptor participates in VerifyAll() (user methods are normally excluded)
    /// 2. If OnCall is configured on the interceptor, VerifyAll expects it to be called
    ///
    /// This test demonstrates that configuring OnCall on a method that SHOULD be a user
    /// method interceptor causes it to appear in VerifyAll, when it shouldn't.
    /// </summary>
    [Fact]
    public void Standalone_UserMethod_CustomTypeParam_OnCallSupersedesUserMethod()
    {
        // Arrange
        var stub = new CustomTypeUserMethodStub();
        stub.FindUser.OnCall(q => $"[ONCALL: {q.Id}]");

        // Act
        ICustomTypeUserMethodService service = stub;
        var result = service.FindUser(new UserQuery { Id = 99, Name = "Test" });

        // Assert - OnCall should supersede user method
        Assert.Equal("[ONCALL: 99]", result);

        // This part tests that tracking works even with the bug
        stub.FindUser.Verify(Times.Once);
    }

    #endregion
}

#region Custom Types for Testing

/// <summary>
/// Custom type used as a method parameter. This is NOT a primitive type,
/// so it triggers the signature key mismatch bug.
/// </summary>
public class UserQuery
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>
/// Another custom type used as a method parameter.
/// </summary>
public class UserRecord
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
}

#endregion

#region Interface and Stub Definitions

/// <summary>
/// Interface with methods that take custom type parameters.
/// These expose the user method detection bug: the generator fails to match
/// user override methods when parameters use non-primitive types.
/// </summary>
public interface ICustomTypeUserMethodService
{
    /// <summary>Method with custom type parameter and non-void return.</summary>
    string FindUser(UserQuery query);

    /// <summary>Method with custom type parameter and void return.</summary>
    void SaveUser(UserRecord record);

    /// <summary>Method with both primitive and custom type parameters.</summary>
    bool UpdateUser(int id, UserRecord record);

    /// <summary>Control method with only primitive parameter (should work correctly).</summary>
    string GetById(int id);
}

/// <summary>
/// Standalone stub with user method overrides for both custom-type and primitive-type
/// parameter methods. The primitive-type user methods will be detected correctly;
/// the custom-type user methods will NOT be detected due to the bug.
/// </summary>
[KnockOff]
public partial class CustomTypeUserMethodStub : ICustomTypeUserMethodService
{
    /// <summary>Tracks whether SaveUser_ user method was called.</summary>
    public UserRecord? LastSavedRecord { get; set; }

    /// <summary>Tracks whether UpdateUser_ user method was called.</summary>
    public UserRecord? LastUpdatedRecord { get; set; }
}

public partial class CustomTypeUserMethodStub
{
    /// <summary>
    /// User method for FindUser - takes custom type UserQuery.
    /// BUG: This override will NOT be detected by the generator because
    /// the syntax-based key "FindUser_(UserQuery)" won't match the
    /// semantic-based key "FindUser_(KnockOff.Tests.UserQuery)".
    /// </summary>
    protected override string FindUser_(UserQuery query)
    {
        return $"[FOUND: {query.Id}-{query.Name}]";
    }

    /// <summary>
    /// User method for SaveUser - takes custom type UserRecord.
    /// BUG: Same key mismatch as FindUser_.
    /// </summary>
    protected override void SaveUser_(UserRecord record)
    {
        LastSavedRecord = record;
    }

    /// <summary>
    /// User method for UpdateUser - takes int (primitive) + UserRecord (custom).
    /// BUG: Even though int matches, UserRecord causes the whole key to mismatch.
    /// Detection key: "UpdateUser_(int,UserRecord)"
    /// Matching key:  "UpdateUser_(int,KnockOff.Tests.UserRecord)"
    /// </summary>
    protected override bool UpdateUser_(int id, UserRecord record)
    {
        LastUpdatedRecord = record;
        return true;
    }

    /// <summary>
    /// User method for GetById - takes only primitive int.
    /// This WILL be detected correctly (both sides normalize "int" → "int").
    /// Included as a control to prove primitive params work.
    /// </summary>
    protected override string GetById_(int id)
    {
        return $"[ID: {id}]";
    }
}

#endregion
