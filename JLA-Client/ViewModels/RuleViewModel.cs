using System;
using CommunityToolkit.Mvvm.ComponentModel;
using JLALibrary.Models;
using JLAClient.Models;

namespace JLAClient.ViewModels;
/// <summary>
/// This is a ViewModel which represents a <see cref="ScheduleRule"/>
/// </summary>
public partial class RuleViewModel : ViewModelBase
{
    /// <summary>
    /// Gets or sets the name of each rule
    /// </summary>
    [ObservableProperty]
    private string? _name;
    /// <summary>
    /// Gets or sets the interval of each rule
    /// </summary>
    [ObservableProperty]
    private int _interval;
    /// <summary>
    /// Gets or sets the daily start time of each rule
    /// </summary>
    [ObservableProperty]
    private TimeSpan _dailyStartTime;
    /// <summary>
    /// Gets or sets the daily end time of each rule
    /// </summary>
    [ObservableProperty]
    private TimeSpan _dailyEndTime;
    /// <summary>
    /// Gets or sets the request specifications object
    /// </summary>
    [ObservableProperty]
    private RequestSpecifications? _requestSpecifications;
    /// <summary>
    /// Creates a new RuleViewModel for the given <see cref="ScheduleRule"/>
    /// </summary>
    /// <param name="ScheduleRule">The rule to load</param>
    public RuleViewModel(ScheduleRule scheduleRule)
    {
        // Init the properties with the given values
        Name = scheduleRule.Name;
        Interval = scheduleRule.Interval;
        DailyStartTime = scheduleRule.DailyStartTime;
        DailyEndTime = scheduleRule.DailyEndTime;
        RequestSpecifications = scheduleRule.RequestSpecifications;
    }
    /// <summary>
    /// Gets the ScheduleRule of this ViewModel
    /// </summary>
    /// <returns>The ScheduleRule</returns>
    public ScheduleRule GetScheduleRule()
    {
        return new ScheduleRule()
        {
            Name = this.Name!,
            Interval = this.Interval,
            DailyStartTime = this.DailyStartTime,
            DailyEndTime = this.DailyEndTime,
            RequestSpecifications = this.RequestSpecifications!
        };
    }  
}