// -----------------------------------------------------------------------------
// Design.Stubs - Async Method API Consistency Across All 9 Patterns
// -----------------------------------------------------------------------------
// This file demonstrates that the three async APIs work consistently across
// all nine KnockOff patterns:
//
// For an async method like Task<string> FetchAsync(int id):
// 1. stub.FetchAsync.Returns("value")       — auto-wraps in Task.FromResult
// 2. stub.FetchAsync.OnCall((id) => "value") — simplified callback, auto-wrapped
// 3. stub.FetchAsync.OnCall((id) => Task.FromResult("value")) — full delegate
//
// Pattern 1 is already covered in BasicMethods.cs (IDataService).
// Pattern 7 (delegate) is intentionally different — no auto-wrapping.
//
// NOTE: Patterns 6/7 and 8/9 are in separate classes because the generator
// has separate pipelines for [KnockOff<T>] (closed generic) and
// [KnockOff(typeof(T<>))] (open generic). Mixing both attribute styles on a
// single class causes a duplicate hintName error in the generator.
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using Design.Domain.Delegates;
using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Methods;

// =============================================================================
// PATTERN 2: GENERIC STANDALONE (interface)
// =============================================================================

[KnockOff]
public partial class AsyncRepositoryStub<T> : IAsyncRepository<T> where T : class;

// =============================================================================
// PATTERN 3: STANDALONE CLASS
// =============================================================================

[KnockOffBase<AsyncServiceBase>]
public partial class AsyncServiceStub;

// =============================================================================
// PATTERN 4: GENERIC STANDALONE CLASS
// =============================================================================

[KnockOffBase(typeof(AsyncRepositoryBase<>))]
public partial class AsyncRepositoryBaseStub<T> where T : class;

// =============================================================================
// PATTERNS 5, 6, 7: INLINE CLOSED GENERIC
// =============================================================================
// Pattern 5 (Inline Interface) uses BasicMethodsDemo.Stubs.IDataService which
// already has [KnockOff<IDataService>] — tested directly in the test file.

[KnockOff<AsyncServiceBase>]                   // Pattern 6: Inline Class
[KnockOff<AsyncOperation>]                     // Pattern 7: Inline Delegate
public partial class AsyncClosedGenericDemo
{
    // =========================================================================
    // Pattern 6: Inline Class — async methods
    // =========================================================================

    public async Task Pattern6_Returns()
    {
        var stub = new Stubs.AsyncServiceBase();
        stub.FetchAsync.Returns("fetched");

        AsyncServiceBase svc = stub.Object;
        var result = await svc.FetchAsync(1);
        // result == "fetched"
    }

    public async Task Pattern6_OnCallSimplified()
    {
        var stub = new Stubs.AsyncServiceBase();
        stub.FetchAsync.OnCall((id) => $"Fetch-{id}");

        AsyncServiceBase svc = stub.Object;
        var result = await svc.FetchAsync(42);
        // result == "Fetch-42"
    }

    public async Task Pattern6_OnCallFull()
    {
        var stub = new Stubs.AsyncServiceBase();
        stub.FetchAsync.OnCall((int id) => Task.FromResult($"Full-{id}"));

        AsyncServiceBase svc = stub.Object;
        var result = await svc.FetchAsync(99);
        // result == "Full-99"
    }

    // =========================================================================
    // Pattern 7: Inline Delegate — async delegate (intentionally different)
    // =========================================================================
    // DESIGN DECISION: Async delegates do NOT auto-wrap. The delegate's return
    // type IS the full return type (Task<int>), so:
    // - Returns(Task.FromResult(42))  — takes the FULL return type
    // - OnCall(Func<int, Task<int>>)  — takes the FULL delegate type
    //
    // This is intentional: delegates don't have a "method signature" to derive
    // the unwrapped type from. The delegate type IS the contract.
    // =========================================================================

    public async Task Pattern7_Returns()
    {
        var stub = new Stubs.AsyncOperation();
        stub.Interceptor.Returns(Task.FromResult(42));

        AsyncOperation op = stub;
        var result = await op(10);
        // result == 42
    }

    public async Task Pattern7_OnCall()
    {
        var stub = new Stubs.AsyncOperation();
        stub.Interceptor.OnCall((x) => Task.FromResult(x * 2));

        AsyncOperation op = stub;
        var result = await op(21);
        // result == 42
    }
}

// =============================================================================
// PATTERNS 8, 9: INLINE OPEN GENERIC
// =============================================================================

[KnockOff(typeof(IAsyncRepository<>))]         // Pattern 8: Open Generic Interface
[KnockOff(typeof(AsyncRepositoryBase<>))]      // Pattern 9: Open Generic Class
public partial class AsyncOpenGenericDemo
{
    // =========================================================================
    // Pattern 8: Open Generic Interface — async methods
    // =========================================================================

    public async Task Pattern8_Returns()
    {
        var stub = new Stubs.IAsyncRepository<string>();
        stub.GetByIdAsync.Returns("found");

        IAsyncRepository<string> repo = stub;
        var result = await repo.GetByIdAsync(1);
        // result == "found"
    }

    public async Task Pattern8_OnCallSimplified()
    {
        var stub = new Stubs.IAsyncRepository<string>();
        stub.GetByIdAsync.OnCall((id) => $"Item-{id}");

        IAsyncRepository<string> repo = stub;
        var result = await repo.GetByIdAsync(42);
        // result == "Item-42"
    }

    public async Task Pattern8_OnCallFull()
    {
        var stub = new Stubs.IAsyncRepository<string>();
        stub.GetByIdAsync.OnCall((int id) => Task.FromResult<string?>($"Full-{id}"));

        IAsyncRepository<string> repo = stub;
        var result = await repo.GetByIdAsync(99);
        // result == "Full-99"
    }

    // =========================================================================
    // Pattern 9: Open Generic Class — async methods
    // =========================================================================

    public async Task Pattern9_Returns()
    {
        var stub = new Stubs.AsyncRepositoryBase<string>();
        stub.GetByIdAsync.Returns("found");

        AsyncRepositoryBase<string> repo = stub.Object;
        var result = await repo.GetByIdAsync(1);
        // result == "found"
    }

    public async Task Pattern9_OnCallSimplified()
    {
        var stub = new Stubs.AsyncRepositoryBase<string>();
        stub.GetByIdAsync.OnCall((id) => $"Item-{id}");

        AsyncRepositoryBase<string> repo = stub.Object;
        var result = await repo.GetByIdAsync(42);
        // result == "Item-42"
    }

    public async Task Pattern9_OnCallFull()
    {
        var stub = new Stubs.AsyncRepositoryBase<string>();
        stub.GetByIdAsync.OnCall((int id) => Task.FromResult<string?>($"Full-{id}"));

        AsyncRepositoryBase<string> repo = stub.Object;
        var result = await repo.GetByIdAsync(99);
        // result == "Full-99"
    }
}
