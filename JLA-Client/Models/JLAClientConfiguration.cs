namespace JLAClient.Models;

public class JLAClientConfiguration()
{
    /// <summary>
    /// Gets or sets the saved schedule rule source path
    /// </summary>
    public required string RulesSourcePath { get; set; }
    /// <summary>
    /// Gets or sets the saved job listing source path
    /// </summary>
    public required string ListingsSourcePath { get; set; }
    /// <summary>
    /// Gets or sets the autosave frequency (unit = milliseconds)
    /// </summary>
    public required double AutosaveFrequencyInMilliseconds { get; set; }
    ///<summary>
    /// Gets or sets the boolean indicating whether RabbitMQ should be used
    /// </summary>
    public required bool UseRabbitMQ { get; set; }
    /// <summary>
    /// Gets or sets the queue name for use with RabbitMQ
    /// </summary>
    public required string QueueName { get; set; }
    /// <summary>
    /// Gets or sets the user name for use with RabbitMQ
    /// </summary>
    public required string Username { get; set; }
    /// <summary>
    /// Gets or sets the password for use with RabbitMQ
    /// </summary>
    public required string Password { get; set; }
    /// <summary>
    /// Gets or sets the host name for use with RabbitMQ
    /// </summary>
    public required string HostName { get; set; }
    /// <summary>
    /// Gets or sets the exchange to use for sending log messages with RabbitMQ
    /// </summary>
    public required string LogExchangeName { get; set; }
    /// <summary>
    /// Gets or sets the user agent for HTTP requests
    /// </summary>
    public required string UserAgent { get; set; }
}