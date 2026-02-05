namespace KnockOff;

/// <summary>
/// Marks a partial class as a standalone stub for a concrete class.
/// The stub can override virtual/abstract members with interceptor-based behavior.
/// </summary>
/// <remarks>
/// <para>
/// The user's class acts as a wrapper that holds interceptors with clean names.
/// A nested Impl class is generated that inherits from the target class and delegates to the wrapper.
/// </para>
/// <para>
/// Usage:
/// <code>
/// [KnockOffBase&lt;ServiceBase&gt;]
/// public partial class ServiceStub
/// {
///     // User can add custom methods, fields, etc.
/// }
/// </code>
/// Access the target class instance via <c>.Object</c>.
/// </para>
/// </remarks>
/// <typeparam name="T">The concrete class to stub. Must not be sealed.</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class KnockOffBaseAttribute<T> : Attribute where T : class
{
    /// <summary>
    /// When true, unconfigured method calls throw <see cref="StubException"/> instead of returning default.
    /// This catches unexpected interactions during tests.
    /// </summary>
    public bool Strict { get; set; }
}

/// <summary>
/// Marks a partial class as a standalone stub for an open generic class.
/// Use with <c>typeof()</c> syntax for open generic types.
/// </summary>
/// <remarks>
/// <para>
/// For open generic classes, use this attribute with typeof:
/// <code>
/// [KnockOffBase(typeof(RepositoryBase&lt;&gt;))]
/// public partial class RepositoryStub&lt;T&gt; where T : class
/// {
/// }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class KnockOffBaseAttribute : Attribute
{
    /// <summary>
    /// Creates a KnockOffBase attribute for open generic class stubs.
    /// </summary>
    /// <param name="type">
    /// An open generic type using <c>typeof()</c> syntax.
    /// Must be a class type (not interface or delegate).
    /// Example: <c>typeof(RepositoryBase&lt;&gt;)</c>
    /// </param>
    public KnockOffBaseAttribute(Type type)
    {
        Type = type;
    }

    /// <summary>
    /// The open generic class type to stub.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// When true, unconfigured method calls throw <see cref="StubException"/> instead of returning default.
    /// This catches unexpected interactions during tests.
    /// </summary>
    public bool Strict { get; set; }
}
