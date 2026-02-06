// -----------------------------------------------------------------------------
// Design.Domain - Interface hierarchy for demonstrating Source delegation
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Base read-only interface for Source hierarchy demonstration.
/// </summary>
public interface IReadableStore
{
	/// <summary>
	/// Gets a value by ID.
	/// </summary>
	string? GetById(int id);

	/// <summary>
	/// Gets the number of items.
	/// </summary>
	int Count { get; }
}

/// <summary>
/// Extended interface that inherits from IReadableStore.
///
/// This hierarchy demonstrates Source delegation with partial implementations:
/// - Source(IStore) delegates all members
/// - Source(IReadableStore) delegates only GetById and Count
/// </summary>
public interface IStore : IReadableStore
{
	/// <summary>
	/// Saves a value with the given ID.
	/// </summary>
	void Save(int id, string value);

	/// <summary>
	/// Deletes a value by ID.
	/// </summary>
	void Delete(int id);
}
