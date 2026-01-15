using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KnockOff;

/// <summary>
/// An immutable, equatable array for incremental source generation.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
	where T : IEquatable<T>
{
	public static readonly EquatableArray<T> Empty = new(Array.Empty<T>());

	private readonly T[]? _array;

	public EquatableArray(T[] array)
	{
		_array = array;
	}

	public bool Equals(EquatableArray<T> array)
	{
		return AsSpan().SequenceEqual(array.AsSpan());
	}

	public override bool Equals(object? obj)
	{
		return obj is EquatableArray<T> array && Equals(array);
	}

	public override int GetHashCode()
	{
		if (_array is not T[] array)
		{
			return 0;
		}

		HashCode hashCode = default;

		foreach (var item in array)
		{
			hashCode.Add(item);
		}

		return hashCode.ToHashCode();
	}

	public ReadOnlySpan<T> AsSpan()
	{
		return _array.AsSpan();
	}

	public T[]? GetArray() => _array;

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return ((IEnumerable<T>)(_array ?? Array.Empty<T>())).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<T>)(_array ?? Array.Empty<T>())).GetEnumerator();
	}

	public int Count => _array?.Length ?? 0;

	public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

	public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}

/// <summary>
/// Extension methods for converting to EquatableArray.
/// </summary>
internal static class EquatableArrayExtensions
{
	/// <summary>
	/// Converts an enumerable to an EquatableArray.
	/// </summary>
	public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> source) where T : IEquatable<T>
	{
		return new EquatableArray<T>(source.ToArray());
	}
}
