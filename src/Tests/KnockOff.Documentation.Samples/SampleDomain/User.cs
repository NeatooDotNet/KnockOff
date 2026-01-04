namespace KnockOff.Documentation.Samples.SampleDomain;

/// <summary>
/// User entity used in various examples.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
}
