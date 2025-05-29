using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using JLALibrary;
using JLAClient.Models;
using JLALibrary.Models;
namespace JLAClient.Services;
public class RabbitMQService : IAsyncDisposable
{
    //Establish the RabbitMQ variables needed for RabbitMQ operations
    private readonly IConnectionFactory _connectionFactory;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper = new();
    private IConnection? _connection;
    private IChannel? _channel;
    private string? _replyQueueName;
    public RabbitMQService(string hostName, string userName, string password)
    {
        _connectionFactory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password
        };
    }
    /// <summary>
    /// Establishes the RabbitMQ connections and sets up the asynchronous callbacks
    /// </summary>
    /// <param name="id">The id string assigned to this session</param>
    /// <param name="logExchangeName">The name of the exchange for log messages</param>
    public async Task StartAsync(string id, string logExchangeName)
    {
        _connection = await _connectionFactory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        QueueDeclareOk queueDeclareResult = await _channel.QueueDeclareAsync();
        _replyQueueName = queueDeclareResult.QueueName;
        var consumer = new AsyncEventingBasicConsumer(_channel);
        //Attach Functionality to respond to incoming messages asynchronously
        consumer.ReceivedAsync += (model, ea) =>
        {
            string? correlationId = ea.BasicProperties.CorrelationId;

            if (false == string.IsNullOrEmpty(correlationId))
            {
                if (_callbackMapper.TryRemove(correlationId, out var tcs))
                {
                    var body = ea.Body.ToArray();
                    var response = Encoding.UTF8.GetString(body);
                    tcs.TrySetResult(response);
                }
            }
            return Task.CompletedTask;
        };
        await _channel.BasicConsumeAsync(_replyQueueName, true, consumer);
        //Start up the logging exchange, and send a preliminary log message upon initialization of program
        await _channel.ExchangeDeclareAsync(exchange: logExchangeName, type: ExchangeType.Topic);
        await LogAsync("Client.info", "Client Initialized", id, logExchangeName);
    }
    /// <summary>
    /// Sends a log message through RabbitMQ
    /// </summary>
    /// <param name="routingKey">The key by which to route the message</param>
    /// <param name="message">The contents of the message</param>
    /// <param name="id">The id string assigned to this session</param>
    /// <param name="logExchangeName">The name of the exchange for log messages</param>
    public async Task LogAsync(string routingKey, string message, string id, string logExchangeName)
    {
        if (_channel is null)
        {
            throw new InvalidOperationException();
        }
        var body = Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd : HH:mm:ss.ffff") + " - " + message + " (" + id + ")");
        await _channel.BasicPublishAsync(exchange: logExchangeName, routingKey: routingKey, body: body);
    }
    /// <summary>
    /// Asynchronously calls through RabbitMQ (in our case, to send out scheduled rule requests and receive new job listings in response)
    /// </summary>
    /// <param name="queueName">The name of thw queue to send this message to</param>
    /// <param name="message">The contents of the message</param>
    /// <param name="cancellationToken">Optionally the cancellation token, if 'default' isn't the desired input</param>
    public async Task<string> CallAsync(string message, string queueName, CancellationToken cancellationToken = default)
    {
        //First, make sure channel is good
        if (_channel is null)
        {
            throw new InvalidOperationException();
        }
        //Generate correlation ID and put it in generated properties
        string correlationId = Guid.NewGuid().ToString();
        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = _replyQueueName
        };
        //Generate task completion source using correlation ID, and add to callback mapping
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _callbackMapper.TryAdd(correlationId, tcs);
        //All of that done, we can make and send out our message
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, mandatory: true, basicProperties: props, body: messageBytes);
        using CancellationTokenRegistration ctr =
            cancellationToken.Register(() =>
            {
                _callbackMapper.TryRemove(correlationId, out _);
                tcs.SetCanceled();
            });
        return await tcs.Task;
    }
    /// <summary>
    /// Disposes of _channel and _connection by asynchronously closing them if they are not null.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
        }
    }
}
public class MyRabbitMQ
{
    /// <summary>
    /// Initializes the RabbitMQ service with all of the values obtained from the config file.
    /// </summary>
    /// <param name="ProcessingFunction">The function to bring results into the _mainviewwindow</param>
    /// <param name="rules">The scheduleRules to initialize as repeating calls</param>
    /// <param name="lastTimeListingsSaved">The last time the job listings were saved to file, for use in single-trigger rules</param>
    /// <param name="configuration">The configuration values obtained from the config file</param>
    public static async Task Initialize(Func<List<DisplayableJobListing>, bool, bool> ProcessingFunction, IEnumerable<ScheduleRule>? rules, DateTime? lastTimeListingsSaved, JLAClientConfiguration configuration)
    {
        string instanceId = Guid.NewGuid().ToString();
        //Set up rabbitMQService so I can use it when needed (moved from InvokeAsync so it only happens once)
        var rabbitMQService = new RabbitMQService(configuration.HostName, configuration.Username, configuration.Password);
        await rabbitMQService.StartAsync(instanceId, configuration.LogExchangeName);
        //Read schedule rules from config file, and set each one running
        if (rules != null)
        {
            foreach (ScheduleRule rule in rules)
            {
                if (rule.Interval != 0)
                {
                    //This is a rule meant to run on a periodic basis, so set that up
                    rule.RequestSpecifications.CutoffTime = DateTime.Now.Subtract(new TimeSpan(0, 0, rule.Interval)); //Because this should be a relative time.
                    _ = Task.Run(() => PeriodicAsyncScheduledCall(//Do not await; doing so ensures that only the first rule will be implemented
                    rabbitMQService,
                    new TimeSpan(0, 0, rule.Interval),
                    instanceId,
                    ProcessingFunction,
                    configuration.LogExchangeName,
                    configuration.QueueName,
                    rule));
                }
                else
                {
                    //This is a rule meant to only run on startup, so skip the async and just call it
                    rule.RequestSpecifications.CutoffTime = lastTimeListingsSaved ?? DateTime.Now.Subtract(new TimeSpan(1, 0, 0)); //if we've got a last time saved, let's only request back to then
                    InvokeAsync(rabbitMQService, JsonSerializer.Serialize(rule.RequestSpecifications), instanceId, ProcessingFunction, configuration.LogExchangeName, configuration.QueueName);
                }
            }
        }
    }
    /// <summary>
    /// Sends out a RabbitMQ message and processes the result, when received, as well as logging the process
    /// </summary>
    /// <param name="rabbitMQService">The service to send out the call with</param>
    /// <param name="message">The serialized message</param>
    /// <param name="id">The unique session id</param>
    /// <param name="ProcessingFunction">The function to process our received listings with</param>
    /// <param name="logExchangeName">The name of the log exchange</param>
    /// <param name="queueName">The name of the RabbitMQ queue</param>
    private static async void InvokeAsync(RabbitMQService rabbitMQService, string message, string id, Func<List<DisplayableJobListing>, bool, bool> ProcessingFunction, string logExchangeName, string queueName)
    {
        await rabbitMQService.LogAsync("Client.info", "Request made by Client for data from source: " + message, id, logExchangeName); //Send log announcing request
        var response = await rabbitMQService.CallAsync(message, queueName); //Send request, await response
        await rabbitMQService.LogAsync("Client.info", "Response received by Client for data from source: " + message, id, logExchangeName);//Send log announcing received request
        List<GenericJobListing> incomingJobList = [];
        try
        {
            incomingJobList = JsonSerializer.Deserialize<List<GenericJobListing>>(response) ?? [];
        }
        catch (Exception e)
        {
            FormattedConsoleOutput.Warning("JsonSerializer failed to deserialize incoming response. Error message: " + e);
        }
        List<DisplayableJobListing> jobListings = [];
        foreach (GenericJobListing listing in incomingJobList)
        {
            jobListings.Add(new DisplayableJobListing
            {
                HasBeenViewed = false,
                Listing = listing
            });
        }
        //Finally, get them into the actual main window using the passed function
        ProcessingFunction(jobListings, true);
    }

    /// <summary>
    /// A wrapper function that periodically, per interval, makes the scheduled call and passes the data along
    /// </summary>
    /// <param name="rabbitMQService">The service to send out the call with</param>
    /// <param name="id">The unique session id</param>
    /// <param name="ProcessingFunction">The function to process our received listings with</param>
    /// <param name="logExchangeName">The name of the log exchange</param>
    /// <param name="queueName">The name of the RabbitMQ queue</param>
    /// <param name="interval">The time between calls of the rule</param>
    /// <param name="scheduleRule">The rule to periodically call</param>
    /// <param name="cancellationToken">Optionally the cancellation token, if 'default' isn't the desired value</param>
    private static async Task PeriodicAsyncScheduledCall(RabbitMQService rabbitMQService, TimeSpan interval, string id, Func<List<DisplayableJobListing>, bool, bool> ProcessingFunction, string logExchangeName, string queueName, ScheduleRule scheduleRule, CancellationToken cancellationToken = default)
    {
        using PeriodicTimer timer = new(interval);
        while (true)
        {
            ScheduledCall(rabbitMQService, id, ProcessingFunction, logExchangeName, queueName, scheduleRule);
            await timer.WaitForNextTickAsync(cancellationToken);
        }
    }
    /// <summary>
    /// the function that evaluates whether a call is taking place within requested hours, and if so, invokes the main call with the necessary data
    /// </summary>
    /// <param name="rabbitMQService">The service to send out the call with</param>
    /// <param name="id">The unique session id</param>
    /// <param name="ProcessingFunction">The function to process our received listings with</param>
    /// <param name="logExchangeName">The name of the log exchange</param>
    /// <param name="queueName">The name of the RabbitMQ queue</param>
    /// <param name="scheduleRule">The rule to  call</param>
    private static void ScheduledCall(RabbitMQService rabbitMQService, string id, Func<List<DisplayableJobListing>, bool, bool> ProcessingFunction, string logExchangeName, string queueName, ScheduleRule scheduleRule)
    {
        if (DateTime.Now >= DateTime.Now.Date.Add(scheduleRule.DailyStartTime) && DateTime.Now <= DateTime.Now.Date.Add(scheduleRule.DailyEndTime))
        {
            InvokeAsync(rabbitMQService, JsonSerializer.Serialize(scheduleRule.RequestSpecifications), id, ProcessingFunction, logExchangeName, queueName);
        }
    }
}