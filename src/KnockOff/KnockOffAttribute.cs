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
