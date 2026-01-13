namespace KnockOff.Documentation.Samples.Design;

// =============================================================================
// ADVANCED FEATURES: INDEXERS, INIT PROPERTIES, REQUIRED PROPERTIES
// =============================================================================

// -----------------------------------------------------------------------------
// STUB DEFINITIONS
// -----------------------------------------------------------------------------

#region docs:design:indexer-stub-definition
[KnockOff<IDsCache>]
[KnockOff<IDsMultiIndexer>]
public partial class DsIndexerTests { }
#endregion

#region docs:design:init-property-stub-definition
[KnockOff<IDsEntity>]
[KnockOff<IDsDocument>]
[KnockOff<DsEntityBase>]
public partial class DsInitPropertyTests { }
#endregion

#region docs:design:required-property-stub-definition
[KnockOff<DsAuditableEntity>]
[KnockOff<DsImmutableEntity>]
public partial class DsRequiredPropertyTests { }
#endregion

// -----------------------------------------------------------------------------
// USAGE EXAMPLES
// -----------------------------------------------------------------------------

public static class DesignAdvancedSamples
{
    // =========================================================================
    // INDEXERS
    // =========================================================================

    #region docs:design:indexer-basic-usage
    public static void IndexerBasicUsage()
    {
        var stub = new DsIndexerTests.Stubs.IDsCache();
        IDsCache cache = stub.Object;

        // Pre-populate backing dictionary
        stub.Indexer.Backing["user:1"] = "Alice";
        stub.Indexer.Backing["user:2"] = "Bob";

        // Access through interface
        var user1 = cache["user:1"];  // Returns "Alice"
        cache["user:3"] = "Charlie";  // Adds to backing

        // Verify access
        // stub.Indexer.GetCount == 1
        // stub.Indexer.SetCount == 1
        // stub.Indexer.LastGetKey == "user:1"
        // stub.Indexer.LastSetEntry == ("user:3", "Charlie")
    }
    #endregion

    #region docs:design:indexer-oncall-usage
    public static void IndexerOnCallUsage()
    {
        var stub = new DsIndexerTests.Stubs.IDsCache();
        IDsCache cache = stub.Object;

        // Custom getter behavior
        stub.Indexer.OnGet = (ko, key) =>
        {
            if (key.StartsWith("user:"))
                return $"User_{key[5..]}";
            return null;
        };

        // Custom setter behavior
        stub.Indexer.OnSet = (ko, key, value) =>
        {
            // Log, validate, or transform before storing
            ko.Indexer.Backing[key.ToLower()] = value?.ToUpper();
        };

        var result = cache["user:42"];  // Returns "User_42"
        cache["Key"] = "value";         // Stores as ["key"] = "VALUE"
    }
    #endregion

    #region docs:design:indexer-multiple-usage
    public static void MultipleIndexersUsage()
    {
        var stub = new DsIndexerTests.Stubs.IDsMultiIndexer();
        IDsMultiIndexer multi = stub.Object;

        // Each indexer has its own interceptor named by key type
        // String-keyed indexer: IndexerString
        // Int-keyed indexer: IndexerInt32
        stub.IndexerString.Backing["name"] = "Alice";
        stub.IndexerInt32.Backing[0] = "First";

        var byName = multi["name"];  // "Alice"
        var byIndex = multi[0];      // "First"

        // Verify separately
        // stub.IndexerString.GetCount == 1
        // stub.IndexerInt32.GetCount == 1
    }
    #endregion

    // =========================================================================
    // INIT PROPERTIES
    // =========================================================================

    #region docs:design:init-property-interface-usage
    public static void InitPropertyInterfaceUsage()
    {
        var stub = new DsInitPropertyTests.Stubs.IDsEntity();
        IDsEntity entity = stub.Object;

        // Configure init properties via interceptor's Value
        stub.Id.Value = "entity-123";
        stub.Name.Value = "Test Entity";

        // Read through interface
        var id = entity.Id;      // "entity-123"
        var name = entity.Name;  // "Test Entity"

        // Verify read access
        // stub.Id.GetCount == 1
        // stub.Name.GetCount == 1
    }
    #endregion

    #region docs:design:init-property-mixed-usage
    public static void InitPropertyMixedUsage()
    {
        var stub = new DsInitPropertyTests.Stubs.IDsDocument();
        IDsDocument doc = stub.Object;

        // Init property: configure via Value
        stub.Id.Value = "doc-456";

        // Regular property: configure via Value or OnGet/OnSet
        stub.Title.Value = "My Document";

        // Get-only property: configure via Value
        stub.Version.Value = 1;

        // Read values
        var id = doc.Id;         // "doc-456" (init - immutable)
        doc.Title = "New Title"; // Works (regular setter)
        var ver = doc.Version;   // 1 (get-only)

        // Init properties track reads, not writes (writes via stub.X.Value)
        // stub.Id.GetCount == 1
        // stub.Title.SetCount == 1
    }
    #endregion

    #region docs:design:init-property-class-usage
    public static void InitPropertyClassUsage()
    {
        var stub = new DsInitPropertyTests.Stubs.DsEntityBase();
        DsEntityBase entity = stub.Object;

        // Class stubs use OnGet for init properties (no direct Value)
        stub.Id.OnGet = _ => "base-entity-789";
        stub.CreatedBy.OnGet = _ => "system";

        // Access triggers OnGet callback
        var id = entity.Id;           // "base-entity-789"
        var createdBy = entity.CreatedBy;  // "system"

        // Verify access
        // stub.Id.GetCount == 1
        // stub.CreatedBy.GetCount == 1
    }
    #endregion

    // =========================================================================
    // REQUIRED PROPERTIES
    // =========================================================================

    #region docs:design:required-property-usage
    public static void RequiredPropertyUsage()
    {
        // Required properties work like regular properties in stubs
        // The [SetsRequiredMembers] attribute is auto-generated on constructors
        var stub = new DsRequiredPropertyTests.Stubs.DsAuditableEntity();
        DsAuditableEntity entity = stub.Object;

        // Configure via OnGet (class stub pattern)
        stub.Id.OnGet = _ => "audit-001";
        stub.CreatedBy.OnGet = _ => "admin";
        stub.CreatedAt.OnGet = _ => new DateTime(2024, 1, 15);

        // Access through object
        var id = entity.Id;              // "audit-001"
        var createdBy = entity.CreatedBy; // "admin"
        var createdAt = entity.CreatedAt; // 2024-01-15

        // Or set through object (required { get; set; } allows mutation)
        entity.Id = "audit-002";
        // stub.Id.SetCount == 1
    }
    #endregion

    #region docs:design:required-init-property-usage
    public static void RequiredInitPropertyUsage()
    {
        // Required + init: must be set at construction, immutable after
        var stub = new DsRequiredPropertyTests.Stubs.DsImmutableEntity();
        DsImmutableEntity entity = stub.Object;

        // Configure via OnGet
        stub.Id.OnGet = _ => "immutable-001";
        stub.Name.OnGet = _ => "Immutable Entity";
        stub.Version.OnGet = _ => 42;

        // Read values
        var id = entity.Id;        // "immutable-001"
        var name = entity.Name;    // "Immutable Entity"
        var version = entity.Version; // 42

        // Version is not required, can be set
        entity.Version = 43;
        // stub.Version.SetCount == 1
    }
    #endregion
}

// =============================================================================
// SUMMARY
// =============================================================================
//
// INDEXERS:
//   - Single indexer: stub.Indexer
//   - Multiple indexers: stub.IndexerString, stub.IndexerInt (by key type)
//   - Backing dictionary: stub.Indexer.Backing[key] = value
//   - Custom behavior: stub.Indexer.OnGet, stub.Indexer.OnSet
//   - Tracking: stub.Indexer.GetCount, stub.Indexer.LastGetKey
//
// INIT PROPERTIES (interface stubs):
//   - Configure via: stub.PropertyName.Value = "value"
//   - Track reads: stub.PropertyName.GetCount
//   - No OnGet/OnSet (value is set directly)
//
// INIT PROPERTIES (class stubs):
//   - Configure via: stub.PropertyName.OnGet = _ => "value"
//   - Track reads: stub.PropertyName.GetCount
//   - Delegates to base if OnGet not set
//
// REQUIRED PROPERTIES:
//   - Work like regular class properties
//   - [SetsRequiredMembers] auto-generated on stub constructors
//   - Configure via OnGet/OnSet, track via GetCount/SetCount
//
// =============================================================================
