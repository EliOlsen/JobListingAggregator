using System.Text;
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
        JLALoggerConfiguration DefaultConfiguration = new()
        {
            UserName = "guest", //RabbitMQ default username upon installation
            Password = "guest", //RabbitMQ default password upon installation
            HostName = "localhost",
            LogExchangeName = "scratchjobs_log",
            LogFolderPath = Path.Combine(Environment.CurrentDirectory, "logs"),
            LogQueueBindingKeys = ["#"]
        };
        JLALoggerConfiguration settings = await new UserJsonConfiguration<JLALoggerConfiguration>().RetrieveAndValidateSettings(DefaultConfiguration, configFilePath);
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
}