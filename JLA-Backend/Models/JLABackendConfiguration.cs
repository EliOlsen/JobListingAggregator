namespace JLABackend.Models;

public class JLABackendConfiguration
{
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public required string HostName { get; set; }
    public required string LogExchangeName { get; set; }
    public required string QueueName { get; set; }
}