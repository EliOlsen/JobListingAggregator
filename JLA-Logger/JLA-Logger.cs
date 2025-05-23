using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using JLALibrary;

namespace JLALogger;

public class JLALogger
{
    public static async Task Main()
    {
        //Before anything else: Get configuration data from config file.
        string configFilePath = Path.Combine(Environment.CurrentDirectory, "config.json");
        JLALoggerConfiguration defaultConfiguration = new()
        {
            UserName = "guest", //RabbitMQ default username upon installation
            Password = "guest", //RabbitMQ default password upon installation
            HostName = "localhost",
            LogExchangeName = "scratchjobs_log",
            LogFolderPath = Path.Combine(Environment.CurrentDirectory, "logs"),
            LogQueueBindingKeys = ["#"]
        };
        if (!File.Exists(configFilePath))
        {
            //If file does not exist, make a default one and close program with error message
            OutputFormattedConsoleError("Error: configuration file not found. Generating default file at " + configFilePath + " and exiting program. Please edit config file to reflect desired configuration.");
            using (var fs = File.Create(configFilePath))
            {
                //serialize default configuration object into file for user to edit (set up inside a dictionary like so for proper format used when retrieving later)
                await JsonSerializer.SerializeAsync(fs, new Dictionary<string, JLALoggerConfiguration> { { defaultConfiguration.GetType().Name, defaultConfiguration } });
            }
            System.Environment.Exit(1);
        }
        //Knowing now that the configuration file does exist, we'll move on to building the configuration
        var configuration = new ConfigurationBuilder().AddJsonFile(configFilePath, optional: false, reloadOnChange: true).Build();
        JLALoggerConfiguration? settings = defaultConfiguration;
        try
        {
            settings = configuration.GetRequiredSection(defaultConfiguration.GetType().Name).Get<JLALoggerConfiguration>();
        }
        catch (InvalidOperationException e) //The file may exist, but it's always possible it's not formatted properly
        {
            OutputFormattedConsoleError("configuration file improperly formatted. Specifically: " + e.ToString());
            System.Environment.Exit(1);
        }
        //Just in case, let's ensure settings is always non-null and toss a warning if it wasn't somehow
        if (settings is null)
        {
            settings = defaultConfiguration;
            OutputFormattedConsoleError("Configuration file invalid; default configuration file in effect.");
        }
        //In the case where the user specifically did not provide one of the required values, this individual value defaults to a null.
        //Problematic. I want to replace nulls with the default values and throw a warning if so.
        // I COULD fix this generically by using reflection... but that's massive overkill for this project. 
        //On the other hand, fixing it by making a bunch of in-place fallbacks is too little effort.
        // I've added a function to the config class to correct and throw a non-stopping error to notify the user.
        if (settings.ReplaceEmptyStrings(defaultConfiguration))
        {
            OutputFormattedConsoleError("Configuration file missing one or more values; default value substituted for missing value.");
        }
        //At last, we have a verified good (or at least non-problematic) settings object with verfied non-problematic values. We're able to continue on.

        //Main functionality, getting log messages from RabbitMQ and putting them in a local file for user perusal.
        //First, set up RabbitMQ connection
        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            UserName = settings.UserName,
            Password = settings.Password
        };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(exchange: settings.LogExchangeName, type: ExchangeType.Topic);

        // declare a server-named queue
        QueueDeclareOk queueDeclareResult = await channel.QueueDeclareAsync();
        string queueName = queueDeclareResult.QueueName;

        //Bind each queue given in args
        foreach (string? bindingKey in settings.LogQueueBindingKeys)
        {
            await channel.QueueBindAsync(queue: queueName, exchange: settings.LogExchangeName, routingKey: bindingKey);
        }

        //Set up the log file for this session
        Directory.CreateDirectory(settings.LogFolderPath);
        StreamWriter sw = File.AppendText(Path.Combine(settings.LogFolderPath, DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss") + "_log.txt"));
        sw.AutoFlush = true;
        Console.SetError(sw);

        //Prep to actually do something with received messages
        var consumer = new AsyncEventingBasicConsumer(channel);
        //Actual functionality, currently writes to log file and console
        consumer.ReceivedAsync += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;
            Console.Error.WriteLine($"{routingKey} - {message}");
            Console.WriteLine($"{routingKey} - {message}");
            return Task.CompletedTask;
        };

        //The call to that functionality above
        await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
        //And the exit method for this command-line program
        Console.WriteLine(" [*] Waiting for messages.");
        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
    private static void OutputFormattedConsoleError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(" [.] " + message);
        Console.ForegroundColor = ConsoleColor.White;
    }
}