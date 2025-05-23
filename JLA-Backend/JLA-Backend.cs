using System.Text;
using System.Text.Json;
using Backend.Models;
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
            string response = string.Empty;//We default to an empty string

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
                //Step 1: associate each supposed job site with a function. This function will be the one to use to return data.
                //Some will go to parse the sites. Others will generate fallback dummy listings to link to sites. This dictionary controls which are which.
                //All functions will have to have the same parameters using this approach.
                //I know I want to return a list of job listings, but I don't know what I want the common parameters to be yet.
                Dictionary<Jobsite, Func<Task<List<GenericJobListing>>>> handlerDictionary = new()
                {
                    {Jobsite.LinkedIn, Placeholder}, //For now, since I don't have any of these functions, I'll leave them with a default option.
                    {Jobsite.BuiltIn, Placeholder},
                    {Jobsite.Dice, Placeholder},
                    {Jobsite.Indeed, Placeholder},
                    {Jobsite.Glassdoor, Placeholder}
                };
                //Now, having this setup, and expecting a RequestSpecification object in the message, I can pull out the expected Jobsite option.
                Jobsite jobsite = Jobsite.Error;
                RequestSpecifications? request = JsonSerializer.Deserialize<RequestSpecifications>(message);
                if (request is not null) Enum.TryParse(request.Source, true, out jobsite);
                //From that Jobsite, I can use a switch to perform the expected behavior. Also, so long as Jobsite isn't Error, we know Request isn't null.
                //establish variables for use within various switch branches
                List<GenericJobListing> listings = new List<GenericJobListing>();
                //Then switch!
                switch (jobsite)
                {
                    case Jobsite.Error:
                        //error - send nothing
                        FormattedConsoleOuptut.Warning("Request Jobsite enum does not parse. Returning empty list.");
                        response = JsonSerializer.Serialize(new List<GenericJobListing>());
                        break;

                    case Jobsite.Dummy:
                        //dummy - send some dummy data. This will never change, so I've not included it in the Dictionary and it gets its own case here.
                        listings =
                        [
                            new GenericJobListing{Title="dummytitle1", Company="dummycompany1", JobsiteId="dummy0001", LinkToJobListing="dummylinktojoblisting1", Location="dummylocation1", PostDateTime=DateTime.Now.ToString()},
                            new GenericJobListing{Title="dummytitle2", Company="dummycompany2", JobsiteId="dummy0002", LinkToJobListing="dummylinktojoblisting2", Location="dummylocation2", PostDateTime=DateTime.Now.ToString()},
                        ];
                        response = JsonSerializer.Serialize(listings);
                        break;

                    case Jobsite.All:
                        //all - combine the outputs of all of the normal jobsites
                        foreach (Jobsite js in Enum.GetValues(typeof(Jobsite)))
                        {
                            if (js != Jobsite.Error && js != Jobsite.Dummy && js != Jobsite.All)
                            {
                                listings.AddRange(await handlerDictionary[js]());
                            }
                        }
                        response = JsonSerializer.Serialize(listings);
                        break;

                    default:
                        //default - this is a normal jobsite, so call its handler function
                        listings = await handlerDictionary[jobsite]();
                        response = JsonSerializer.Serialize(listings);
                        break;
                }
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

    static async Task<List<GenericJobListing>> Placeholder()
    {
        return new List<GenericJobListing> { };
    }
}