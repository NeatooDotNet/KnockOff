namespace KnockOff;

/// <summary>
/// Marks a partial class for KnockOff source generation.
/// </summary>
/// <remarks>
/// <para>
/// <b>Standalone pattern:</b> Apply to a partial class that implements an interface.
/// The generator creates explicit interface implementations with invocation tracking.
/// </para>
/// <para>
/// <b>Open generic inline pattern:</b> Use with <c>typeof()</c> to generate inline stubs
/// for open generic types:
/// <code>
/// [KnockOff(typeof(IRepository&lt;&gt;))]
/// public partial class MyTests { }
/// // Generates: MyTests.Stubs.Repository&lt;T&gt;
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class KnockOffAttribute : Attribute
{
    /// <summary>
    /// Creates a KnockOff attribute for the standalone pattern.
    /// The class must implement at least one interface.
    /// </summary>
    public KnockOffAttribute()
    {
    }

    /// <summary>
    /// Creates a KnockOff attribute for open generic inline stubs.
    /// </summary>
    /// <param name="type">
    /// An open generic type using <c>typeof()</c> syntax.
    /// Supports interfaces, classes, and delegates.
    /// Example: <c>typeof(IRepository&lt;&gt;)</c>
    /// </param>
    public KnockOffAttribute(Type type)
    {
        Type = type;
    }

    /// <summary>
    /// The open generic type to stub, or null for standalone pattern.
    /// </summary>
    public Type? Type { get; }

    /// <summary>
    /// When true, unconfigured method calls throw <see cref="StubException"/> instead of returning default.
    /// This catches unexpected interactions during tests.
    /// </summary>
    /// <remarks>
    /// Sets the default value for the generated constructor's <c>strict</c> parameter.
    /// Can be overridden per-instance at construction time.
    /// </remarks>
    public bool Strict { get; set; }
}

/// <summary>
/// Generates an inline stub for the specified interface or delegate type.
/// Apply to a partial test class to create a nested Stubs class containing
/// stub implementations with full invocation tracking.
/// </summary>
/// <typeparam name="T">The interface or delegate type to stub.</typeparam>
/// <remarks>
/// <para>
/// Multiple <c>[KnockOff&lt;T&gt;]</c> attributes can be applied to create stubs for
/// multiple types. Each stub is accessible via <c>Stubs.{TypeName}</c>.
/// </para>
/// <para>
/// Optionally declare partial properties to get automatic instantiation:
/// <code>
/// [KnockOff&lt;IUserService&gt;]
/// public partial class MyTests
/// {
///     protected partial Stubs.IUserService userService { get; }
/// }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class KnockOffAttribute<T> : Attribute
{
    /// <summary>
    /// When true, unconfigured method calls throw <see cref="StubException"/> instead of returning default.
    /// This catches unexpected interactions during tests.
    /// </summary>
    /// <remarks>
    /// Sets the default value for the generated constructor's <c>strict</c> parameter.
    /// Can be overridden per-instance at construction time.
    /// </remarks>
    public bool Strict { get; set; }
}
