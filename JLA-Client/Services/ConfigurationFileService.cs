using System;
using System.IO;
using JLALibrary;
using System.Threading.Tasks;
using JLAClient.Models;
namespace JLAClient.Services;

public class ConfigurationFileService()
{
    public static async Task<JLAClientConfiguration> RetrieveStartupConfiguration()
    {
        string configFilePath = Path.Combine(Environment.CurrentDirectory, "config.json");
        JLAClientConfiguration DefaultConfiguration = new()
        {
            RulesSourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Avalonia.JLAClient", "rules.json"),
            ListingsSourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Avalonia.JLAClient", "listings.json"),
            AutosaveFrequencyInMilliseconds = 900000,
            QueueName = "scratchjobs_queue",
            Username = "guest",
            Password = "guest",
            HostName = "localhost",
            LogExchangeName = "scratchjobs_log"
        };
        JLAClientConfiguration settings = await new UserJsonConfiguration<JLAClientConfiguration>().RetrieveAndValidateSettings(DefaultConfiguration, configFilePath);
        return settings;
    }
}