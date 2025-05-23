using JLABackend.Models;
using JLALibrary;

namespace JLABackend;

public class JLABackend
{
    public static async Task Main()
    {
        //Before anything else: Get configuration data from config file.
        string configFilePath = Path.Combine(Environment.CurrentDirectory, "config.json");
        JLABackendConfiguration DefaultConfiguration = new()
        {
            UserName = "guest", //RabbitMQ default username upon installation
            Password = "guest", //RabbitMQ default password upon installation
            HostName = "localhost",
            LogExchangeName = "scratchjobs_log",
            QueueName = "scratchjobs_queue"
        };
        JLABackendConfiguration settings = await new UserJsonConfiguration<JLABackendConfiguration>().RetrieveAndValidateSettings(DefaultConfiguration, configFilePath);
        //Configuration acquired. Now, to establish the RabbitMQ setup we need.
    }
}