using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using JLAClient.Interfaces;
using JLAClient.Models;
using JLALibrary;
using JLALibrary.Models;
namespace JLAClient.Services;
public class HTTPRequestService : IJobRequestService
{
    public async Task Initialize(Func<List<DisplayableJobListing>, bool, bool> ProcessingFunction, IEnumerable<ScheduleRule>? rules, DateTime? lastTimeListingsSaved, JLAClientConfiguration configuration)
    {
        HttpClient client = new();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        client.DefaultRequestHeaders.Add("User-Agent", configuration.UserAgent);

        //Read schedule rules, set them running. Instead of calling RabbitMQ stuff, just call the backend functions in this file directly.
        if (rules != null)
        {
            foreach (ScheduleRule rule in rules)
            {
                if (rule.Interval != 0)
                {
                    //This is a rule meant to run on a periodic basis, so set that up
                    rule.RequestSpecifications.CutoffTime = DateTime.Now.Subtract(new TimeSpan(0, 0, rule.Interval)); //Because this should be a relative time.
                    _ = Task.Run(() => PeriodicAsyncScheduledCall(//Do not await; doing so ensures that only the first rule will be implemented
                    new TimeSpan(0, 0, rule.Interval),
                    ProcessingFunction,
                    rule,
                    client));
                }
                else
                {
                    //This is a rule meant to only run on startup, so skip the async and just call it
                    rule.RequestSpecifications.CutoffTime = lastTimeListingsSaved ?? DateTime.Now.Subtract(new TimeSpan(1, 0, 0)); //if we've got a last time saved, let's only request back to then
                    InvokeAsync(rule.RequestSpecifications, ProcessingFunction, client);
                }
            }
        }
    }
    /// <summary>
    /// A helper function that uses the request and client to procure jobs to put through the processing function
    /// </summary>
    /// <param name="ProcessingFunction">The function to process our received listings with</param>
    /// <param name="request">The details of the request being made</param>
    /// <param name="client">The HttpClient to use to make calls</param>
    private static async void InvokeAsync(RequestSpecifications request, Func<List<DisplayableJobListing>, bool, bool> ProcessingFunction, HttpClient client)
    {
        List<GenericJobListing> incomingJobList = await HTTPHandlers.ReceivedAsyncJobRequest(request, client);
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
    /// <param name="ProcessingFunction">The function to process our received listings with</param>
    /// <param name="interval">The time between calls of the rule</param>
    /// <param name="scheduleRule">The rule to periodically call</param>
    /// <param name="client">The HttpClient to use to make calls</param>
    /// <param name="cancellationToken">Optionally the cancellation token, if 'default' isn't the desired value</param>
    private static async Task PeriodicAsyncScheduledCall(TimeSpan interval, Func<List<DisplayableJobListing>, bool, bool> ProcessingFunction, ScheduleRule scheduleRule, HttpClient client, CancellationToken cancellationToken = default)
    {
        using PeriodicTimer timer = new(interval);
        while (true)
        {
            ScheduledCall(ProcessingFunction, scheduleRule, client);
            await timer.WaitForNextTickAsync(cancellationToken);
        }
    }
    /// <summary>
    /// the function that evaluates whether a call is taking place within requested hours, and if so, invokes the main call with the necessary data
    /// </summary>
    /// <param name="ProcessingFunction">The function to process our received listings with</param>
    /// <param name="scheduleRule">The rule to  call</param>
    /// <param name="client">The HttpClient to use to make calls</param>
    private static void ScheduledCall(Func<List<DisplayableJobListing>, bool, bool> ProcessingFunction, ScheduleRule scheduleRule, HttpClient client)
    {
        if (DateTime.Now >= DateTime.Now.Date.Add(scheduleRule.DailyStartTime) && DateTime.Now <= DateTime.Now.Date.Add(scheduleRule.DailyEndTime))
        {
            InvokeAsync(scheduleRule.RequestSpecifications, ProcessingFunction, client);
        }
    }
}