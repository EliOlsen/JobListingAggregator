using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JLAClient.Models;

interface IJobRequestService
{
    Task Initialize(Func<List<DisplayableJobListing>, bool, bool> ProcessingFunction, IEnumerable<ScheduleRule>? rules, DateTime? lastTimeListingsSaved, JLAClientConfiguration configuration);
}