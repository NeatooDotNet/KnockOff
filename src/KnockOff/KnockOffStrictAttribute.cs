namespace KnockOff;

/// <summary>
/// Sets the default strict mode for all generated stubs in this assembly.
/// </summary>
/// <remarks>
/// <para>
/// When applied at assembly level, all KnockOff stubs in this assembly default to strict mode
/// (throwing <see cref="StubException"/> for unconfigured calls) unless overridden.
/// </para>
/// <para>
/// <b>Precedence (highest to lowest):</b>
/// <list type="number">
/// <item>Runtime: <c>stub.Strict = false</c> or <c>stub.Strict()</c></item>
/// <item>Constructor parameter (inline stubs): <c>new Stubs.IService(strict: false)</c></item>
/// <item>Attribute property: <c>[KnockOff(Strict = false)]</c> or <c>[KnockOff&lt;T&gt;(Strict = false)]</c></item>
/// <item>Assembly attribute: <c>[assembly: KnockOffStrict]</c></item>
/// <item>Default: <c>false</c></item>
/// </list>
/// </para>
/// <example>
/// <code>
/// [assembly: KnockOffStrict]
///
/// // All stubs default to strict mode:
/// [KnockOff&lt;IUserService&gt;]
/// public partial class MyTests { }
///
/// // Opt-out for specific stubs:
/// [KnockOff&lt;ILegacyService&gt;(Strict = false)]
/// public partial class LegacyTests { }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class KnockOffStrictAttribute : Attribute
{
}
