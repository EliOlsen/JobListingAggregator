using System;
using JLALibrary.Models;
namespace JLAClient.Models;

public class ScheduleRule()
{
    /// <summary>
    /// Gets or sets the scheduled rule name
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// Gets or sets the interval at which this rule is meant to be triggered (in seconds, 0 indicating a one-time trigger upon startup)
    /// </summary>
    public required int Interval { get; set; }
    /// <summary>
    /// Gets or sets the daily start time before which the scheduled rule will not activate
    /// </summary>
    public required TimeSpan DailyStartTime { get; set; }
    /// <summary>
    /// Gets or sets the daily end time after which the scheduled rule will not activate
    /// </summary>
    public required TimeSpan DailyEndTime { get; set; }
    /// <summary>
    /// Gets or sets the request specifications the rule will send out when activated
    /// </summary>
    public required RequestSpecifications RequestSpecifications { get; set; }
}