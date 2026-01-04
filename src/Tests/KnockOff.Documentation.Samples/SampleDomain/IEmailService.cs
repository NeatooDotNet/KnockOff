namespace KnockOff.Documentation.Samples.SampleDomain;

/// <summary>
/// Email service interface used in Getting Started examples.
/// </summary>
public interface IEmailService
{
    void SendEmail(string to, string subject, string body);
    bool IsConnected { get; }
    bool IsValidAddress(string email);
}
