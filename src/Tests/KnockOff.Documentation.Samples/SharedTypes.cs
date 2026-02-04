namespace KnockOff.Documentation.Samples;

// =============================================================================
// Domain Types - Used across documentation samples
// =============================================================================

/// <summary>
/// Simple User entity for documentation examples.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Product entity for documentation examples.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

/// <summary>
/// Order entity for documentation examples.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event args for data-related events.
/// </summary>
public class DataEventArgs : EventArgs
{
    public string Data { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Custom exception for not found scenarios.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException() : base("Entity not found") { }
    public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// Configuration class with parameterless constructor for smart defaults testing.
/// </summary>
public class Config
{
    public string ConnectionString { get; set; } = "";
    public int Timeout { get; set; } = 30;
}
