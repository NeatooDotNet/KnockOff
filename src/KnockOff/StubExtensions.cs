namespace KnockOff;

/// <summary>
/// Extension methods for KnockOff stubs.
/// </summary>
public static class StubExtensions
{
    /// <summary>
    /// Enables strict mode on the stub. Unconfigured method calls will throw <see cref="StubException"/>.
    /// </summary>
    /// <typeparam name="T">The stub type.</typeparam>
    /// <param name="stub">The stub instance.</param>
    /// <returns>The same stub instance for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// var stub = new UserServiceStub().Strict();
    /// </code>
    /// </example>
    public static T Strict<T>(this T stub) where T : IKnockOffStub
    {
        stub.Strict = true;
        return stub;
    }
}
