using System.Text;
using RabbitMQ.Client;
namespace JLALibrary;
public static class RMQLog
{
    /// <summary>
    /// Sends out a RabbitMQ message on a given logexchange, using a given routing key.
    /// </summary>
    /// <param name="message">The contents of the message to send via RabbitMQ</param>
    ///  <param name="routingKey">The key to route the message with, within RabbitMQ</param>
    ///  <param name="id">The session id, to append to the message so that logs can be distinguished by souce even if there are multiple instances of the same program running</param>
    ///  <param name="channel">The established RabbitMQ channel to send the message with</param>
    ///  <param name="logExchangeName">The name of the log exchange to send the message to</param>
    public static async Task LogAsync(string routingKey, string message, string id, IChannel channel, string logExchangeName)
    {
        if (channel is null)
        {
            throw new InvalidOperationException();
        }
        var body = Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd : HH:mm:ss.ffff") + " - " + message + " (" + id + ")");
        await channel.BasicPublishAsync(exchange: logExchangeName, routingKey: routingKey, body: body);
        Console.WriteLine($" [x] Sent Log '{routingKey}':'{message}'");
    }
}