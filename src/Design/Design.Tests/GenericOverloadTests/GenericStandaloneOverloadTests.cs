// -----------------------------------------------------------------------------
// Design.Tests - Tests for Generic Standalone Stubs with Overloaded Methods
// -----------------------------------------------------------------------------
// These tests verify that the overload API works correctly with generic
// standalone stubs (Pattern 1B with overloaded methods).
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.StubPatterns;
using KnockOff;

namespace Design.Tests.GenericOverloadTests;

/// <summary>
/// Tests for generic standalone stubs with overloaded methods.
/// Verifies the overload API (Call, When, tracking) works with class-level type parameters.
/// </summary>
public class GenericStandaloneOverloadTests
{
    // =========================================================================
    // Call() - Overload Disambiguation with Generic Type Parameter
    // =========================================================================

    [Fact]
    public void OnCall_DifferentParameterCounts_DisambiguatesCorrectly()
    {
        // Arrange
        var stub = new GenericFormatterStub<string>();

        // Configure each overload - explicit types needed for disambiguation
        stub.Format.Call((string item) => $"[1-param: {item}]");
        stub.Format.Call((string item, bool uppercase) => $"[2-param: {item}, {uppercase}]");
        stub.Format.Call((string item, bool uppercase, int maxLength) => $"[3-param: {item}, {uppercase}, {maxLength}]");

        IGenericFormatter<string> formatter = stub;

        // Act & Assert - each overload routes to its callback
        Assert.Equal("[1-param: hello]", formatter.Format("hello"));
        Assert.Equal("[2-param: world, True]", formatter.Format("world", true));
        Assert.Equal("[3-param: test, False, 5]", formatter.Format("test", false, 5));
    }

    [Fact]
    public void OnCall_WithReferenceType_WorksCorrectly()
    {
        // Arrange - using a reference type as T
        var stub = new GenericFormatterStub<TestEntity>();
        var entity = new TestEntity { Id = 42, Name = "Test" };

        stub.Format.Call((TestEntity item) => $"Entity: {item.Name}");
        stub.Format.Call((TestEntity item, bool uppercase) => uppercase ? item.Name.ToUpperInvariant() : item.Name);

        IGenericFormatter<TestEntity> formatter = stub;

        // Act & Assert
        Assert.Equal("Entity: Test", formatter.Format(entity));
        Assert.Equal("TEST", formatter.Format(entity, true));
        Assert.Equal("Test", formatter.Format(entity, false));
    }

    // =========================================================================
    // When() - Parameter Matching with Generic Types
    // =========================================================================

    [Fact]
    public void When_ExactValueMatch_WithGenericParameter()
    {
        // Arrange
        var stub = new GenericFormatterStub<string>();

        stub.Format.Call((string item) => "default");
        stub.Format.When("special").Return("SPECIAL");

        IGenericFormatter<string> formatter = stub;

        // Act & Assert
        Assert.Equal("SPECIAL", formatter.Format("special"));
        Assert.Equal("default", formatter.Format("other"));
    }

    [Fact]
    public void When_PredicateMatch_WithGenericParameter()
    {
        // Arrange
        var stub = new GenericFormatterStub<TestEntity>();

        stub.Format.Call((TestEntity item) => "default");
        stub.Format.When((TestEntity item) => item.Id > 100).Return("HIGH-ID");

        IGenericFormatter<TestEntity> formatter = stub;

        // Act & Assert
        Assert.Equal("HIGH-ID", formatter.Format(new TestEntity { Id = 200, Name = "Big" }));
        Assert.Equal("default", formatter.Format(new TestEntity { Id = 50, Name = "Small" }));
    }

    [Fact]
    public void When_MultiParameterOverload_MatchesCorrectOverload()
    {
        // Arrange
        var stub = new GenericFormatterStub<string>();

        // Configure When for different overloads
        stub.Format.When("a").Return("SINGLE-A");
        stub.Format.When("b", true).Return("DOUBLE-B-UPPER");
        stub.Format.When("c", false, 10).Return("TRIPLE-C");

        // Default callbacks for non-matched
        stub.Format.Call((string item) => "default-1");
        stub.Format.Call((string item, bool uppercase) => "default-2");
        stub.Format.Call((string item, bool uppercase, int maxLength) => "default-3");

        IGenericFormatter<string> formatter = stub;

        // Act & Assert - When matches specific overload
        Assert.Equal("SINGLE-A", formatter.Format("a"));
        Assert.Equal("default-1", formatter.Format("x"));

        Assert.Equal("DOUBLE-B-UPPER", formatter.Format("b", true));
        Assert.Equal("default-2", formatter.Format("b", false));

        Assert.Equal("TRIPLE-C", formatter.Format("c", false, 10));
        Assert.Equal("default-3", formatter.Format("c", false, 20));
    }

    // =========================================================================
    // Tracking - Per-Overload Call Tracking with Generic Types
    // =========================================================================

    [Fact]
    public void Tracking_EachOverloadTrackedSeparately()
    {
        // Arrange
        var stub = new GenericFormatterStub<string>();

        var tracking1 = stub.Format.Call((string item) => item);
        var tracking2 = stub.Format.Call((string item, bool uppercase) => item);
        var tracking3 = stub.Format.Call((string item, bool uppercase, int maxLength) => item);

        IGenericFormatter<string> formatter = stub;

        // Act
        formatter.Format("a");
        formatter.Format("b");
        formatter.Format("c", true);
        formatter.Format("d", false, 5);
        formatter.Format("e", true, 10);

        // Assert - per-overload tracking
        tracking1.Verify(Called.Exactly(2));  // "a", "b"
        tracking2.Verify(Called.Once);        // "c"
        tracking3.Verify(Called.Exactly(2));  // "d", "e"
    }

    [Fact]
    public void Tracking_LastArg_WithGenericType()
    {
        // Arrange
        var stub = new GenericFormatterStub<TestEntity>();
        var entity1 = new TestEntity { Id = 1, Name = "First" };
        var entity2 = new TestEntity { Id = 2, Name = "Second" };

        var tracking = stub.Format.Call((TestEntity item) => item.Name);

        IGenericFormatter<TestEntity> formatter = stub;

        // Act
        formatter.Format(entity1);
        formatter.Format(entity2);

        // Assert - LastArg captures the generic type
        Assert.Equal(entity2, tracking.LastArg);
    }

    [Fact]
    public void Tracking_LastArgs_WithMultipleParameters()
    {
        // Arrange
        var stub = new GenericFormatterStub<string>();

        var tracking = stub.Format.Call((string item, bool uppercase, int maxLength) => item);

        IGenericFormatter<string> formatter = stub;

        // Act
        formatter.Format("test", true, 100);

        // Assert - LastArgs captures tuple with generic type
        var lastArgs = tracking.LastArgs;
        Assert.Equal("test", lastArgs.item);
        Assert.True(lastArgs.uppercase);
        Assert.Equal(100, lastArgs.maxLength);
    }

    [Fact]
    public void Verify_OnInterceptor_CountsAllOverloads()
    {
        // Arrange
        var stub = new GenericFormatterStub<string>();

        stub.Format.Call((string item) => item);
        stub.Format.Call((string item, bool uppercase) => item);
        stub.Format.Call((string item, bool uppercase, int maxLength) => item);

        IGenericFormatter<string> formatter = stub;

        // Act
        formatter.Format("a");
        formatter.Format("b", true);
        formatter.Format("c", false, 5);
        formatter.Format("d");

        // Assert - Verify on interceptor counts ALL overloads
        stub.Format.Verify(Called.Exactly(4));
    }

    // =========================================================================
    // Void Method Overloads with Generic Type
    // =========================================================================

    [Fact]
    public void VoidOverloads_OnCall_DisambiguatesCorrectly()
    {
        // Arrange
        var stub = new GenericFormatterStub<string>();
        var calls = new List<string>();

        stub.Process.Call((string item) => calls.Add($"1-param: {item}"));
        stub.Process.Call((string item, string tag) => calls.Add($"2-param: {item}, {tag}"));

        IGenericFormatter<string> formatter = stub;

        // Act
        formatter.Process("hello");
        formatter.Process("world", "tag1");

        // Assert
        Assert.Equal(2, calls.Count);
        Assert.Equal("1-param: hello", calls[0]);
        Assert.Equal("2-param: world, tag1", calls[1]);
    }

    // =========================================================================
    // Transform() - Overloads Returning Generic Type T
    // =========================================================================

    [Fact]
    public void Transform_ReturnsGenericType_WorksWithOverloads()
    {
        // Arrange
        var stub = new GenericFormatterStub<TestEntity>();

        stub.Transform.Call((TestEntity item) => new TestEntity { Id = item.Id * 2, Name = item.Name });
        stub.Transform.Call((TestEntity item, string options) => new TestEntity { Id = item.Id, Name = $"{item.Name}-{options}" });

        IGenericFormatter<TestEntity> formatter = stub;

        var input = new TestEntity { Id = 5, Name = "Original" };

        // Act
        var result1 = formatter.Transform(input);
        var result2 = formatter.Transform(input, "modified");

        // Assert
        Assert.Equal(10, result1.Id);
        Assert.Equal("Original", result1.Name);
        Assert.Equal(5, result2.Id);
        Assert.Equal("Original-modified", result2.Name);
    }

    // =========================================================================
    // Sequences with Generic Overloads
    // =========================================================================

    [Fact]
    public void Sequences_WorkPerOverload_WithGenericType()
    {
        // Arrange
        var stub = new GenericFormatterStub<string>();

        // Sequence for single-param overload
        stub.Format
            .Call((string item) => "first")
            .ThenReturn("second")
            .ThenReturn("third");

        // Independent sequence for two-param overload
        stub.Format
            .Call((string item, bool uppercase) => "A")
            .ThenReturn("B");

        IGenericFormatter<string> formatter = stub;

        // Act & Assert - sequences are independent per overload
        Assert.Equal("first", formatter.Format("x"));
        Assert.Equal("second", formatter.Format("x"));

        Assert.Equal("A", formatter.Format("y", true));
        Assert.Equal("B", formatter.Format("y", false));

        Assert.Equal("third", formatter.Format("x"));  // continues its own sequence
    }

    // =========================================================================
    // Multiple Instantiations with Different Type Arguments
    // =========================================================================

    [Fact]
    public void DifferentTypeArguments_IndependentStubs()
    {
        // Arrange
        var stringStub = new GenericFormatterStub<string>();
        var entityStub = new GenericFormatterStub<TestEntity>();

        stringStub.Format.Call((string item) => $"String: {item}");
        entityStub.Format.Call((TestEntity item) => $"Entity: {item.Name}");

        IGenericFormatter<string> stringFormatter = stringStub;
        IGenericFormatter<TestEntity> entityFormatter = entityStub;

        // Act & Assert
        Assert.Equal("String: hello", stringFormatter.Format("hello"));
        Assert.Equal("Entity: Test", entityFormatter.Format(new TestEntity { Id = 1, Name = "Test" }));

        // Verify independently
        stringStub.Format.Verify(Called.Once);
        entityStub.Format.Verify(Called.Once);
    }

    // =========================================================================
    // Get() - Overloads Where Return Type is T
    // =========================================================================

    [Fact]
    public void Get_OverloadsReturningT_DisambiguateByParameterType()
    {
        // Arrange
        var stub = new GenericFormatterStub<TestEntity>();

        var defaultEntity = new TestEntity { Id = 0, Name = "Default" };
        var entity42 = new TestEntity { Id = 42, Name = "ById" };
        var entityNamed = new TestEntity { Id = 99, Name = "ByName" };

        // Configure each Get() overload - need explicit parameter types when
        // overloads differ only by parameter type (not count)
        stub.Get.Call(() => defaultEntity);           // T Get()
        stub.Get.Call((int id) => entity42);          // T Get(int id) - explicit int
        stub.Get.Call((string name) => entityNamed);  // T Get(string name) - explicit string

        IGenericFormatter<TestEntity> formatter = stub;

        // Act & Assert - each overload routes correctly
        Assert.Same(defaultEntity, formatter.Get());
        Assert.Same(entity42, formatter.Get(42));
        Assert.Same(entityNamed, formatter.Get("test"));
    }

    [Fact]
    public void Get_NoParamOverload_TracksCorrectly()
    {
        // Arrange
        var stub = new GenericFormatterStub<string>();

        var tracking = stub.Get.Call(() => "result");

        IGenericFormatter<string> formatter = stub;

        // Act
        formatter.Get();
        formatter.Get();

        // Assert
        tracking.Verify(Called.Exactly(2));
    }

    [Fact]
    public void Get_When_MatchesByParameterSignature()
    {
        // Arrange
        var stub = new GenericFormatterStub<TestEntity>();

        var special = new TestEntity { Id = 100, Name = "Special" };
        var normal = new TestEntity { Id = 1, Name = "Normal" };

        // When on int overload - explicit type needed for Call
        stub.Get.When(100).Return(special);
        stub.Get.Call((int id) => normal);

        // When on string overload - explicit type needed for Call
        stub.Get.When("admin").Return(new TestEntity { Id = 999, Name = "Admin" });
        stub.Get.Call((string name) => new TestEntity { Id = 0, Name = name });

        IGenericFormatter<TestEntity> formatter = stub;

        // Act & Assert
        Assert.Equal("Special", formatter.Get(100).Name);
        Assert.Equal("Normal", formatter.Get(1).Name);
        Assert.Equal("Admin", formatter.Get("admin").Name);
        Assert.Equal("guest", formatter.Get("guest").Name);
    }

    // =========================================================================
    // Find() - Overloads with Func<T, bool> Predicate
    // =========================================================================

    [Fact]
    public void Find_OverloadsWithPredicate_Work()
    {
        // Arrange
        var stub = new GenericFormatterStub<TestEntity>();

        var allMatches = new List<TestEntity>
        {
            new() { Id = 1, Name = "One" },
            new() { Id = 2, Name = "Two" },
            new() { Id = 3, Name = "Three" }
        };

        // Configure overloads - explicit types for disambiguation
        stub.Find.Call((Func<TestEntity, bool> predicate) => allMatches.Where(predicate));
        stub.Find.Call((Func<TestEntity, bool> predicate, int limit) => allMatches.Where(predicate).Take(limit));

        IGenericFormatter<TestEntity> formatter = stub;

        // Act
        var all = formatter.Find(e => e.Id > 0).ToList();
        var limited = formatter.Find(e => e.Id > 0, 1).ToList();

        // Assert
        Assert.Equal(3, all.Count);
        Assert.Single(limited);
    }

    [Fact]
    public void Find_Tracking_PerOverload()
    {
        // Arrange
        var stub = new GenericFormatterStub<string>();

        var tracking1 = stub.Find.Call((Func<string, bool> predicate) => Array.Empty<string>());
        var tracking2 = stub.Find.Call((Func<string, bool> predicate, int limit) => Array.Empty<string>());

        IGenericFormatter<string> formatter = stub;

        // Act
        formatter.Find(s => true);
        formatter.Find(s => true);
        formatter.Find(s => true, 5);

        // Assert
        tracking1.Verify(Called.Exactly(2));
        tracking2.Verify(Called.Once);
    }
    // =========================================================================
    // STUB OVERRIDE METHODS - Overrides Returning Generic Type T
    // =========================================================================

    [Fact]
    public void StubOverride_Get_OverloadsReturningT_Work()
    {
        // Arrange
        var stub = new GenericFormatterWithStubOverridesStub<TestEntity>();

        var defaultEntity = new TestEntity { Id = 0, Name = "Default" };
        var byIdEntity = new TestEntity { Id = 42, Name = "ById" };
        var byNameEntity = new TestEntity { Id = 99, Name = "ByName" };

        stub.DefaultInstance = defaultEntity;
        stub.CreateById = id => new TestEntity { Id = id, Name = $"Entity-{id}" };
        stub.CreateByName = name => new TestEntity { Id = 0, Name = name };

        IGenericFormatter<TestEntity> formatter = stub;

        // Act - each overload calls its stub override override
        var result1 = formatter.Get();
        var result2 = formatter.Get(42);
        var result3 = formatter.Get("TestName");

        // Assert
        Assert.Same(defaultEntity, result1);
        Assert.Equal(42, result2.Id);
        Assert.Equal("Entity-42", result2.Name);
        Assert.Equal("TestName", result3.Name);
    }

    [Fact]
    public void StubOverride_Transform_OverloadsWithT_Work()
    {
        // Arrange
        var stub = new GenericFormatterWithStubOverridesStub<TestEntity>();
        var entity = new TestEntity { Id = 1, Name = "Original" };

        IGenericFormatter<TestEntity> formatter = stub;

        // Act - Transform stub overrides just return the item
        var result1 = formatter.Transform(entity);
        var result2 = formatter.Transform(entity, "options");

        // Assert - same instance returned
        Assert.Same(entity, result1);
        Assert.Same(entity, result2);
    }

    [Fact]
    public void StubOverride_Format_OverloadsWithTParameter_Work()
    {
        // Arrange
        var stub = new GenericFormatterWithStubOverridesStub<TestEntity>();
        var entity = new TestEntity { Id = 1, Name = "Test" };

        IGenericFormatter<TestEntity> formatter = stub;

        // Act
        var result1 = formatter.Format(entity);
        var result2 = formatter.Format(entity, uppercase: true);
        var result3 = formatter.Format(entity, uppercase: false, maxLength: 5);

        // Assert - stub override implementations
        Assert.StartsWith("[User:", result1, StringComparison.Ordinal);
        Assert.Contains("TEST", result2, StringComparison.Ordinal); // uppercase
        Assert.Equal(5, result3.Length); // truncated
    }

    [Fact]
    public void StubOverride_Tracking_WorksWithStubOverrides()
    {
        // Arrange
        var stub = new GenericFormatterWithStubOverridesStub<TestEntity>();
        stub.DefaultInstance = new TestEntity { Id = 0, Name = "Default" };
        stub.CreateById = id => new TestEntity { Id = id, Name = "ById" };

        IGenericFormatter<TestEntity> formatter = stub;

        // Act
        formatter.Get();
        formatter.Get();
        formatter.Get(1);
        formatter.Get(2);
        formatter.Get(3);

        // Assert - tracking works across all overloads
        stub.Get.Verify(Called.Exactly(5));
    }

    [Fact]
    public void StubOverride_OnCall_SupersedesStubOverride()
    {
        // Arrange
        var stub = new GenericFormatterWithStubOverridesStub<TestEntity>();
        stub.DefaultInstance = new TestEntity { Id = 0, Name = "StubOverride" };

        // Call supersedes the stub override - overloads are disambiguated by delegate type
        stub.Get.Call(() => new TestEntity { Id = 999, Name = "OnCallOverride" });

        IGenericFormatter<TestEntity> formatter = stub;

        // Act
        var result = formatter.Get();

        // Assert - Call wins over stub override
        Assert.Equal(999, result.Id);
        Assert.Equal("OnCallOverride", result.Name);
    }

    [Fact]
    public void StubOverride_OnCall_SupersedesSpecificOverload()
    {
        // Arrange
        var stub = new GenericFormatterWithStubOverridesStub<TestEntity>();
        stub.DefaultInstance = new TestEntity { Id = 0, Name = "Default" };
        stub.CreateById = id => new TestEntity { Id = id, Name = "StubOverrideById" };
        stub.CreateByName = name => new TestEntity { Id = 0, Name = name };

        // Override only the int overload with Call - overloads are disambiguated by delegate type
        stub.Get.Call(id => new TestEntity { Id = id * 10, Name = "OnCallById" });

        IGenericFormatter<TestEntity> formatter = stub;

        // Act
        var result1 = formatter.Get();           // Uses stub override
        var result2 = formatter.Get(5);          // Uses Call (supersedes stub override)
        var result3 = formatter.Get("test");     // Uses stub override

        // Assert
        Assert.Equal("Default", result1.Name);       // Stub override method
        Assert.Equal(50, result2.Id);                // Call: 5 * 10
        Assert.Equal("OnCallById", result2.Name);    // Call
        Assert.Equal("test", result3.Name);          // Stub override method
    }


    [Fact]
    public void StubOverride_DifferentTypeArguments_IndependentBehavior()
    {
        // Arrange - two stubs with different type arguments
        var stringStub = new GenericFormatterWithStubOverridesStub<string>();
        var entityStub = new GenericFormatterWithStubOverridesStub<TestEntity>();

        stringStub.DefaultInstance = "DefaultString";
        stringStub.CreateById = id => $"String-{id}";

        entityStub.DefaultInstance = new TestEntity { Id = 0, Name = "DefaultEntity" };
        entityStub.CreateById = id => new TestEntity { Id = id, Name = $"Entity-{id}" };

        IGenericFormatter<string> stringFormatter = stringStub;
        IGenericFormatter<TestEntity> entityFormatter = entityStub;

        // Act
        var stringResult = stringFormatter.Get(5);
        var entityResult = entityFormatter.Get(5);

        // Assert - each uses its own type's stub override
        Assert.Equal("String-5", stringResult);
        Assert.Equal("Entity-5", entityResult.Name);
    }
}

// Test entity class for generic stub tests
internal sealed class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
