using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
namespace JLALibrary;
public class UserJsonConfiguration<T>
{
    /// <summary>
    /// Retrieves and validates the contents of a Json file at a given path, as an object of type T. 
    /// Uses the default object provided to create said file if it does not exist, and to fill in any missing values if it exists but is incomplete
    /// </summary>
    /// <param name="defaultConfiguration">An object of type T to serve as the default in case of missing or incomplete file</param>
    /// <param name="configFilePath">The path of the configuration file in question</param>
    public async Task<T> RetrieveAndValidateSettings(T defaultConfiguration, string configFilePath)
    {
        //First, let's ensure we didn't pass a null for default configuration.
        if (defaultConfiguration is null)
        {
            FormattedConsoleOutput.Warning("Default configuration is null! Returning that null, because this is a serious problem.");
            return defaultConfiguration; //Not sure I actually want to do this
        }
        if (!File.Exists(configFilePath))
        {
            //If file does not exist, make a default one and close program with error message
            FormattedConsoleOutput.Error("Error: configuration file not found. Generating default file at " + configFilePath + " and exiting program. Please edit config file to reflect desired configuration.");
            using (var fs = File.Create(configFilePath))
            {
                //serialize default configuration object into file for user to edit (set up inside a dictionary like so for proper format used when retrieving later)
                await JsonSerializer.SerializeAsync(fs, new Dictionary<string, T> { { defaultConfiguration.GetType().Name, defaultConfiguration } });
            }
            System.Environment.Exit(1);
        }
        //Knowing now that the configuration file does exist, we'll move on to building the configuration
        var configuration = new ConfigurationBuilder().AddJsonFile(configFilePath, optional: false, reloadOnChange: true).Build();
        T? settings = defaultConfiguration;
        try
        {
            settings = configuration.GetRequiredSection(defaultConfiguration.GetType().Name).Get<T>();
        }
        catch (InvalidOperationException e) //The file may exist, but it's always possible it's not formatted properly
        {
            FormattedConsoleOutput.Error("configuration file improperly formatted. Specifically: " + e.ToString());
            System.Environment.Exit(1);
        }
        //Just in case, let's ensure settings is always non-null and toss a warning if it wasn't somehow
        if (settings is null)
        {
            settings = defaultConfiguration;
            FormattedConsoleOutput.Warning("Configuration file invalid; default configuration file in effect.");
        }
        //In the case where the user specifically did not provide one of the required values, this individual value defaults to a null.
        //I want to replace nulls with the default values and throw a warning if so.
        foreach (PropertyInfo property in typeof(T).GetProperties())
        {
            if (property.GetValue(settings) is null)
            {
                property.SetValue(settings, property.GetValue(defaultConfiguration));
                FormattedConsoleOutput.Warning("Configuration file property invalid; default configuration file value for " + property.Name + " in effect.");
            }
        }
        return settings;
    }
}