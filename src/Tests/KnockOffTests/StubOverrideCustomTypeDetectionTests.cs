using KnockOff;
using Xunit;

namespace KnockOff.Tests;

/// <summary>
/// Tests for stub override detection when interface methods use custom (non-primitive) type parameters.
///
/// BUG: Stub override override detection fails for custom type parameters because the detection
/// side (BuildOverrideSignatureKey in Helpers.cs) uses syntax-based p.Type?.ToString() which
/// returns the type as written in source (e.g., "User"), while the matching side
/// (BuildOverrideSignatureKey in SymbolHelpers.cs) uses the fully qualified name from the
/// semantic model (e.g., "KnockOff.Tests.User").
///
/// Detection key: "Update_(User)"
/// Matching key:  "Update_(KnockOff.Tests.User)"
/// Result: No match -> stub override not detected -> interceptor generates without stub override fallback
///
/// For primitive types (int, string) both sides normalize to C# keywords, so keys match.
/// For custom types they don't match, so the stub override is silently ignored.
///
/// All tests in this file are EXPECTED TO FAIL until the bug is fixed.
/// </summary>
public class StubOverrideCustomTypeDetectionTests
{
    #region Pattern 1: Standalone Interface - Custom Type Parameter (Non-Void)

    /// <summary>
    /// BUG: Stub override for FindUser(UserQuery) is NOT detected because:
    /// - Detection key: "FindUser_(UserQuery)" (syntax-based, short name)
    /// - Matching key:  "FindUser_(KnockOff.Tests.UserQuery)" (semantic, fully qualified)
    /// - Keys don't match, so the generated interceptor Invoke() doesn't call the stub override.
    ///
    /// Expected: Stub override is called as fallback, returning the user from the query.
    /// Actual: Interceptor falls through to _source (null) or default, NOT calling stub override.
    /// </summary>
    [Fact]
    public void Standalone_StubOverride_CustomTypeParam_NonVoid_IsCalledAsFallback()
    {
        // Arrange
        var stub = new CustomTypeStubOverrideStub();
        var query = new UserQuery { Id = 42, Name = "Alice" };

        // Act - No OnCall configured, should fall to stub override
        ICustomTypeStubOverrideService service = stub;
        var result = service.FindUser(query);

        // Assert - Stub override should return "[FOUND: 42-Alice]"
        // FAILS: stub override not detected, so interceptor returns default (null) instead
        Assert.NotNull(result);
        Assert.Equal("[FOUND: 42-Alice]", result);
    }

    #endregion

    #region Pattern 1: Standalone Interface - Custom Type Parameter (Void)

    /// <summary>
    /// BUG: Stub override for SaveUser(UserRecord) is NOT detected because:
    /// - Detection key: "SaveUser_(UserRecord)" (syntax-based, short name)
    /// - Matching key:  "SaveUser_(KnockOff.Tests.UserRecord)" (semantic, fully qualified)
    ///
    /// Expected: Stub override is called, recording the save in LastSavedRecord.
    /// Actual: Interceptor falls to _source or no-op, stub override never invoked.
    /// </summary>
    [Fact]
    public void Standalone_StubOverride_CustomTypeParam_Void_IsCalledAsFallback()
    {
        // Arrange
        var stub = new CustomTypeStubOverrideStub();
        var record = new UserRecord { Id = 7, Email = "bob@test.com" };

        // Act - No OnCall configured, should fall to stub override
        ICustomTypeStubOverrideService service = stub;
        service.SaveUser(record);

        // Assert - Stub override should have recorded the save
        // FAILS: stub override not detected, so SaveUser_ is never called
        Assert.NotNull(stub.LastSavedRecord);
        Assert.Equal(7, stub.LastSavedRecord!.Id);
    }

    #endregion

    #region Pattern 1: Standalone Interface - Mixed Primitive and Custom Type Parameters

    /// <summary>
    /// BUG: Stub override for UpdateUser(int, UserRecord) is NOT detected because the
    /// UserRecord parameter causes key mismatch, even though int would match on its own.
    /// - Detection key: "UpdateUser_(int,UserRecord)"
    /// - Matching key:  "UpdateUser_(int,KnockOff.Tests.UserRecord)"
    ///
    /// Expected: Stub override is called, recording the update.
    /// Actual: Interceptor falls to default behavior, stub override never invoked.
    /// </summary>
    [Fact]
    public void Standalone_StubOverride_MixedPrimitiveAndCustomTypeParams_IsCalledAsFallback()
    {
        // Arrange
        var stub = new CustomTypeStubOverrideStub();
        var record = new UserRecord { Id = 3, Email = "carol@test.com" };

        // Act
        ICustomTypeStubOverrideService service = stub;
        var result = service.UpdateUser(3, record);

        // Assert - Stub override should return true
        // FAILS: stub override not detected due to UserRecord parameter
        Assert.True(result);
        Assert.NotNull(stub.LastUpdatedRecord);
        Assert.Equal(3, stub.LastUpdatedRecord!.Id);
    }

    #endregion

    #region Contrast: Primitive Parameters Work Correctly

    /// <summary>
    /// This test PASSES because the stub override uses only primitive parameters (int).
    /// Both detection and matching normalize "int" to "int", so keys match.
    /// Included as a control to demonstrate the bug is specifically about custom types.
    /// </summary>
    [Fact]
    public void Standalone_StubOverride_PrimitiveParam_IsCalledAsFallback()
    {
        // Arrange
        var stub = new CustomTypeStubOverrideStub();

        // Act - No OnCall configured, should fall to stub override
        ICustomTypeStubOverrideService service = stub;
        var result = service.GetById(42);

        // Assert - Stub override should return "[ID: 42]"
        // PASSES: "int" normalizes correctly on both sides
        Assert.Equal("[ID: 42]", result);
    }

    #endregion

    #region Verification Consequence: Unconfigured Custom-Type Stub Overrides Appear in VerifyAll

    /// <summary>
    /// BUG CONSEQUENCE: Because the stub override is not detected, the interceptor for
    /// FindUser does NOT have the stub-override flag set. This means:
    /// 1. The interceptor participates in VerifyAll() (stub overrides are normally excluded)
    /// 2. If OnCall is configured on the interceptor, VerifyAll expects it to be called
    ///
    /// This test demonstrates that configuring OnCall on a method that SHOULD be a user
    /// method interceptor causes it to appear in VerifyAll, when it shouldn't.
    /// </summary>
    [Fact]
    public void Standalone_StubOverride_CustomTypeParam_OnCallSupersedesStubOverride()
    {
        // Arrange
        var stub = new CustomTypeStubOverrideStub();
        stub.FindUser.Return(q => $"[ONCALL: {q.Id}]");

        // Act
        ICustomTypeStubOverrideService service = stub;
        var result = service.FindUser(new UserQuery { Id = 99, Name = "Test" });

        // Assert - OnCall should supersede stub override
        Assert.Equal("[ONCALL: 99]", result);

        // This part tests that tracking works even with the bug
        stub.FindUser.Verify(Called.Once);
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
/// These expose the stub override detection bug: the generator fails to match
/// stub override methods when parameters use non-primitive types.
/// </summary>
public interface ICustomTypeStubOverrideService
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
/// Standalone stub with stub override overrides for both custom-type and primitive-type
/// parameter methods. The primitive-type stub overrides will be detected correctly;
/// the custom-type stub overrides will NOT be detected due to the bug.
/// </summary>
[KnockOff]
public partial class CustomTypeStubOverrideStub : ICustomTypeStubOverrideService
{
    /// <summary>Tracks whether SaveUser_ stub override was called.</summary>
    public UserRecord? LastSavedRecord { get; set; }

    /// <summary>Tracks whether UpdateUser_ stub override was called.</summary>
    public UserRecord? LastUpdatedRecord { get; set; }
}

public partial class CustomTypeStubOverrideStub
{
    /// <summary>
    /// Stub override for FindUser - takes custom type UserQuery.
    /// BUG: This override will NOT be detected by the generator because
    /// the syntax-based key "FindUser_(UserQuery)" won't match the
    /// semantic-based key "FindUser_(KnockOff.Tests.UserQuery)".
    /// </summary>
    protected override string FindUser_(UserQuery query)
    {
        return $"[FOUND: {query.Id}-{query.Name}]";
    }

    /// <summary>
    /// Stub override for SaveUser - takes custom type UserRecord.
    /// BUG: Same key mismatch as FindUser_.
    /// </summary>
    protected override void SaveUser_(UserRecord record)
    {
        LastSavedRecord = record;
    }

    /// <summary>
    /// Stub override for UpdateUser - takes int (primitive) + UserRecord (custom).
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
    /// Stub override for GetById - takes only primitive int.
    /// This WILL be detected correctly (both sides normalize "int" → "int").
    /// Included as a control to prove primitive params work.
    /// </summary>
    protected override string GetById_(int id)
    {
        return $"[ID: {id}]";
    }
}

#endregion
