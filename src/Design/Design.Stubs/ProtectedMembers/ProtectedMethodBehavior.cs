// -----------------------------------------------------------------------------
// Design.Stubs - Protected Method Behavior
// -----------------------------------------------------------------------------
// This file documents how KnockOff handles protected virtual/abstract methods
// on class stubs. Protected members apply ONLY to class stubs (Patterns 3, 4,
// 6, 9) — interface stubs have no access modifiers.
//
// GENERATOR BEHAVIOR: The generator treats protected virtual/abstract members
// identically to public ones. The generated Impl class emits
// "protected override" to match the base class access modifier. The stub
// wrapper exposes ALL interceptors (public and protected) as public properties.
//
// KEY INSIGHT: You cannot call protected methods directly from a test. Instead:
// 1. Configure the protected method interceptor on the stub wrapper
// 2. Call a public method that internally calls the protected method
// 3. Verify the result reflects your interceptor configuration
//
// This is the realistic pattern: public methods depend on protected template
// methods, and stubs let you control those template methods from tests.
//
// MEMBER TYPES WITH PROTECTED SUPPORT:
// - Methods (abstract + virtual with base fallback)
// - Properties (get/set)
// - Indexers (get/set with key)
// - Events (subscribe/raise)
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using KnockOff;

namespace Design.Stubs.ProtectedMembers;

// =============================================================================
// INLINE PATTERN: Protected method behavior
// =============================================================================

[KnockOff<ServiceBase>]
public partial class ProtectedMethodBehaviorDemo
{
    // =========================================================================
    // PROTECTED ABSTRACT METHOD: Must configure via interceptor
    // =========================================================================
    // DESIGN DECISION: Protected abstract methods work like public abstract
    // methods. No Return(value) vs Return(callback) distinction — just
    // configure the interceptor the same way.
    //
    // GENERATOR BEHAVIOR (in Impl class):
    //   protected override string GetInternalId()
    //   {
    //       return _stub.GetInternalId.Invoke(_stub.Strict);
    //   }
    //
    // The interceptor is public on the wrapper:
    //   stub.GetInternalId.Return("my-id");
    // =========================================================================

    public void ProtectedAbstractMethod_ConfigureViaReturn()
    {
        var stub = new Stubs.ServiceBase();

        // Configure protected abstract method via public interceptor
        stub.GetInternalId.Return("ID-42");

        // Call public method that depends on the protected method
        ServiceBase service = stub.Object;
        var desc = service.GetDescription();
        // desc contains "ID-42" (from GetInternalId)
    }

    public void ProtectedAbstractMethod_ConfigureViaCallback()
    {
        var stub = new Stubs.ServiceBase();

        // Callback-based configuration works too
        stub.GetInternalId.Return(() => $"ID-{DateTime.Now.Ticks}");

        ServiceBase service = stub.Object;
        var desc = service.GetDescription();
        // desc contains the dynamic ID
    }

    // =========================================================================
    // PROTECTED VIRTUAL METHOD: Unconfigured falls back to base
    // =========================================================================
    // DESIGN DECISION: Protected virtual methods follow the same base fallback
    // rule as public virtual methods. When unconfigured, the Impl class calls
    // the base implementation.
    //
    // GENERATOR BEHAVIOR (in Impl class):
    //   protected override string FormatTag()
    //   {
    //       if (_stub == null) return base.FormatTag();
    //       var unconfiguredBefore = _stub.FormatTag.UnconfiguredCallCount;
    //       var result = _stub.FormatTag.Invoke(_stub.Strict);
    //       if (_stub.FormatTag.UnconfiguredCallCount > unconfiguredBefore)
    //           return base.FormatTag();
    //       return result;
    //   }
    //
    // When configured: returns interceptor result
    // When unconfigured: returns base.FormatTag() which is $"[{Tag}]"
    // =========================================================================

    public void ProtectedVirtualMethod_ConfiguredOverridesBase()
    {
        var stub = new Stubs.ServiceBase();

        // Configure protected virtual method — overrides base implementation
        stub.FormatTag.Return("custom-format");
        stub.GetInternalId.Return("id");

        ServiceBase service = stub.Object;
        var desc = service.GetDescription();
        // desc == "id: custom-format" (NOT "id: []" from base)
    }

    public void ProtectedVirtualMethod_UnconfiguredFallsBackToBase()
    {
        var stub = new Stubs.ServiceBase();

        // Configure the protected property that FormatTag reads
        stub.Tag.Get("myTag");
        stub.GetInternalId.Return("id");

        // Do NOT configure FormatTag — it will call base
        ServiceBase service = stub.Object;
        var desc = service.GetDescription();
        // desc == "id: [myTag]" (base FormatTag returns $"[{Tag}]")
    }

    // =========================================================================
    // PROTECTED PROPERTY: Configure get/set via interceptor
    // =========================================================================
    // DESIGN DECISION: Protected properties work identically to public ones.
    // The stub wrapper's property interceptor is public even though the Impl
    // class override is protected.
    //
    // GENERATOR BEHAVIOR (in Impl class):
    //   protected override string Tag
    //   {
    //       get => _stub.Tag.InvokeGet(_stub.Strict);  // unconfigured: base fallback
    //       set => _stub.Tag.InvokeSet(_stub.Strict, value);
    //   }
    // =========================================================================

    public void ProtectedProperty_ConfigureGet()
    {
        var stub = new Stubs.ServiceBase();

        // Configure protected property getter
        stub.Tag.Get("test-tag");
        stub.GetInternalId.Return("id");

        // FormatTag (unconfigured) calls base which reads Tag
        ServiceBase service = stub.Object;
        var desc = service.GetDescription();
        // desc == "id: [test-tag]"
    }

    // =========================================================================
    // PROTECTED INDEXER: Configure get/set via interceptor
    // =========================================================================
    // DESIGN DECISION: Protected indexers work like public indexers.
    // Get(callback), Set(callback), Backing dictionary all available.
    //
    // ServiceBase has: protected virtual string this[int index]
    // Stub wrapper has: stub.Indexer (public indexer interceptor)
    // =========================================================================

    public void ProtectedIndexer_ConfigureGetCallback()
    {
        var stub = new Stubs.ServiceBase();

        // Configure protected indexer via callback
        stub.Indexer.Get((index) => $"item-{index}");
        stub.Name.Get("test");
        stub.GetInternalId.Return("id");

        ServiceBase service = stub.Object;
        var item = service.GetItemAt(3);
        // item == "item-3"
    }

    public void ProtectedIndexer_UseBacking()
    {
        var stub = new Stubs.ServiceBase();

        // Use backing dictionary for protected indexer
        stub.Indexer.Backing[0] = "first";
        stub.Indexer.Backing[1] = "second";
        stub.Name.Get("test");
        stub.GetInternalId.Return("id");

        ServiceBase service = stub.Object;
        var item = service.GetItemAt(0);
        // item == "first"
    }

    // =========================================================================
    // PROTECTED EVENT: Subscribe and raise via interceptor
    // =========================================================================
    // DESIGN DECISION: Protected events are accessible via the stub wrapper's
    // event interceptor. You can check HasSubscribers and Raise from tests.
    //
    // GENERATOR BEHAVIOR (in Impl class):
    //   protected override event EventHandler? InternalStateChanged
    //   {
    //       add => _stub?.InternalStateChanged.RecordAdd(value);
    //       remove => _stub?.InternalStateChanged.RecordRemove(value);
    //   }
    // =========================================================================

    public void ProtectedEvent_RaiseViaInterceptor()
    {
        var stub = new Stubs.ServiceBase();
        stub.Name.Get("test");
        stub.GetInternalId.Return("id");

        // Subscribe to the protected event via the base class
        // (Note: InternalStateChanged is protected, but OnInternalStateChanged
        //  triggers it. For testing, we use the stub wrapper's Raise method.)
        var raised = false;
        stub.InternalStateChanged.Raise(stub.Object, EventArgs.Empty);
        // Event fires even though no subscriber — but we can check HasSubscribers
    }

    // =========================================================================
    // VERIFICATION: Protected method calls are tracked
    // =========================================================================
    // DESIGN DECISION: Verification works identically for protected methods.
    // The interceptor tracks calls regardless of access modifier.
    // =========================================================================

    public void ProtectedMethod_Verify()
    {
        var stub = new Stubs.ServiceBase();
        stub.GetInternalId.Return("id");
        stub.Name.Get("test");

        ServiceBase service = stub.Object;
        service.GetDescription(); // calls GetInternalId + FormatTag
        service.GetDescription(); // calls again

        // Verify protected method was called
        stub.GetInternalId.Verify(Times.Exactly(2));
        stub.FormatTag.Verify(Times.Exactly(2));
    }

    // =========================================================================
    // SEQUENCES: Protected methods support ThenReturn/ThenCall
    // =========================================================================

    public void ProtectedMethod_Sequences()
    {
        var stub = new Stubs.ServiceBase();
        stub.Name.Get("test");

        // Use callback form for sequences
        stub.GetInternalId
            .Return(() => "first-id")
            .ThenReturn(() => "second-id");

        ServiceBase service = stub.Object;
        var desc1 = service.GetDescription(); // contains "first-id"
        var desc2 = service.GetDescription(); // contains "second-id"
        var desc3 = service.GetDescription(); // repeats "second-id"
    }
}
