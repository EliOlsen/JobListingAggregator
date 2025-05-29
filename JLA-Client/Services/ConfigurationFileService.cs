using System;
using System.IO;
using System.Threading.Tasks;
using JLALibrary;
using JLAClient.Models;
namespace JLAClient.Services;
public class ConfigurationFileService()
{
    public static async Task<JLAClientConfiguration> RetrieveStartupConfiguration()
    {
        string configFilePath = Path.Combine(Environment.CurrentDirectory, "config.json"); //This IS hardcoded, and will remain so for now.
        JLAClientConfiguration DefaultConfiguration = new() //For use in creating a config file template if no valid config file is found, 
        // and for providing fallback values if config file is partially invalid
        {
            RulesSourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Avalonia.JLAClient", "rules.json"),
            ListingsSourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Avalonia.JLAClient", "listings.json"),
            AutosaveFrequencyInMilliseconds = 900000,
            QueueName = "scratchjobs_queue",
            Username = "guest", //The default RabbitMQ username upon installation of RabbitMQ - more likely to be right than a random placeholder.
            Password = "guest", //The default RabbitMQ username upon installation of RabbitMQ - more likely to be right than a random placeholder.
            HostName = "localhost",
            LogExchangeName = "scratchjobs_log"
        };
        JLAClientConfiguration settings = await new UserJsonConfiguration<JLAClientConfiguration>().RetrieveAndValidateSettings(DefaultConfiguration, configFilePath);
        return settings;
    }
}