using KnockOff.Documentation.Samples.Guides;

namespace KnockOff.Documentation.Samples.Tests.Guides;

/// <summary>
/// Tests for docs/guides/generics.md samples.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Guides")]
public class GenericsSamplesTests
{
    // ========================================================================
    // Generic Method Interface (Of<T>() pattern)
    // ========================================================================

    [Fact]
    public void GenericMethod_OfT_ConfiguresPerType()
    {
        var knockOff = new GenSerializerKnockOff();
        IGenSerializer service = knockOff;

        knockOff.Deserialize.Of<GenUser>().OnCall((ko, json) =>
            new GenUser { Id = 1, Name = "FromJson" });

        var user = service.Deserialize<GenUser>("{}");

        Assert.Equal(1, user.Id);
        Assert.Equal("FromJson", user.Name);
    }

    [Fact]
    public void GenericMethod_OfT_TracksPerType()
    {
        var knockOff = new GenSerializerKnockOff();
        IGenSerializer service = knockOff;

        knockOff.Deserialize.Of<GenUser>().OnCall((ko, json) => new GenUser());
        knockOff.Deserialize.Of<GenOrder>().OnCall((ko, json) => new GenOrder());

        service.Deserialize<GenUser>("{}");
        service.Deserialize<GenUser>("{}");
        service.Deserialize<GenOrder>("{}");

        Assert.Equal(2, knockOff.Deserialize.Of<GenUser>().CallCount);
        Assert.Equal(1, knockOff.Deserialize.Of<GenOrder>().CallCount);
    }

    [Fact]
    public void GenericMethod_AggregateTracking()
    {
        var knockOff = new GenSerializerKnockOff();
        IGenSerializer service = knockOff;

        knockOff.Deserialize.Of<GenUser>().OnCall((ko, json) => new GenUser());
        knockOff.Deserialize.Of<GenOrder>().OnCall((ko, json) => new GenOrder());

        service.Deserialize<GenUser>("{}");
        service.Deserialize<GenUser>("{}");
        service.Deserialize<GenOrder>("{}");

        Assert.Equal(3, knockOff.Deserialize.TotalCallCount);
        Assert.True(knockOff.Deserialize.WasCalled);
        Assert.Equal(2, knockOff.Deserialize.CalledTypeArguments.Count);
    }

    [Fact]
    public void GenericMethod_MultipleTypeParams()
    {
        var knockOff = new GenConverterKnockOff();
        IGenConverter service = knockOff;

        knockOff.Convert.Of<string, int>().OnCall((ko, s) => s.Length);

        var result = service.Convert<string, int>("hello");

        Assert.Equal(5, result);
    }

    [Fact]
    public void GenericMethod_ConstrainedType()
    {
        var knockOff = new GenEntityFactoryKnockOff();
        IGenEntityFactory service = knockOff;

        knockOff.Create.Of<GenEmployee>().OnCall((ko) =>
            new GenEmployee { Id = 42, Name = "Test" });

        var employee = service.Create<GenEmployee>();

        Assert.Equal(42, employee.Id);
        Assert.Equal("Test", employee.Name);
    }

    // ========================================================================
    // Generic Standalone Stubs
    // ========================================================================

    [Fact]
    public void GenericStandalone_SameStubDifferentTypes()
    {
        var userRepo = new GenericRepoStub<GenUser>();
        var orderRepo = new GenericRepoStub<GenOrder>();

        userRepo.GetById.OnCall((ko, id) => new GenUser { Id = id, Name = $"User-{id}" });
        orderRepo.GetById.OnCall((ko, id) => new GenOrder { Id = id });

        IGenericRepo<GenUser> userService = userRepo;
        IGenericRepo<GenOrder> orderService = orderRepo;

        var user = userService.GetById(1);
        var order = orderService.GetById(2);

        Assert.Equal("User-1", user?.Name);
        Assert.Equal(2, order?.Id);
    }

    [Fact]
    public void GenericStandalone_Tracking()
    {
        var stub = new GenericRepoStub<GenUser>();
        IGenericRepo<GenUser> repo = stub;

        var tracking = stub.Save.OnCall((ko, entity) => { });
        var user = new GenUser { Id = 1, Name = "Test" };
        repo.Save(user);

        Assert.Equal(1, tracking.CallCount);
        Assert.Same(user, tracking.LastArg);
    }

    [Fact]
    public void GenericStandalone_MultipleTypeParams()
    {
        var cache = new GenericKeyValueStub<string, GenUser>();
        IGenericKeyValue<string, GenUser> service = cache;

        cache.Get.OnCall((ko, key) => new GenUser { Name = key });

        var result = service.Get("admin");

        Assert.Equal("admin", result?.Name);
    }
}
