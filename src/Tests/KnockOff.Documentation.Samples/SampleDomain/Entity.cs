namespace KnockOff.Documentation.Samples.SampleDomain;

/// <summary>
/// Base entity class for generic repository examples.
/// </summary>
public abstract class Entity
{
    public int Id { get; set; }
}

/// <summary>
/// Product entity for repository examples.
/// </summary>
public class Product : Entity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

/// <summary>
/// Order entity for repository examples.
/// </summary>
public class Order : Entity
{
    public DateTime OrderDate { get; set; }
    public int CustomerId { get; set; }
    public decimal Total { get; set; }
}
