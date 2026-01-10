namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Interface with generic methods.
/// Used to measure generic method dispatch overhead.
/// </summary>
public interface IConverter
{
    T Convert<T>(object value);
    TOut Transform<TIn, TOut>(TIn input);
}
