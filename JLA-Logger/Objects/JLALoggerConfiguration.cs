namespace JLALogger;

public class JLALoggerConfiguration
{
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public required string HostName { get; set; }
    public required string LogExchangeName { get; set; }
    public required string LogFolderPath { get; set; }
    public required string[] LogQueueBindingKeys { get; set; }
}