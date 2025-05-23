using System.Text;
using RabbitMQ.Client;
namespace JLALibrary;

public static class RMQLog
{
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