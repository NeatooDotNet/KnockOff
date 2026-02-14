namespace Prototype.Stubs.Interfaces;

/// <summary>
/// Copy of Design.Domain.Entities.IMatrix for prototype use.
/// </summary>
public interface IMatrix
{
    double this[int row, int col] { get; set; }
    int Rows { get; }
    int Columns { get; }
}
