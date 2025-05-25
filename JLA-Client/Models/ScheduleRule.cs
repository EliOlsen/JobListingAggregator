using System;
using JLALibrary.Models;
namespace JLAClient.Models;

public class ScheduleRule()
{
    public required string Name { get; set; }
    public required int Interval { get; set; }
    public required TimeSpan DailyStartTime { get; set; }
    public required TimeSpan DailyEndTime { get; set; }
    public required RequestSpecifications Specifications { get; set; }
}