// -----------------------------------------------------------------------------
// Design.Domain - Abstract base class for demonstrating inline class stubs
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

/// <summary>
/// Abstract base class used to demonstrate inline class stubbing.
///
/// DESIGN DECISION: Inline class stubs differ from interface stubs in that:
/// 1. The generated stub class extends this base class
/// 2. Unconfigured virtual methods call the base implementation (not smart default)
/// 3. Access the wrapped instance via .Object property
///
/// PATTERN COMPARISON:
/// - Interface stub: `new Stubs.ICalculator()` IS the implementation
/// - Class stub: `new Stubs.ServiceBase()` wraps the implementation; use .Object to get it
/// </summary>
public abstract class ServiceBase
{
    /// <summary>
    /// Gets the service name. Abstract - must be implemented by derived class.
    /// Used to demonstrate: Abstract property stubbing
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets or sets whether the service is enabled. Virtual with default implementation.
    /// Used to demonstrate: Virtual property with base fallback behavior
    /// </summary>
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Initializes the service. Virtual method with default implementation.
    /// Used to demonstrate: Virtual method stubbing with base fallback
    /// </summary>
    public virtual void Initialize()
    {
        // Default implementation - can be overridden in stub
    }

    /// <summary>
    /// Abstract method that must be implemented.
    /// Used to demonstrate: Abstract method stubbing
    /// </summary>
    public abstract void Execute(string command);
}
