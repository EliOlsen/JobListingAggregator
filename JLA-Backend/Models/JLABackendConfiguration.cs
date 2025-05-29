namespace JLABackend.Models;
public class JLABackendConfiguration
{
    /// <summary>
    /// Gets or sets the user name for use with RabbitMQ
    /// </summary>
    public required string UserName { get; set; }
    /// <summary>
    /// Gets or sets the password for use with RabbitMQ
    /// </summary>
    public required string Password { get; set; }
    /// <summary>
    /// Gets or sets the host name for use with RabbitMQ
    /// </summary>
    public required string HostName { get; set; }
    /// <summary>
    /// Gets or sets the log exchange name for use with RabbitMQ
    /// </summary>
    public required string LogExchangeName { get; set; }
    /// <summary>
    /// Gets or sets the queue name for use with RabbitMQ
    /// </summary>
    public required string QueueName { get; set; }
    /// <summary>
    /// Gets or sets the user agent for HTTP requests
    /// </summary>
    public required string UserAgent { get; set; }
}