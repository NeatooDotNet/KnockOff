// -----------------------------------------------------------------------------
// Design.Stubs - Async Method API Consistency Across All 9 Patterns
// -----------------------------------------------------------------------------
// This file demonstrates that the three async APIs work consistently across
// all nine KnockOff patterns:
//
// For an async method like Task<string> FetchAsync(int id):
// 1. stub.FetchAsync.Returns("value")       — auto-wraps in Task.FromResult
// 2. stub.FetchAsync.Returns((id) => "value") — simplified callback, auto-wrapped
// 3. stub.FetchAsync.Returns((id) => Task.FromResult("value")) — full delegate
//
// Pattern 1 is already covered in BasicMethods.cs (IDataService).
// Pattern 7 (delegate) now supports full auto-wrapping via MethodInterceptorRenderer reuse.
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

    public async Task Pattern6_ReturnsSimplified()
    {
        var stub = new Stubs.AsyncServiceBase();
        stub.FetchAsync.Returns((id) => $"Fetch-{id}");

        AsyncServiceBase svc = stub.Object;
        var result = await svc.FetchAsync(42);
        // result == "Fetch-42"
    }

    public async Task Pattern6_ReturnsFull()
    {
        var stub = new Stubs.AsyncServiceBase();
        stub.FetchAsync.Returns((int id) => Task.FromResult($"Full-{id}"));

        AsyncServiceBase svc = stub.Object;
        var result = await svc.FetchAsync(99);
        // result == "Full-99"
    }

    // =========================================================================
    // Pattern 7: Inline Delegate — async delegate with auto-wrapping
    // =========================================================================
    // DESIGN DECISION: After MethodInterceptorRenderer reuse, async delegates
    // support the same three-tier API as all other patterns:
    // - Returns(42)                   — auto-wraps in Task.FromResult (Tier 1)
    // - Returns((x) => x * 2)          — simplified callback, auto-wrapped (Tier 2)
    // - Returns((x) => Task.FromResult(x * 2)) — full delegate (Tier 3)
    // =========================================================================

    public async Task Pattern7_Returns_AutoWrapped()
    {
        var stub = new Stubs.AsyncOperation();
        // Tier 1: auto-wraps int -> Task<int> via Task.FromResult
        stub.Interceptor.Returns(42);

        AsyncOperation op = stub;
        var result = await op(10);
        // result == 42
    }

    public async Task Pattern7_ReturnsSimplified()
    {
        var stub = new Stubs.AsyncOperation();
        // Tier 2: simplified callback (int -> int), auto-wrapped in Task.FromResult
        stub.Interceptor.Returns((int x) => x * 2);

        AsyncOperation op = stub;
        var result = await op(21);
        // result == 42
    }

    public async Task Pattern7_Returns_DirectValue()
    {
        var stub = new Stubs.AsyncOperation();
        // Returns takes the inner type (int) and auto-wraps in Task.FromResult
        // Same as Pattern7_Returns_AutoWrapped — confirming Returns(int) works
        stub.Interceptor.Returns(42);

        AsyncOperation op = stub;
        var result = await op(10);
        // result == 42
    }

    public async Task Pattern7_Returns_FullDelegate()
    {
        var stub = new Stubs.AsyncOperation();
        // Tier 3: full delegate still works
        stub.Interceptor.Returns((int x) => Task.FromResult(x * 2));

        AsyncOperation op = stub;
        var result = await op(21);
        // result == 42
    }

    public async Task Pattern7_Sequence()
    {
        var stub = new Stubs.AsyncOperation();
        // Sequence support: first value returned once, then second repeats
        stub.Interceptor.Returns(10, 20);

        AsyncOperation op = stub;
        var r1 = await op(0); // 10
        var r2 = await op(0); // 20
        var r3 = await op(0); // 20 (repeats last)
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

    public async Task Pattern8_ReturnsSimplified()
    {
        var stub = new Stubs.IAsyncRepository<string>();
        stub.GetByIdAsync.Returns((id) => $"Item-{id}");

        IAsyncRepository<string> repo = stub;
        var result = await repo.GetByIdAsync(42);
        // result == "Item-42"
    }

    public async Task Pattern8_ReturnsFull()
    {
        var stub = new Stubs.IAsyncRepository<string>();
        stub.GetByIdAsync.Returns((int id) => Task.FromResult<string?>($"Full-{id}"));

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

    public async Task Pattern9_ReturnsSimplified()
    {
        var stub = new Stubs.AsyncRepositoryBase<string>();
        stub.GetByIdAsync.Returns((id) => $"Item-{id}");

        AsyncRepositoryBase<string> repo = stub.Object;
        var result = await repo.GetByIdAsync(42);
        // result == "Item-42"
    }

    public async Task Pattern9_ReturnsFull()
    {
        var stub = new Stubs.AsyncRepositoryBase<string>();
        stub.GetByIdAsync.Returns((int id) => Task.FromResult<string?>($"Full-{id}"));

        AsyncRepositoryBase<string> repo = stub.Object;
        var result = await repo.GetByIdAsync(99);
        // result == "Full-99"
    }
}
