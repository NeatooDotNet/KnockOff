namespace KnockOff;

/// <summary>
/// Marks a partial class for KnockOff source generation.
/// The class must implement at least one interface.
/// The generator will create explicit interface implementations
/// that track invocations for unit test verification.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class KnockOffAttribute : Attribute
{
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
}
