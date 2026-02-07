// -----------------------------------------------------------------------------
// Design.Tests - Tests for Open Generic Inline Stubs with Overloaded Methods
// -----------------------------------------------------------------------------
// These tests verify that the overload API works correctly with:
// - Open generic interface stubs: [KnockOff(typeof(IGenericFormatter<>))]
// - Open generic class stubs: [KnockOff(typeof(RepositoryBase<>))]
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using Design.Domain.Services;
using KnockOff;

namespace Design.Tests.GenericOverloadTests;

// =============================================================================
// OPEN GENERIC INTERFACE WITH OVERLOADS
// =============================================================================

[KnockOff(typeof(IGenericFormatter<>))]
public partial class OpenGenericInterfaceOverloadTests
{
    [Fact]
    public void OpenGenericInterface_OnCall_DisambiguatesByParameterCount()
    {
        // Arrange
        var stub = new Stubs.IGenericFormatter<string>();

        stub.Format.Return((item) => $"[1: {item}]");
        stub.Format.Return((item, uppercase) => $"[2: {item}, {uppercase}]");
        stub.Format.Return((item, uppercase, maxLength) => $"[3: {item}, {uppercase}, {maxLength}]");

        IGenericFormatter<string> formatter = stub;

        // Act & Assert
        Assert.Equal("[1: hello]", formatter.Format("hello"));
        Assert.Equal("[2: world, True]", formatter.Format("world", true));
        Assert.Equal("[3: test, False, 5]", formatter.Format("test", false, 5));
    }

    [Fact]
    public void OpenGenericInterface_Get_OverloadsReturningT()
    {
        // Arrange
        var stub = new Stubs.IGenericFormatter<TestEntity>();

        // Overloads differ by parameter type - need explicit types
        stub.Get.Return(() => new TestEntity { Id = 0, Name = "Default" });
        stub.Get.Return((int id) => new TestEntity { Id = id, Name = "ById" });
        stub.Get.Return((string name) => new TestEntity { Id = 99, Name = name });

        IGenericFormatter<TestEntity> formatter = stub;

        // Act & Assert
        Assert.Equal("Default", formatter.Get().Name);
        Assert.Equal(42, formatter.Get(42).Id);
        Assert.Equal("TestName", formatter.Get("TestName").Name);
    }

    [Fact]
    public void OpenGenericInterface_When_WorksWithOverloads()
    {
        // Arrange
        var stub = new Stubs.IGenericFormatter<string>();

        stub.Format.Return((item) => "default");
        stub.Format.When("special").Return("SPECIAL");

        IGenericFormatter<string> formatter = stub;

        // Act & Assert
        Assert.Equal("SPECIAL", formatter.Format("special"));
        Assert.Equal("default", formatter.Format("other"));
    }

    [Fact]
    public void OpenGenericInterface_Tracking_WorksPerOverload()
    {
        // Arrange
        var stub = new Stubs.IGenericFormatter<string>();

        var tracking1 = stub.Format.Return((item) => item);
        var tracking2 = stub.Format.Return((item, uppercase) => item);

        IGenericFormatter<string> formatter = stub;

        // Act
        formatter.Format("a");
        formatter.Format("b");
        formatter.Format("c", true);

        // Assert
        tracking1.Verify(Times.Exactly(2));
        tracking2.Verify(Times.Once);
        stub.Format.Verify(Times.Exactly(3)); // Total across all overloads
    }

    [Fact]
    public void OpenGenericInterface_Transform_ReturnsT()
    {
        // Arrange
        var stub = new Stubs.IGenericFormatter<TestEntity>();

        stub.Transform.Return((item) => new TestEntity { Id = item.Id * 2, Name = item.Name });
        stub.Transform.Return((item, options) => new TestEntity { Id = item.Id, Name = $"{item.Name}-{options}" });

        IGenericFormatter<TestEntity> formatter = stub;

        var input = new TestEntity { Id = 5, Name = "Test" };

        // Act
        var result1 = formatter.Transform(input);
        var result2 = formatter.Transform(input, "opt");

        // Assert
        Assert.Equal(10, result1.Id);
        Assert.Equal("Test-opt", result2.Name);
    }

    [Fact]
    public void OpenGenericInterface_DifferentTypeArguments_Independent()
    {
        // Arrange
        var stringStub = new Stubs.IGenericFormatter<string>();
        var entityStub = new Stubs.IGenericFormatter<TestEntity>();

        stringStub.Format.Return((item) => $"String: {item}");
        entityStub.Format.Return((item) => $"Entity: {item.Name}");

        IGenericFormatter<string> stringFormatter = stringStub;
        IGenericFormatter<TestEntity> entityFormatter = entityStub;

        // Act & Assert
        Assert.Equal("String: hello", stringFormatter.Format("hello"));
        Assert.Equal("Entity: Test", entityFormatter.Format(new TestEntity { Name = "Test" }));
    }
}

// =============================================================================
// OPEN GENERIC CLASS WITH OVERLOADS
// =============================================================================

[KnockOff(typeof(RepositoryBase<>))]
public partial class OpenGenericClassOverloadTests
{
    [Fact]
    public void OpenGenericClass_BasicUsage()
    {
        // Arrange
        var stub = new Stubs.RepositoryBase<TestEntity>();

        stub.Name.Get("TestRepository");
        stub.GetById.Return((id) => new TestEntity { Id = id, Name = "ById" });

        // Act - use .Object to get the actual instance
        RepositoryBase<TestEntity> repo = stub.Object;

        // Assert
        Assert.Equal("TestRepository", repo.Name);
        Assert.Equal(42, repo.GetById(42)?.Id);
    }

    [Fact]
    public void OpenGenericClass_GetDefault_Overloads()
    {
        // Arrange
        var stub = new Stubs.RepositoryBase<TestEntity>();

        // Class stubs now use the same overload API as interface stubs
        // C# overload resolution determines which OnCall to use based on lambda signature
        stub.GetDefault.Return(() => new TestEntity { Id = 0, Name = "Default" });
        stub.GetDefault.Return((string filter) => new TestEntity { Id = 1, Name = $"Filtered: {filter}" });

        RepositoryBase<TestEntity> repo = stub.Object;

        // Act
        var result1 = repo.GetDefault();
        var result2 = repo.GetDefault("active");

        // Assert
        Assert.Equal("Default", result1?.Name);
        Assert.Equal("Filtered: active", result2?.Name);
    }

    [Fact]
    public void OpenGenericClass_Tracking_Works()
    {
        // Arrange
        var stub = new Stubs.RepositoryBase<TestEntity>();

        var tracking = stub.Save.Call((entity) => { });

        RepositoryBase<TestEntity> repo = stub.Object;

        // Act
        repo.Save(new TestEntity { Id = 1 });
        repo.Save(new TestEntity { Id = 2 });

        // Assert
        tracking.Verify(Times.Exactly(2));
    }

    [Fact]
    public void OpenGenericClass_DifferentTypeArguments_Independent()
    {
        // Arrange
        var stringRepo = new Stubs.RepositoryBase<string>();
        var entityRepo = new Stubs.RepositoryBase<TestEntity>();

        stringRepo.GetById.Return((id) => $"String-{id}");
        entityRepo.GetById.Return((id) => new TestEntity { Id = id, Name = "Entity" });

        RepositoryBase<string> strRepo = stringRepo.Object;
        RepositoryBase<TestEntity> entRepo = entityRepo.Object;

        // Act & Assert
        Assert.Equal("String-5", strRepo.GetById(5));
        Assert.Equal(5, entRepo.GetById(5)?.Id);
    }
}

// Uses TestEntity from GenericStandaloneOverloadTests.cs
