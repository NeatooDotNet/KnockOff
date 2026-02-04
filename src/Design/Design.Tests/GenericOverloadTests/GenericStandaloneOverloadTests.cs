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
/// Verifies the overload API (OnCall, When, tracking) works with class-level type parameters.
/// </summary>
public class GenericStandaloneOverloadTests
{
    // =========================================================================
    // OnCall() - Overload Disambiguation with Generic Type Parameter
    // =========================================================================

    [Fact]
    public void OnCall_DifferentParameterCounts_DisambiguatesCorrectly()
    {
        // Arrange
        var stub = new GenericFormatterStub<string>();

        // Configure each overload - compiler resolves by parameter count
        stub.Format.OnCall((item) => $"[1-param: {item}]");
        stub.Format.OnCall((item, uppercase) => $"[2-param: {item}, {uppercase}]");
        stub.Format.OnCall((item, uppercase, maxLength) => $"[3-param: {item}, {uppercase}, {maxLength}]");

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

        stub.Format.OnCall((item) => $"Entity: {item.Name}");
        stub.Format.OnCall((item, uppercase) => uppercase ? item.Name.ToUpperInvariant() : item.Name);

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

        stub.Format.OnCall((item) => "default");
        stub.Format.When("special").Returns("SPECIAL");

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

        stub.Format.OnCall((item) => "default");
        stub.Format.When((item) => item.Id > 100).Returns("HIGH-ID");

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
        stub.Format.When("a").Returns("SINGLE-A");
        stub.Format.When("b", true).Returns("DOUBLE-B-UPPER");
        stub.Format.When("c", false, 10).Returns("TRIPLE-C");

        // Default callbacks for non-matched
        stub.Format.OnCall((item) => "default-1");
        stub.Format.OnCall((item, uppercase) => "default-2");
        stub.Format.OnCall((item, uppercase, maxLength) => "default-3");

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

        var tracking1 = stub.Format.OnCall((item) => item);
        var tracking2 = stub.Format.OnCall((item, uppercase) => item);
        var tracking3 = stub.Format.OnCall((item, uppercase, maxLength) => item);

        IGenericFormatter<string> formatter = stub;

        // Act
        formatter.Format("a");
        formatter.Format("b");
        formatter.Format("c", true);
        formatter.Format("d", false, 5);
        formatter.Format("e", true, 10);

        // Assert - per-overload tracking
        tracking1.Verify(Times.Exactly(2));  // "a", "b"
        tracking2.Verify(Times.Once);        // "c"
        tracking3.Verify(Times.Exactly(2));  // "d", "e"
    }

    [Fact]
    public void Tracking_LastArg_WithGenericType()
    {
        // Arrange
        var stub = new GenericFormatterStub<TestEntity>();
        var entity1 = new TestEntity { Id = 1, Name = "First" };
        var entity2 = new TestEntity { Id = 2, Name = "Second" };

        var tracking = stub.Format.OnCall((item) => item.Name);

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

        var tracking = stub.Format.OnCall((item, uppercase, maxLength) => item);

        IGenericFormatter<string> formatter = stub;

        // Act
        formatter.Format("test", true, 100);

        // Assert - LastArgs captures tuple with generic type
        var (item, uppercase, maxLength) = tracking.LastArgs;
        Assert.Equal("test", item);
        Assert.True(uppercase);
        Assert.Equal(100, maxLength);
    }

    [Fact]
    public void Verify_OnInterceptor_CountsAllOverloads()
    {
        // Arrange
        var stub = new GenericFormatterStub<string>();

        stub.Format.OnCall((item) => item);
        stub.Format.OnCall((item, uppercase) => item);
        stub.Format.OnCall((item, uppercase, maxLength) => item);

        IGenericFormatter<string> formatter = stub;

        // Act
        formatter.Format("a");
        formatter.Format("b", true);
        formatter.Format("c", false, 5);
        formatter.Format("d");

        // Assert - Verify on interceptor counts ALL overloads
        stub.Format.Verify(Times.Exactly(4));
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

        stub.Process.OnCall((item) => calls.Add($"1-param: {item}"));
        stub.Process.OnCall((item, tag) => calls.Add($"2-param: {item}, {tag}"));

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

        stub.Transform.OnCall((item) => new TestEntity { Id = item.Id * 2, Name = item.Name });
        stub.Transform.OnCall((item, options) => new TestEntity { Id = item.Id, Name = $"{item.Name}-{options}" });

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
            .OnCall((item) => "first")
            .ThenReturns("second")
            .ThenReturns("third");

        // Independent sequence for two-param overload
        stub.Format
            .OnCall((item, uppercase) => "A")
            .ThenReturns("B");

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

        stringStub.Format.OnCall((item) => $"String: {item}");
        entityStub.Format.OnCall((item) => $"Entity: {item.Name}");

        IGenericFormatter<string> stringFormatter = stringStub;
        IGenericFormatter<TestEntity> entityFormatter = entityStub;

        // Act & Assert
        Assert.Equal("String: hello", stringFormatter.Format("hello"));
        Assert.Equal("Entity: Test", entityFormatter.Format(new TestEntity { Id = 1, Name = "Test" }));

        // Verify independently
        stringStub.Format.Verify(Times.Once);
        entityStub.Format.Verify(Times.Once);
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
        stub.Get.OnCall(() => defaultEntity);           // T Get()
        stub.Get.OnCall((int id) => entity42);          // T Get(int id) - explicit int
        stub.Get.OnCall((string name) => entityNamed);  // T Get(string name) - explicit string

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

        var tracking = stub.Get.OnCall(() => "result");

        IGenericFormatter<string> formatter = stub;

        // Act
        formatter.Get();
        formatter.Get();

        // Assert
        tracking.Verify(Times.Exactly(2));
    }

    [Fact]
    public void Get_When_MatchesByParameterSignature()
    {
        // Arrange
        var stub = new GenericFormatterStub<TestEntity>();

        var special = new TestEntity { Id = 100, Name = "Special" };
        var normal = new TestEntity { Id = 1, Name = "Normal" };

        // When on int overload - explicit type needed for OnCall
        stub.Get.When(100).Returns(special);
        stub.Get.OnCall((int id) => normal);

        // When on string overload - explicit type needed for OnCall
        stub.Get.When("admin").Returns(new TestEntity { Id = 999, Name = "Admin" });
        stub.Get.OnCall((string name) => new TestEntity { Id = 0, Name = name });

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

        var limitedMatches = new List<TestEntity>
        {
            new() { Id = 1, Name = "One" }
        };

        // Configure overloads - one takes predicate, one takes predicate + limit
        stub.Find.OnCall((predicate) => allMatches.Where(predicate));
        stub.Find.OnCall((predicate, limit) => allMatches.Where(predicate).Take(limit));

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

        var tracking1 = stub.Find.OnCall((predicate) => Array.Empty<string>());
        var tracking2 = stub.Find.OnCall((predicate, limit) => Array.Empty<string>());

        IGenericFormatter<string> formatter = stub;

        // Act
        formatter.Find(s => true);
        formatter.Find(s => true);
        formatter.Find(s => true, 5);

        // Assert
        tracking1.Verify(Times.Exactly(2));
        tracking2.Verify(Times.Once);
    }
    // =========================================================================
    // USER METHODS - Overrides Returning Generic Type T
    // =========================================================================

    [Fact]
    public void UserMethod_Get_OverloadsReturningT_Work()
    {
        // Arrange
        var stub = new GenericFormatterWithUserMethodsStub<TestEntity>();

        var defaultEntity = new TestEntity { Id = 0, Name = "Default" };
        var byIdEntity = new TestEntity { Id = 42, Name = "ById" };
        var byNameEntity = new TestEntity { Id = 99, Name = "ByName" };

        stub.DefaultInstance = defaultEntity;
        stub.CreateById = id => new TestEntity { Id = id, Name = $"Entity-{id}" };
        stub.CreateByName = name => new TestEntity { Id = 0, Name = name };

        IGenericFormatter<TestEntity> formatter = stub;

        // Act - each overload calls its user method override
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
    public void UserMethod_Transform_OverloadsWithT_Work()
    {
        // Arrange
        var stub = new GenericFormatterWithUserMethodsStub<TestEntity>();
        var entity = new TestEntity { Id = 1, Name = "Original" };

        IGenericFormatter<TestEntity> formatter = stub;

        // Act - Transform user methods just return the item
        var result1 = formatter.Transform(entity);
        var result2 = formatter.Transform(entity, "options");

        // Assert - same instance returned
        Assert.Same(entity, result1);
        Assert.Same(entity, result2);
    }

    [Fact]
    public void UserMethod_Format_OverloadsWithTParameter_Work()
    {
        // Arrange
        var stub = new GenericFormatterWithUserMethodsStub<TestEntity>();
        var entity = new TestEntity { Id = 1, Name = "Test" };

        IGenericFormatter<TestEntity> formatter = stub;

        // Act
        var result1 = formatter.Format(entity);
        var result2 = formatter.Format(entity, uppercase: true);
        var result3 = formatter.Format(entity, uppercase: false, maxLength: 5);

        // Assert - user method implementations
        Assert.StartsWith("[User:", result1, StringComparison.Ordinal);
        Assert.Contains("TEST", result2, StringComparison.Ordinal); // uppercase
        Assert.Equal(5, result3.Length); // truncated
    }

    [Fact]
    public void UserMethod_Tracking_WorksWithUserMethods()
    {
        // Arrange
        var stub = new GenericFormatterWithUserMethodsStub<TestEntity>();
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
        stub.Get.Verify(Times.Exactly(5));
    }

    [Fact]
    public void UserMethod_OnCall_SupersedesUserMethod()
    {
        // Arrange
        var stub = new GenericFormatterWithUserMethodsStub<TestEntity>();
        stub.DefaultInstance = new TestEntity { Id = 0, Name = "UserMethod" };

        // OnCall supersedes the user method - note suffixed method name for overloaded user methods
        stub.Get.OnCall_NoParams_T(() => new TestEntity { Id = 999, Name = "OnCallOverride" });

        IGenericFormatter<TestEntity> formatter = stub;

        // Act
        var result = formatter.Get();

        // Assert - OnCall wins over user method
        Assert.Equal(999, result.Id);
        Assert.Equal("OnCallOverride", result.Name);
    }

    [Fact]
    public void UserMethod_OnCall_SupersedesSpecificOverload()
    {
        // Arrange
        var stub = new GenericFormatterWithUserMethodsStub<TestEntity>();
        stub.DefaultInstance = new TestEntity { Id = 0, Name = "Default" };
        stub.CreateById = id => new TestEntity { Id = id, Name = "UserMethodById" };
        stub.CreateByName = name => new TestEntity { Id = 0, Name = name };

        // Override only the int overload with OnCall - note suffixed method name
        stub.Get.OnCall_Int32_T(id => new TestEntity { Id = id * 10, Name = "OnCallById" });

        IGenericFormatter<TestEntity> formatter = stub;

        // Act
        var result1 = formatter.Get();           // Uses user method
        var result2 = formatter.Get(5);          // Uses OnCall (supersedes user method)
        var result3 = formatter.Get("test");     // Uses user method

        // Assert
        Assert.Equal("Default", result1.Name);       // User method
        Assert.Equal(50, result2.Id);                // OnCall: 5 * 10
        Assert.Equal("OnCallById", result2.Name);    // OnCall
        Assert.Equal("test", result3.Name);          // User method
    }

    [Fact]
    public void UserMethod_Returns_SupersedesUserMethod()
    {
        // Arrange
        var stub = new GenericFormatterWithUserMethodsStub<TestEntity>();
        stub.DefaultInstance = new TestEntity { Id = 0, Name = "UserMethod" };

        // Returns also works with suffixed names
        var overrideEntity = new TestEntity { Id = 888, Name = "ReturnsOverride" };
        stub.Get.Returns_NoParams_T(overrideEntity);

        IGenericFormatter<TestEntity> formatter = stub;

        // Act
        var result = formatter.Get();

        // Assert - Returns wins over user method
        Assert.Same(overrideEntity, result);
    }

    [Fact]
    public void UserMethod_DifferentTypeArguments_IndependentBehavior()
    {
        // Arrange - two stubs with different type arguments
        var stringStub = new GenericFormatterWithUserMethodsStub<string>();
        var entityStub = new GenericFormatterWithUserMethodsStub<TestEntity>();

        stringStub.DefaultInstance = "DefaultString";
        stringStub.CreateById = id => $"String-{id}";

        entityStub.DefaultInstance = new TestEntity { Id = 0, Name = "DefaultEntity" };
        entityStub.CreateById = id => new TestEntity { Id = id, Name = $"Entity-{id}" };

        IGenericFormatter<string> stringFormatter = stringStub;
        IGenericFormatter<TestEntity> entityFormatter = entityStub;

        // Act
        var stringResult = stringFormatter.Get(5);
        var entityResult = entityFormatter.Get(5);

        // Assert - each uses its own type's user method
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
