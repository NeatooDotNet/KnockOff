// -----------------------------------------------------------------------------
// Design.Domain - Delegate types for demonstrating inline delegate stubs
// -----------------------------------------------------------------------------

namespace Design.Domain.Delegates;

/// <summary>
/// A delegate representing an arithmetic operation.
/// Used to demonstrate inline delegate stubs with return values.
/// </summary>
public delegate int ArithmeticOperation(int a, int b);

/// <summary>
/// A delegate representing a logging action.
/// Used to demonstrate void delegate stubs.
/// </summary>
public delegate void LogAction(string message);

/// <summary>
/// A delegate representing a no-argument action.
/// Used to demonstrate void delegate stubs with no parameters.
/// </summary>
public delegate void SimpleAction();

/// <summary>
/// A generic factory delegate.
/// Used to demonstrate closed generic delegate stubs.
/// </summary>
public delegate T Factory<T>();
