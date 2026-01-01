using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KnockOff;

/// <summary>
/// Polyfill for HashCode (netstandard2.0 compatibility)
/// </summary>
internal struct HashCode
{
	private static readonly uint s_seed = GenerateGlobalSeed();

	private const uint Prime1 = 2654435761U;
	private const uint Prime2 = 2246822519U;
	private const uint Prime3 = 3266489917U;
	private const uint Prime4 = 668265263U;
	private const uint Prime5 = 374761393U;

	private uint _v1, _v2, _v3, _v4;
	private uint _queue1, _queue2, _queue3;
	private uint _length;

	private static uint GenerateGlobalSeed()
	{
		var buffer = new byte[sizeof(uint)];
#pragma warning disable RS1035
		new Random().NextBytes(buffer);
#pragma warning restore RS1035
		return BitConverter.ToUInt32(buffer, 0);
	}

	public static int Combine<T1>(T1 value1)
	{
		var hc1 = (uint)(value1?.GetHashCode() ?? 0);
		var hash = MixEmptyState();
		hash += 4;
		hash = QueueRound(hash, hc1);
		hash = MixFinal(hash);
		return (int)hash;
	}

	public static int Combine<T1, T2>(T1 value1, T2 value2)
	{
		var hc1 = (uint)(value1?.GetHashCode() ?? 0);
		var hc2 = (uint)(value2?.GetHashCode() ?? 0);
		var hash = MixEmptyState();
		hash += 8;
		hash = QueueRound(hash, hc1);
		hash = QueueRound(hash, hc2);
		hash = MixFinal(hash);
		return (int)hash;
	}

	public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
	{
		var hc1 = (uint)(value1?.GetHashCode() ?? 0);
		var hc2 = (uint)(value2?.GetHashCode() ?? 0);
		var hc3 = (uint)(value3?.GetHashCode() ?? 0);
		var hash = MixEmptyState();
		hash += 12;
		hash = QueueRound(hash, hc1);
		hash = QueueRound(hash, hc2);
		hash = QueueRound(hash, hc3);
		hash = MixFinal(hash);
		return (int)hash;
	}

	public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
	{
		var hc1 = (uint)(value1?.GetHashCode() ?? 0);
		var hc2 = (uint)(value2?.GetHashCode() ?? 0);
		var hc3 = (uint)(value3?.GetHashCode() ?? 0);
		var hc4 = (uint)(value4?.GetHashCode() ?? 0);
		Initialize(out var v1, out var v2, out var v3, out var v4);
		v1 = Round(v1, hc1);
		v2 = Round(v2, hc2);
		v3 = Round(v3, hc3);
		v4 = Round(v4, hc4);
		var hash = MixState(v1, v2, v3, v4);
		hash += 16;
		hash = MixFinal(hash);
		return (int)hash;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Initialize(out uint v1, out uint v2, out uint v3, out uint v4)
	{
		v1 = s_seed + Prime1 + Prime2;
		v2 = s_seed + Prime2;
		v3 = s_seed;
		v4 = s_seed - Prime1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint Round(uint hash, uint input)
	{
		return RotateLeft(hash + input * Prime2, 13) * Prime1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint QueueRound(uint hash, uint queuedValue)
	{
		return RotateLeft(hash + queuedValue * Prime3, 17) * Prime4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint MixState(uint v1, uint v2, uint v3, uint v4)
	{
		return RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
	}

	private static uint MixEmptyState()
	{
		return s_seed + Prime5;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint MixFinal(uint hash)
	{
		hash ^= hash >> 15;
		hash *= Prime2;
		hash ^= hash >> 13;
		hash *= Prime3;
		hash ^= hash >> 16;
		return hash;
	}

	public void Add<T>(T value)
	{
		Add(value?.GetHashCode() ?? 0);
	}

	public void Add<T>(T value, IEqualityComparer<T>? comparer)
	{
		Add(value is null ? 0 : (comparer?.GetHashCode(value) ?? value.GetHashCode()));
	}

	private void Add(int value)
	{
		var val = (uint)value;
		var previousLength = _length++;
		var position = previousLength % 4;

		if (position == 0)
			_queue1 = val;
		else if (position == 1)
			_queue2 = val;
		else if (position == 2)
			_queue3 = val;
		else
		{
			if (previousLength == 3)
				Initialize(out _v1, out _v2, out _v3, out _v4);

			_v1 = Round(_v1, _queue1);
			_v2 = Round(_v2, _queue2);
			_v3 = Round(_v3, _queue3);
			_v4 = Round(_v4, val);
		}
	}

	public int ToHashCode()
	{
		var length = _length;
		var position = length % 4;
		var hash = length < 4 ? MixEmptyState() : MixState(_v1, _v2, _v3, _v4);
		hash += length * 4;

		if (position > 0)
		{
			hash = QueueRound(hash, _queue1);
			if (position > 1)
			{
				hash = QueueRound(hash, _queue2);
				if (position > 2)
					hash = QueueRound(hash, _queue3);
			}
		}

		hash = MixFinal(hash);
		return (int)hash;
	}

#pragma warning disable 0809
	[Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes.", error: true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override int GetHashCode() => throw new NotSupportedException();

	[Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes.", error: true)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override bool Equals(object? obj) => throw new NotSupportedException();
#pragma warning restore 0809

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint RotateLeft(uint value, int offset)
		=> (value << offset) | (value >> (32 - offset));
}
