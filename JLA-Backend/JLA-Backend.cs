using System.Text;
using JLABackend.Models;
using JLALibrary;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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

        string instanceId = Guid.NewGuid().ToString();
        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            UserName = settings.UserName,
            Password = settings.Password
        };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        //Set up Queue for remote tasks to be assigned in
        await channel.QueueDeclareAsync(queue: settings.QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
        //Start up the logging exchange, and send a preliminary log message upon initialization of program
        await channel.ExchangeDeclareAsync(exchange: settings.LogExchangeName, type: ExchangeType.Topic);
        string routingKeyBase = "Backend";
        await RMQLog.LogAsync(routingKeyBase + ".info", "Backend Initialized", instanceId, channel, settings.LogExchangeName);
        var consumer = new AsyncEventingBasicConsumer(channel);
        //Now, to set up the receivedAsync logic
        consumer.ReceivedAsync += async (object sender, BasicDeliverEventArgs ea) =>
        {
            AsyncEventingBasicConsumer cons = (AsyncEventingBasicConsumer)sender;
            IChannel ch = cons.Channel;
            string response = string.Empty;//We default to absolutely nothing.

            byte[] body = ea.Body.ToArray();
            IReadOnlyBasicProperties props = ea.BasicProperties;
            var replyProps = new BasicProperties
            {
                CorrelationId = props.CorrelationId
            };
            var message = "";
            await RMQLog.LogAsync(routingKeyBase + ".info", "Request received by Backend for data from source: " + Encoding.UTF8.GetString(body), instanceId, channel, settings.LogExchangeName);
            try
            {
                //Here is where I will actually do stuff with the incoming request, to formulate a response to send back.
            }
            catch (Exception e)
            {
                FormattedConsoleOuptut.Warning(e.Message);
                response = string.Empty; //This is already the default, but the try block may have altered it before hitting an exception so we'll set it here.
            }
            finally
            {
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!, mandatory: true, basicProperties: replyProps, body: responseBytes);
                await ch.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                await RMQLog.LogAsync(routingKeyBase + ".info", "Response sent by Backend about data from source: " + message + " on channel of " + ch, instanceId, channel, settings.LogExchangeName);
            }
        };
    }
}