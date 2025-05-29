using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JLAClient.Interfaces;
using JLAClient.Models;

namespace JLAClient.Services;

public class HTTPRequestService : IJobRequestService
{
    public Task Initialize(Func<List<DisplayableJobListing>, bool, bool> ProcessingFunction, IEnumerable<ScheduleRule>? rules, DateTime? lastTimeListingsSaved, JLAClientConfiguration configuration)
    {
        return Task.CompletedTask;
    }
}