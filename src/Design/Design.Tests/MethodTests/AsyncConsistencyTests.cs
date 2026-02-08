// -----------------------------------------------------------------------------
// Design.Tests - Async Method API Consistency Tests
// -----------------------------------------------------------------------------
// Verifies all three async APIs work identically across all 9 patterns:
// - Returns(unwrappedValue) — auto-wraps in Task.FromResult
// - OnCall(Func<..., T>) — simplified callback, auto-wrapped
// - OnCall(Func<..., Task<T>>) — full delegate
//
// Pattern 1 is covered in MethodBasicsTests (IDataService).
// Pattern 7 (delegate) supports full auto-wrapping via MethodInterceptorRenderer reuse.
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using Design.Domain.Delegates;
using Design.Domain.Services;
using Design.Stubs.Methods;

namespace Design.Tests.MethodTests;

public class AsyncConsistencyTests
{
    // =========================================================================
    // Pattern 2: Generic Standalone (interface)
    // =========================================================================

    [Fact]
    public async Task Pattern2_Returns()
    {
        var stub = new AsyncRepositoryStub<string>();
        stub.GetByIdAsync.Return("found");

        IAsyncRepository<string> repo = stub;
        var result = await repo.GetByIdAsync(1);

        Assert.Equal("found", result);
    }

    [Fact]
    public async Task Pattern2_OnCallSimplified()
    {
        var stub = new AsyncRepositoryStub<string>();
        stub.GetByIdAsync.Return((id) => $"Item-{id}");

        IAsyncRepository<string> repo = stub;
        var result = await repo.GetByIdAsync(42);

        Assert.Equal("Item-42", result);
    }

    [Fact]
    public async Task Pattern2_OnCallFull()
    {
        var stub = new AsyncRepositoryStub<string>();
        stub.GetByIdAsync.Return((int id) => Task.FromResult<string?>($"Full-{id}"));

        IAsyncRepository<string> repo = stub;
        var result = await repo.GetByIdAsync(99);

        Assert.Equal("Full-99", result);
    }

    [Fact]
    public async Task Pattern2_VoidAsync()
    {
        var stub = new AsyncRepositoryStub<string>();
        string? saved = null;
        stub.SaveAsync.Return((entity) => saved = entity);

        IAsyncRepository<string> repo = stub;
        await repo.SaveAsync("test-entity");

        Assert.Equal("test-entity", saved);
    }

    // =========================================================================
    // Pattern 3: Standalone Class
    // =========================================================================

    [Fact]
    public async Task Pattern3_Returns()
    {
        var stub = new AsyncServiceStub();
        stub.FetchAsync.Return("fetched");

        AsyncServiceBase svc = stub.Object;
        var result = await svc.FetchAsync(1);

        Assert.Equal("fetched", result);
    }

    [Fact]
    public async Task Pattern3_OnCallSimplified()
    {
        var stub = new AsyncServiceStub();
        stub.FetchAsync.Return((id) => $"Fetch-{id}");

        AsyncServiceBase svc = stub.Object;
        var result = await svc.FetchAsync(42);

        Assert.Equal("Fetch-42", result);
    }

    [Fact]
    public async Task Pattern3_OnCallFull()
    {
        var stub = new AsyncServiceStub();
        stub.FetchAsync.Return((int id) => Task.FromResult($"Full-{id}"));

        AsyncServiceBase svc = stub.Object;
        var result = await svc.FetchAsync(99);

        Assert.Equal("Full-99", result);
    }

    [Fact]
    public async Task Pattern3_VoidAsync()
    {
        var stub = new AsyncServiceStub();
        string? saved = null;
        stub.SaveAsync.Return((data) => saved = data);

        AsyncServiceBase svc = stub.Object;
        await svc.SaveAsync("test-data");

        Assert.Equal("test-data", saved);
    }

    // =========================================================================
    // Pattern 4: Generic Standalone Class
    // =========================================================================

    [Fact]
    public async Task Pattern4_Returns()
    {
        var stub = new AsyncRepositoryBaseStub<string>();
        stub.GetByIdAsync.Return("found");

        AsyncRepositoryBase<string> repo = stub.Object;
        var result = await repo.GetByIdAsync(1);

        Assert.Equal("found", result);
    }

    [Fact]
    public async Task Pattern4_OnCallSimplified()
    {
        var stub = new AsyncRepositoryBaseStub<string>();
        stub.GetByIdAsync.Return((id) => $"Item-{id}");

        AsyncRepositoryBase<string> repo = stub.Object;
        var result = await repo.GetByIdAsync(42);

        Assert.Equal("Item-42", result);
    }

    [Fact]
    public async Task Pattern4_OnCallFull()
    {
        var stub = new AsyncRepositoryBaseStub<string>();
        stub.GetByIdAsync.Return((int id) => Task.FromResult<string?>($"Full-{id}"));

        AsyncRepositoryBase<string> repo = stub.Object;
        var result = await repo.GetByIdAsync(99);

        Assert.Equal("Full-99", result);
    }

    // =========================================================================
    // Pattern 5: Inline Interface (uses BasicMethodsDemo which has [KnockOff<IDataService>])
    // =========================================================================

    [Fact]
    public async Task Pattern5_Returns()
    {
        var stub = new BasicMethodsDemo.Stubs.IDataService();
        stub.GetDataAsync.Return("hello");

        IDataService svc = stub;
        var result = await svc.GetDataAsync(1);

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task Pattern5_OnCallSimplified()
    {
        var stub = new BasicMethodsDemo.Stubs.IDataService();
        stub.GetDataAsync.Return((id) => $"Data-{id}");

        IDataService svc = stub;
        var result = await svc.GetDataAsync(42);

        Assert.Equal("Data-42", result);
    }

    [Fact]
    public async Task Pattern5_OnCallFull()
    {
        var stub = new BasicMethodsDemo.Stubs.IDataService();
        stub.GetDataAsync.Return((int id) => Task.FromResult<string?>($"Full-{id}"));

        IDataService svc = stub;
        var result = await svc.GetDataAsync(99);

        Assert.Equal("Full-99", result);
    }

    // =========================================================================
    // Pattern 6: Inline Class
    // =========================================================================

    [Fact]
    public async Task Pattern6_Returns()
    {
        var stub = new AsyncClosedGenericDemo.Stubs.AsyncServiceBase();
        stub.FetchAsync.Return("fetched");

        AsyncServiceBase svc = stub.Object;
        var result = await svc.FetchAsync(1);

        Assert.Equal("fetched", result);
    }

    [Fact]
    public async Task Pattern6_OnCallSimplified()
    {
        var stub = new AsyncClosedGenericDemo.Stubs.AsyncServiceBase();
        stub.FetchAsync.Return((id) => $"Fetch-{id}");

        AsyncServiceBase svc = stub.Object;
        var result = await svc.FetchAsync(42);

        Assert.Equal("Fetch-42", result);
    }

    [Fact]
    public async Task Pattern6_OnCallFull()
    {
        var stub = new AsyncClosedGenericDemo.Stubs.AsyncServiceBase();
        stub.FetchAsync.Return((int id) => Task.FromResult($"Full-{id}"));

        AsyncServiceBase svc = stub.Object;
        var result = await svc.FetchAsync(99);

        Assert.Equal("Full-99", result);
    }

    // =========================================================================
    // Pattern 7: Inline Delegate — async auto-wrapping (same as all patterns)
    // After MethodInterceptorRenderer reuse, delegates support auto-wrapping.
    // =========================================================================

    [Fact]
    public async Task Pattern7_Returns()
    {
        var stub = new AsyncClosedGenericDemo.Stubs.AsyncOperation();
        // Returns takes the inner type (int) and auto-wraps in Task.FromResult
        stub.Interceptor.Return(42);

        AsyncOperation op = stub;
        var result = await op(10);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Pattern7_OnCall()
    {
        var stub = new AsyncClosedGenericDemo.Stubs.AsyncOperation();
        stub.Interceptor.Return((x) => Task.FromResult(x * 2));

        AsyncOperation op = stub;
        var result = await op(21);

        Assert.Equal(42, result);
    }

    // =========================================================================
    // Pattern 8: Open Generic Interface
    // =========================================================================

    [Fact]
    public async Task Pattern8_Returns()
    {
        var stub = new AsyncOpenGenericDemo.Stubs.IAsyncRepository<string>();
        stub.GetByIdAsync.Return("found");

        IAsyncRepository<string> repo = stub;
        var result = await repo.GetByIdAsync(1);

        Assert.Equal("found", result);
    }

    [Fact]
    public async Task Pattern8_OnCallSimplified()
    {
        var stub = new AsyncOpenGenericDemo.Stubs.IAsyncRepository<string>();
        stub.GetByIdAsync.Return((id) => $"Item-{id}");

        IAsyncRepository<string> repo = stub;
        var result = await repo.GetByIdAsync(42);

        Assert.Equal("Item-42", result);
    }

    [Fact]
    public async Task Pattern8_OnCallFull()
    {
        var stub = new AsyncOpenGenericDemo.Stubs.IAsyncRepository<string>();
        stub.GetByIdAsync.Return((int id) => Task.FromResult<string?>($"Full-{id}"));

        IAsyncRepository<string> repo = stub;
        var result = await repo.GetByIdAsync(99);

        Assert.Equal("Full-99", result);
    }

    // =========================================================================
    // Pattern 9: Open Generic Class
    // =========================================================================

    [Fact]
    public async Task Pattern9_Returns()
    {
        var stub = new AsyncOpenGenericDemo.Stubs.AsyncRepositoryBase<string>();
        stub.GetByIdAsync.Return("found");

        AsyncRepositoryBase<string> repo = stub.Object;
        var result = await repo.GetByIdAsync(1);

        Assert.Equal("found", result);
    }

    [Fact]
    public async Task Pattern9_OnCallSimplified()
    {
        var stub = new AsyncOpenGenericDemo.Stubs.AsyncRepositoryBase<string>();
        stub.GetByIdAsync.Return((id) => $"Item-{id}");

        AsyncRepositoryBase<string> repo = stub.Object;
        var result = await repo.GetByIdAsync(42);

        Assert.Equal("Item-42", result);
    }

    [Fact]
    public async Task Pattern9_OnCallFull()
    {
        var stub = new AsyncOpenGenericDemo.Stubs.AsyncRepositoryBase<string>();
        stub.GetByIdAsync.Return((int id) => Task.FromResult<string?>($"Full-{id}"));

        AsyncRepositoryBase<string> repo = stub.Object;
        var result = await repo.GetByIdAsync(99);

        Assert.Equal("Full-99", result);
    }
}
