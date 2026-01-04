namespace KnockOff.Documentation.Samples.SampleDomain;

/// <summary>
/// User service interface used in various examples.
/// </summary>
public interface IUserService
{
    string Name { get; set; }
    User GetUser(int id);
    User? GetUserOrNull(int id);
    void SaveUser(User user);
    Task<User?> GetUserAsync(int id);
    Task<User> GetUserRequiredAsync(int id);
    bool IsConnected { get; }
}
