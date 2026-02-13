// -----------------------------------------------------------------------------
// Design.Domain - Interfaces with Default Interface Methods (DIMs)
// -----------------------------------------------------------------------------
// C# 8.0+ allows interfaces to provide method bodies as default implementations.
// When a member is unconfigured, the DIM should execute rather than returning
// default(T). These interfaces test all three DIM member types.
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Interface with a default method implementation.
/// GetPerimeter() has an implementation that depends on abstract members.
/// </summary>
public interface IDefaultMethodPolygon
{
    int NumberOfSides { get; }
    int SideLength { get; }

    double GetPerimeter() => this.SideLength * this.NumberOfSides;
}

/// <summary>
/// Interface with a default property implementation.
/// Perimeter has a getter body that depends on abstract members.
/// </summary>
public interface IDefaultPropertyPolygon
{
    int NumberOfSides { get; }
    int SideLength { get; }

    double Perimeter => this.SideLength * this.NumberOfSides;
}

/// <summary>
/// Interface with a default indexer implementation.
/// The indexer has a body that depends on an abstract member.
/// </summary>
public interface IDefaultIndexerPolygon
{
    int SideLength { get; }

    double this[int numberOfSides] => this.SideLength * numberOfSides;
}
