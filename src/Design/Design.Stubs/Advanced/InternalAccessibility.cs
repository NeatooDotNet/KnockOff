// -----------------------------------------------------------------------------
// Design.Stubs - Internal Accessibility Verification
// -----------------------------------------------------------------------------
// This file verifies that KnockOff correctly emits accessibility modifiers on
// generated stub classes when the target type is internal.
//
// DESIGN DECISION: The generated stub class accessibility must be compatible
// with the target type's accessibility. A public class cannot implement an
// internal interface (CS0060) or inherit from an internal class (CS0060).
//
// For standalone patterns (1-4), the generated Base helper class must match
// the user's stub class accessibility.
//
// For inline patterns (5-9), the generated stub class must match the target
// type's accessibility.
// -----------------------------------------------------------------------------

#pragma warning disable CA1812 // Internal class is never instantiated - design verification code
#pragma warning disable CA1852 // Type can be sealed - standalone stubs must be partial, not sealed

using KnockOff;

namespace Design.Stubs.Advanced;

// =============================================================================
// Internal target types (defined in the same assembly as the stubs)
// =============================================================================

internal interface IInternalService
{
    string GetName();
    int Count { get; }
}

internal interface IGenericInternalService<T> where T : class
{
    T GetById(int id);
    void Save(T entity);
}

internal abstract class InternalClassBase
{
    public virtual string Name { get; set; } = "";
    public virtual string Process(string input) => input;
}

internal abstract class InternalGenericClassBase<T> where T : class
{
    public abstract T GetItem(int id);
    public virtual string Describe(T item) => item?.ToString() ?? "";
}

internal delegate int InternalOperation(int a, int b);

// =============================================================================
// PATTERN 1: Standalone stub for internal interface
// =============================================================================
// The user declares `internal partial class` -- the generated Base class must
// also be `internal class StubBase`.

[KnockOff]
internal partial class InternalServiceStub : IInternalService
{
}

// =============================================================================
// PATTERN 2: Generic standalone stub for internal generic interface
// =============================================================================
// The user declares `internal partial class` with type parameters -- the
// generated Base class must also be `internal class StubBase`.

[KnockOff]
internal partial class GenericInternalServiceStub<T> : IGenericInternalService<T> where T : class
{
}

// =============================================================================
// PATTERN 3: Standalone class stub for internal class
// =============================================================================
// The user declares `internal partial class` -- the generated Base class must
// also be `internal class StubBase`.

[KnockOffBase<InternalClassBase>]
internal partial class InternalClassStub
{
}

// =============================================================================
// PATTERN 4: Generic standalone class stub for internal generic class
// =============================================================================
// The user declares `internal partial class` with type parameters -- the
// generated Base class must also be `internal class StubBase`.

[KnockOffBase(typeof(InternalGenericClassBase<>))]
internal partial class GenericInternalClassStub<T> where T : class
{
}

// =============================================================================
// PATTERNS 5, 6, 7: Inline stubs for internal types
// =============================================================================
// The generated stub classes inside the Stubs container must use `internal`
// accessibility to match the target type.

[KnockOff<IInternalService>]
[KnockOff<InternalClassBase>]
[KnockOff<InternalOperation>]
public partial class InternalInlineStubTests
{
    public void Verify_InlineInterfaceStub_InternalAccessibility()
    {
        // Pattern 5: Inline interface stub for internal interface
        var interfaceStub = new Stubs.IInternalService();
        IInternalService svc = interfaceStub;
        interfaceStub.GetName.Return("test");
    }

    public void Verify_InlineClassStub_InternalAccessibility()
    {
        // Pattern 6: Inline class stub for internal class
        var classStub = new Stubs.InternalClassBase();
        InternalClassBase obj = classStub.Object;
        classStub.Name.Get("test");
    }

    public void Verify_InlineDelegateStub_InternalAccessibility()
    {
        // Pattern 7: Inline delegate stub for internal delegate
        var delegateStub = new Stubs.InternalOperation();
        delegateStub.Interceptor.Call((a, b) => a + b);
    }
}

// =============================================================================
// PATTERN 8: Open generic inline stub for internal interface
// =============================================================================

[KnockOff(typeof(IGenericInternalService<>))]
public partial class InternalOpenGenericInterfaceTests
{
    public void Verify_OpenGenericInterfaceStub_InternalAccessibility()
    {
        var stub = new Stubs.IGenericInternalService<string>();
        IGenericInternalService<string> svc = stub;
        stub.GetById.Return("test");
    }
}

// =============================================================================
// PATTERN 9: Open generic inline stub for internal generic class
// =============================================================================
// The generated stub class inside the Stubs container must use `internal`
// accessibility to match the target type.

[KnockOff(typeof(InternalGenericClassBase<>))]
public partial class InternalOpenGenericClassTests
{
    public void Verify_OpenGenericClassStub_InternalAccessibility()
    {
        var stub = new Stubs.InternalGenericClassBase<string>();
        InternalGenericClassBase<string> obj = stub.Object;
        stub.GetItem.Return("test");
        stub.Describe.Call((item) => $"Described: {item}");
    }
}
