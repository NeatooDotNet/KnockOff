using Xunit;

namespace KnockOffTests;

// ===== Interface with Init Properties =====

public interface IEntityWithInitProperty
{
	string Id { get; init; }
}

public interface IDocumentWithMixedProperties
{
	string Id { get; init; }       // init-only
	string Title { get; set; }     // regular settable
	int Version { get; }           // get-only
}

public interface INullableInitProperty
{
	string? Name { get; init; }
}

public interface IValueTypeInitProperty
{
	int Count { get; init; }
}

public interface INullableValueTypeInitProperty
{
	int? Revision { get; init; }
}

public interface IMultipleInitProperties
{
	string Id { get; init; }
	string Name { get; init; }
	int Version { get; init; }
}

// ===== Standalone Stubs =====

[KnockOff.KnockOff]
public partial class EntityWithInitPropertyKnockOff : IEntityWithInitProperty { }

[KnockOff.KnockOff]
public partial class DocumentWithMixedPropertiesKnockOff : IDocumentWithMixedProperties { }

[KnockOff.KnockOff]
public partial class NullableInitPropertyKnockOff : INullableInitProperty { }

[KnockOff.KnockOff]
public partial class ValueTypeInitPropertyKnockOff : IValueTypeInitProperty { }

[KnockOff.KnockOff]
public partial class NullableValueTypeInitPropertyKnockOff : INullableValueTypeInitProperty { }

[KnockOff.KnockOff]
public partial class MultipleInitPropertiesKnockOff : IMultipleInitProperties { }

// ===== Inline Stubs =====

[KnockOff.KnockOff<IEntityWithInitProperty>]
[KnockOff.KnockOff<IDocumentWithMixedProperties>]
[KnockOff.KnockOff<INullableInitProperty>]
public partial class InitPropertyInlineTests { }

/// <summary>
/// Tests for C# 9 init-only property support in standalone stubs.
///
/// Key insight: The init semantics are enforced at the interface level (explicit implementation
/// uses init accessor), but the backing storage (interceptor's Value property) uses { get; set; }
/// so tests can set up values conveniently. This provides the best of both worlds:
/// - Interface consumers see proper init-only semantics
/// - Test code can easily configure stub values
/// </summary>
public class InitPropertyStandaloneTests
{
	[Fact]
	public void InitProperty_CanSetValueForTesting()
	{
		// Arrange - backing Value property is { get; set; } for test convenience
		var stub = new EntityWithInitPropertyKnockOff();
		stub.Id.Value = "test-123";

		// Assert - value is returned through interface
		IEntityWithInitProperty entity = stub;
		Assert.Equal("test-123", entity.Id);
	}

	[Fact]
	public void InitProperty_TracksGetAccess()
	{
		// Arrange
		var stub = new EntityWithInitPropertyKnockOff();
		stub.Id.Value = "tracked-value";

		// Act
		IEntityWithInitProperty entity = stub;
		_ = entity.Id;
		_ = entity.Id;

		// Assert
		Assert.Equal(2, stub.Id.GetCount);
	}

	[Fact]
	public void MixedProperties_InitAndSetBothWork()
	{
		// Arrange
		var stub = new DocumentWithMixedPropertiesKnockOff();
		stub.Id.Value = "doc-1";
		stub.Title.Value = "Initial Title";
		stub.Version.Value = 1;

		// Act - change Title (set property can be modified)
		stub.Title.Value = "Updated Title";

		// Assert
		IDocumentWithMixedProperties doc = stub;
		Assert.Equal("doc-1", doc.Id);
		Assert.Equal("Updated Title", doc.Title);
		Assert.Equal(1, doc.Version);
	}

	[Fact]
	public void NullableInitProperty_AcceptsNull()
	{
		// Arrange
		var stub = new NullableInitPropertyKnockOff();
		stub.Name.Value = null;

		// Assert
		INullableInitProperty entity = stub;
		Assert.Null(entity.Name);
	}

	[Fact]
	public void NullableInitProperty_AcceptsValue()
	{
		// Arrange
		var stub = new NullableInitPropertyKnockOff();
		stub.Name.Value = "Test Name";

		// Assert
		INullableInitProperty entity = stub;
		Assert.Equal("Test Name", entity.Name);
	}

	[Fact]
	public void ValueTypeInitProperty_SetsCorrectly()
	{
		// Arrange
		var stub = new ValueTypeInitPropertyKnockOff();
		stub.Count.Value = 42;

		// Assert
		IValueTypeInitProperty entity = stub;
		Assert.Equal(42, entity.Count);
	}

	[Fact]
	public void NullableValueTypeInitProperty_AcceptsNull()
	{
		// Arrange
		var stub = new NullableValueTypeInitPropertyKnockOff();
		stub.Revision.Value = null;

		// Assert
		INullableValueTypeInitProperty entity = stub;
		Assert.Null(entity.Revision);
	}

	[Fact]
	public void NullableValueTypeInitProperty_AcceptsValue()
	{
		// Arrange
		var stub = new NullableValueTypeInitPropertyKnockOff();
		stub.Revision.Value = 5;

		// Assert
		INullableValueTypeInitProperty entity = stub;
		Assert.Equal(5, entity.Revision);
	}

	[Fact]
	public void MultipleInitProperties_AllSetCorrectly()
	{
		// Arrange
		var stub = new MultipleInitPropertiesKnockOff();
		stub.Id.Value = "multi-1";
		stub.Name.Value = "Test Entity";
		stub.Version.Value = 3;

		// Assert
		IMultipleInitProperties entity = stub;
		Assert.Equal("multi-1", entity.Id);
		Assert.Equal("Test Entity", entity.Name);
		Assert.Equal(3, entity.Version);
	}

	[Fact]
	public void InitProperty_ResetClearsState()
	{
		// Arrange
		var stub = new EntityWithInitPropertyKnockOff();
		stub.Id.Value = "reset-test";
		IEntityWithInitProperty entity = stub;
		_ = entity.Id; // trigger get

		// Act
		stub.Id.Reset();

		// Assert - init properties only have GetCount and Value
		Assert.Equal(0, stub.Id.GetCount);
		Assert.Equal(default, stub.Id.Value);
	}
}

/// <summary>
/// Tests for C# 9 init-only property support in inline stubs.
/// </summary>
public class InitPropertyInlineStubTests
{
	[Fact]
	public void InlineStub_InitProperty_CanSetValueForTesting()
	{
		// Arrange
		var stub = new InitPropertyInlineTests.Stubs.IEntityWithInitProperty();
		stub.Id.Value = "inline-test-123";

		// Assert
		IEntityWithInitProperty entity = stub;
		Assert.Equal("inline-test-123", entity.Id);
	}

	[Fact]
	public void InlineStub_MixedProperties_InitAndSetBothWork()
	{
		// Arrange
		var stub = new InitPropertyInlineTests.Stubs.IDocumentWithMixedProperties();
		stub.Id.Value = "inline-doc-1";
		stub.Title.Value = "Initial";
		stub.Version.Value = 1;

		// Act
		stub.Title.Value = "Updated";

		// Assert
		IDocumentWithMixedProperties doc = stub;
		Assert.Equal("inline-doc-1", doc.Id);
		Assert.Equal("Updated", doc.Title);
	}

	[Fact]
	public void InlineStub_NullableInitProperty_Works()
	{
		// Arrange
		var stub = new InitPropertyInlineTests.Stubs.INullableInitProperty();
		stub.Name.Value = "inline-name";

		// Assert
		INullableInitProperty entity = stub;
		Assert.Equal("inline-name", entity.Name);
	}

	[Fact]
	public void InlineStub_InitProperty_TracksAccess()
	{
		// Arrange
		var stub = new InitPropertyInlineTests.Stubs.IEntityWithInitProperty();
		stub.Id.Value = "tracked";

		// Act
		IEntityWithInitProperty entity = stub;
		_ = entity.Id;
		_ = entity.Id;

		// Assert
		Assert.Equal(2, stub.Id.GetCount);
	}
}

/// <summary>
/// Tests verifying that the explicit interface implementation uses init accessor.
/// These tests verify the generated code structure, not runtime behavior.
/// </summary>
public class InitPropertyGeneratedCodeTests
{
	[Fact]
	public void GeneratedCode_ExplicitImplementation_HasInitAccessor()
	{
		// This test verifies that the generated explicit interface implementation
		// uses 'init' accessor by checking that we can't set through the interface
		// after construction (without object initializer).

		// The actual verification is done by the compiler - if the generated code
		// used 'set' instead of 'init', this interface would accept assignment
		// at any time. With 'init', only object initializers work.

		// We verify the stub compiles, which proves the generated code has matching
		// accessors (init when source is init, set when source is set).
		var stub = new EntityWithInitPropertyKnockOff();
		Assert.NotNull(stub);

		// Can access through interface
		IEntityWithInitProperty entity = stub;
		Assert.NotNull(entity);
	}
}

// ===== Abstract Classes with Init Properties =====

public abstract class EntityBaseWithVirtualInit
{
	public virtual string Id { get; init; } = "";
}

public abstract class EntityBaseWithAbstractInit
{
	public abstract string Id { get; init; }
}

public abstract class EntityBaseWithMixedInit
{
	public virtual string Id { get; init; } = "";
	public virtual string Name { get; set; } = "";
	public abstract int Version { get; init; }
}

// ===== Abstract Classes with Required Properties =====

public abstract class EntityBaseWithRequiredProperty
{
	public required virtual string Id { get; set; }
}

public abstract class EntityBaseWithRequiredInit
{
	public required virtual string Id { get; init; }
}

public abstract class EntityBaseWithMultipleRequired
{
	public required virtual string Id { get; init; }
	public required virtual string Name { get; set; }
	public virtual int Version { get; set; }  // Not required
}

// ===== Inline Stubs for Class Testing =====

[KnockOff.KnockOff<EntityBaseWithVirtualInit>]
[KnockOff.KnockOff<EntityBaseWithAbstractInit>]
[KnockOff.KnockOff<EntityBaseWithMixedInit>]
[KnockOff.KnockOff<EntityBaseWithRequiredProperty>]
[KnockOff.KnockOff<EntityBaseWithRequiredInit>]
[KnockOff.KnockOff<EntityBaseWithMultipleRequired>]
public partial class ClassInitPropertyTests { }

/// <summary>
/// Tests for C# 9 init-only property support in class stubs.
/// </summary>
public class ClassInitPropertyStubTests
{
	[Fact]
	public void ClassStub_VirtualInitProperty_CanSetViaInterceptor()
	{
		// Arrange
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithVirtualInit();

		// Act - set via OnGet callback
		stub.Id.OnGet = _ => "test-id";

		// Assert
		Assert.Equal("test-id", stub.Object.Id);
	}

	[Fact]
	public void ClassStub_VirtualInitProperty_TracksAccess()
	{
		// Arrange
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithVirtualInit();

		// Act
		_ = stub.Object.Id;
		_ = stub.Object.Id;

		// Assert
		Assert.Equal(2, stub.Id.GetCount);
	}

	[Fact]
	public void ClassStub_VirtualInitProperty_DelegatesToBase()
	{
		// Arrange - without OnGet, should use base class default
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithVirtualInit();

		// Act
		var result = stub.Object.Id;

		// Assert - base class initializes to ""
		Assert.Equal("", result);
	}

	[Fact]
	public void ClassStub_AbstractInitProperty_ReturnsDefault()
	{
		// Arrange
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithAbstractInit();

		// Act - abstract has no base, returns default
		var result = stub.Object.Id;

		// Assert
		Assert.Equal(default, result);
	}

	[Fact]
	public void ClassStub_AbstractInitProperty_OnGetWorks()
	{
		// Arrange
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithAbstractInit();
		stub.Id.OnGet = _ => "abstract-id";

		// Act
		var result = stub.Object.Id;

		// Assert
		Assert.Equal("abstract-id", result);
	}

	[Fact]
	public void ClassStub_MixedInitProperties_AllWork()
	{
		// Arrange
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithMixedInit();
		stub.Id.OnGet = _ => "mixed-id";
		stub.Name.OnGet = _ => "mixed-name";
		stub.Version.OnGet = _ => 42;

		// Act & Assert
		Assert.Equal("mixed-id", stub.Object.Id);
		Assert.Equal("mixed-name", stub.Object.Name);
		Assert.Equal(42, stub.Object.Version);
	}

	[Fact]
	public void ClassStub_MixedInitProperties_SetPropertyCanBeSet()
	{
		// Arrange
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithMixedInit();

		// Act - Name is { get; set; } so can be set through object
		stub.Object.Name = "new-name";

		// Assert
		Assert.Equal("new-name", stub.Object.Name);
		Assert.Equal(1, stub.Name.SetCount);
	}
}

/// <summary>
/// Tests for C# 11 required property support in class stubs.
/// </summary>
public class ClassRequiredPropertyStubTests
{
	[Fact]
	public void ClassStub_RequiredProperty_StubCompiles()
	{
		// The key test is that the stub compiles at all.
		// If the generator didn't emit 'required' on the override,
		// we'd get CS9030: "override must be required because base is required"

		// This test verifies we can create the stub
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithRequiredProperty();
		Assert.NotNull(stub);
		Assert.NotNull(stub.Object);
	}

	[Fact]
	public void ClassStub_RequiredProperty_OnGetWorks()
	{
		// Arrange
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithRequiredProperty();
		stub.Id.OnGet = _ => "required-id";

		// Act
		var result = stub.Object.Id;

		// Assert
		Assert.Equal("required-id", result);
	}

	[Fact]
	public void ClassStub_RequiredProperty_CanSetThroughObject()
	{
		// Arrange
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithRequiredProperty();

		// Act - required { get; set; } can be set after construction
		stub.Object.Id = "set-after-construction";

		// Assert
		Assert.Equal("set-after-construction", stub.Object.Id);
		Assert.Equal(1, stub.Id.SetCount);
	}

	[Fact]
	public void ClassStub_RequiredInitProperty_StubCompiles()
	{
		// Tests required + init combination compiles
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithRequiredInit();
		Assert.NotNull(stub);
		Assert.NotNull(stub.Object);
	}

	[Fact]
	public void ClassStub_RequiredInitProperty_OnGetWorks()
	{
		// Arrange
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithRequiredInit();
		stub.Id.OnGet = _ => "required-init-id";

		// Act
		var result = stub.Object.Id;

		// Assert
		Assert.Equal("required-init-id", result);
	}

	[Fact]
	public void ClassStub_MultipleRequiredProperties_AllWork()
	{
		// Arrange
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithMultipleRequired();
		stub.Id.OnGet = _ => "multi-req-id";
		stub.Name.OnGet = _ => "multi-req-name";
		stub.Version.OnGet = _ => 99;

		// Act & Assert
		Assert.Equal("multi-req-id", stub.Object.Id);
		Assert.Equal("multi-req-name", stub.Object.Name);
		Assert.Equal(99, stub.Object.Version);
	}

	[Fact]
	public void ClassStub_MultipleRequiredProperties_TracksAccess()
	{
		// Arrange
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithMultipleRequired();

		// Act
		_ = stub.Object.Id;
		_ = stub.Object.Name;
		_ = stub.Object.Name;
		stub.Object.Name = "changed";
		stub.Object.Version = 1;

		// Assert
		Assert.Equal(1, stub.Id.GetCount);
		Assert.Equal(2, stub.Name.GetCount);
		Assert.Equal(1, stub.Name.SetCount);
		Assert.Equal(1, stub.Version.SetCount);
	}

	[Fact]
	public void ClassStub_RequiredProperty_ResetWorks()
	{
		// Arrange
		var stub = new ClassInitPropertyTests.Stubs.EntityBaseWithRequiredProperty();
		stub.Id.OnGet = _ => "test";
		_ = stub.Object.Id;

		// Act
		stub.Id.Reset();

		// Assert
		Assert.Equal(0, stub.Id.GetCount);
		Assert.Null(stub.Id.OnGet);
	}
}
