namespace KnockOff.Documentation.Samples.Design;

// =============================================================================
// API DESIGN OVERVIEW
// =============================================================================
//
// KnockOff supports three stub patterns. All use the same consistent API:
//   - stub.Member     = interceptor (tracking, callbacks)
//   - stub.Object     = interface/class instance for invoking members
//
// =============================================================================

#region docs:design:inline-interface-definition
/// <summary>
/// Pattern 1: Inline Interface Stub
/// Apply [KnockOff&lt;T&gt;] to generate nested stub class.
/// </summary>
[KnockOff<IDsUserService>]
public partial class DsInlineInterfaceTests { }
#endregion

#region docs:design:inline-class-definition
/// <summary>
/// Pattern 2: Inline Class Stub
/// Apply [KnockOff&lt;T&gt;] with a class to stub virtual members.
/// </summary>
[KnockOff<DsEmailService>]
public partial class DsInlineClassTests { }
#endregion

#region docs:design:standalone-interface-definition
/// <summary>
/// Pattern 3: Stand-Alone Interface Stub
/// Apply [KnockOff] to a partial class that implements the interface.
/// </summary>
[KnockOff]
public partial class DsUserServiceStub : IDsUserService { }
#endregion

// =============================================================================
// USAGE EXAMPLES
// =============================================================================

public static class DesignSamples
{
    #region docs:design:inline-interface-usage
    public static void InlineInterfaceStubUsage()
    {
        // Create stub
        var stub = new DsInlineInterfaceTests.Stubs.IDsUserService();
        IDsUserService service = stub.Object;

        // METHOD: Configure and verify
        stub.GetUser.OnCall((ko, id) => new DsUser { Id = id });
        var user = service.GetUser(42);
        // Verify: stub.GetUser.CallCount, stub.GetUser.LastCallArg

        // PROPERTY: Configure and verify
        stub.Name.Value = "Test";
        var name = service.Name;
        // Verify: stub.Name.GetCount, stub.Name.SetCount

        // INDEXER: Configure and verify
        stub.Indexer.Backing["key"] = "value";
        var val = service["key"];
        // Verify: stub.Indexer.GetCount, stub.Indexer.LastGetKey
    }
    #endregion

    #region docs:design:inline-class-usage
    public static void InlineClassStubUsage()
    {
        // Create stub
        var stub = new DsInlineClassTests.Stubs.DsEmailService();
        DsEmailService service = stub.Object;

        // METHOD: Configure and verify
        stub.Send.OnCall((ko, to, body) => { /* custom behavior */ });
        service.Send("test@example.com", "Hello");
        // Verify: stub.Send.CallCount, stub.Send.LastCallArgs

        // PROPERTY: Configure and verify
        stub.ServerName.OnGet = (ko) => "smtp.test.com";
        var server = service.ServerName;
        // Verify: stub.ServerName.GetCount

        // INDEXER: Configure and verify
        stub.Indexer.Backing[0] = "value";
        var val = service[0];
        // Verify: stub.Indexer.GetCount
    }
    #endregion

    #region docs:design:standalone-interface-usage
    public static void StandaloneInterfaceStubUsage()
    {
        // Create stub
        var stub = new DsUserServiceStub();
        IDsUserService service = stub.Object;

        // METHOD: Configure and verify
        var getUserTracking = stub.GetUser.OnCall((ko, id) => new DsUser { Id = id });
        var user = service.GetUser(42);
        // Verify: getUserTracking.CallCount, getUserTracking.LastArg

        // PROPERTY: Configure and verify
        stub.Name.Value = "Test";
        var name = service.Name;
        // Verify: stub.Name.GetCount, stub.Name.SetCount

        // INDEXER: Configure and verify
        stub.Indexer.Backing["key"] = "value";
        var val = service["key"];
        // Verify: stub.Indexer.GetCount, stub.Indexer.LastGetKey
    }
    #endregion
}

// =============================================================================
// API SUMMARY
// =============================================================================
//
// | Pattern              | Interceptor      | Actual Value         |
// |----------------------|------------------|----------------------|
// | stub.Member          | Yes (always)     |                      |
// | stub.Object.Member   |                  | Yes (always)         |
//
// stub.Object returns:
//   - Inline Interface:    IInterface
//   - Inline Class:        ClassName
//   - Stand-Alone:         IInterface
//
// =============================================================================
