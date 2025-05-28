using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System;
using JLAClient.Models;
using System.Globalization;

namespace JLAClient.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Gets a collection of <see cref="DisplayableJobListing"/> which allows adding and removing listings
    /// </summary>
    public ObservableCollection<ListingViewModel> Listings { get; } = [];

    /// <summary>
    /// This command is used to add a new Listing to the List
    /// </summary>
    /// <param name="jobListing">the jobListing to add</param>
    [RelayCommand]
    public void AddListing(ListingViewModel jobListing)
    {
        // Add a new listing to the list
        Listings.Add(jobListing);
    }
    /// <summary>
    /// Removes the given Listing from the list
    /// </summary>
    /// <param name="jobListing">the jobListing to remove</param>
    [RelayCommand]
    public void RemoveListing(ListingViewModel jobListing)
    {
        // Remove the given listing from the list
        Listings.Remove(jobListing);
    }

    /// <summary>
    /// Removes all Listings from the list
    /// </summary>
    [RelayCommand]
    public void ClearListings()
    {
        Listings.Clear();
    }
    /// <summary>
    /// Appends all DisplayableJobListings of a list that are not already in Listings
    /// </summary>
    /// <param name="newListings">the DisplayableJobListings to append if unique</param>
    public bool AppendListings(IEnumerable<DisplayableJobListing> newListings, bool PushToFront)
    {
        foreach (DisplayableJobListing displayableListing in newListings)
        {   //Check that our listing id is not null, and that a listing with that id is not already present
            var firstOrDefaultListing = Listings.FirstOrDefault(a => a.GetDisplayableJobListing().Listing.JobsiteId == displayableListing.Listing.JobsiteId) ?? null;
            if (displayableListing.Listing.JobsiteId is not null && firstOrDefaultListing is null)
            {
                if (PushToFront) Listings.Insert(0, new ListingViewModel(displayableListing));
                else Listings.Add(new ListingViewModel(displayableListing));
            }
        }
        return true;
    }



    /// <summary>
    /// Gets a collection of <see cref="ScheduleRule"/> which allows adding and removing scheduled rules
    /// </summary>
    public ObservableCollection<RuleViewModel> Rules { get; } = [];

    /// <summary>
    /// Gets or set the name for a new rule. If this string is not empty and unique, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleName;
    /// <summary>
    /// Gets or set the interval in seconds for a new rule. If this string is not empty and parses to a non-negative int, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))]
    private string? _newRuleIntervalString;
    // <summary>
    /// Gets or set the daily start time for a new rule. If this string is not empty and parses to a timespan, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))]
    private string? _newRuleDailyStartTimeString;
    // <summary>
    /// Gets or set the daily end time for a new rule. If this string is not empty and parses to a timespan, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))]
    private string? _newRuleDailyEndTimeString;
    /// <summary>
    /// Gets or set the jobsite source for a new rule. If this string equals one of the accepted values, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))]
    private string? _newRuleSource;
    private string[] acceptedSources = ["dummy", "all", "linkedin", "builtin", "dice", "glassdoor", "indeed"];
    /// <summary>
    /// Gets or set the radius for a new rule. If this string is not empty and parses to a non-negative int, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))]
    private string? _newRuleRadiusString;
    // <summary>
    /// Gets or set the isRemote bool for the new rule.
    /// </summary>
    [ObservableProperty]
    private bool _newRuleIsRemote;
    /// <summary>
    /// Gets or set the search terms for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleSearchTerms;
    /// <summary>
    /// Gets or set the cultural string for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleCulture;
    /// <summary>
    /// Gets or set the city for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleCity;
    /// <summary>
    /// Gets or set the State for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleState;
    /// <summary>
    /// Gets or set the State Abbreviation for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleStateAbbrev;
    /// <summary>
    /// Gets or set the Longitude for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleLongitudeString;
    /// <summary>
    /// Gets or set the Latitude for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleLatitudeString;
    /// <summary>
    /// Gets or set the LinkedIn GeoId for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleGeoId;
    /// <summary>
    /// Gets or set the min salary for a new rule. If this string is not empty and parses to a non-negative int, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))]
    private string? _newRuleMinSalary;
    /// <summary>
    /// Gets or set the max salary for a new rule. If this string is not empty and parses to a non-negative int, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))]
    private string? _newRuleMaxSalary;
    /// <summary>
    /// Gets or set the BuiltIn Job Category for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleJobCategory;
    /// <summary>
    /// Gets or set the company filter array for a new rule.
    /// </summary>
    [ObservableProperty]
    private string? _newRuleCompanyFilterArrayString;
    /// <summary>
    /// Gets or set the title filter array for a new rule.
    /// </summary>
    [ObservableProperty]
    private string? _newRuleTitleFilterArrayString;


    /// <summary>
    /// Returns if a new Rule can be added. There are (many) validation requirements to check
    /// </summary>
    private bool CanAddRule() =>
        //Name validation
        !string.IsNullOrWhiteSpace(NewRuleName)
        && !Rules.Where(x => x.Name == NewRuleName).Any()
        //Interval validation
        && NewRuleIntervalString is not null
        && !NewRuleIntervalString.Contains('-')
        && int.TryParse(NewRuleIntervalString, out _)
        //Daily Start Time validation
        && NewRuleDailyStartTimeString is not null
        && TimeSpan.TryParse(NewRuleDailyStartTimeString, out _)
        //Daily End Time validation
        && NewRuleDailyEndTimeString is not null
        && TimeSpan.TryParse(NewRuleDailyEndTimeString, out _)
        //Source validation
        && NewRuleSource is not null
        && acceptedSources.Contains(NewRuleSource.ToLower())
        //Radius validation
        && NewRuleRadiusString is not null
        && !NewRuleRadiusString.Contains('-')
        && int.TryParse(NewRuleRadiusString, out _)
        //Search Terms validation
        && !string.IsNullOrWhiteSpace(NewRuleSearchTerms)
        //Culture validation
        && !string.IsNullOrWhiteSpace(NewRuleCulture)
        //City validation
        && !string.IsNullOrWhiteSpace(NewRuleCity)
        //State validation
        && !string.IsNullOrWhiteSpace(NewRuleState)
        //State Abbreviation validation
        && !string.IsNullOrWhiteSpace(NewRuleStateAbbrev)
        && NewRuleStateAbbrev.Length == 2
        //Longitude string validation
        && !string.IsNullOrWhiteSpace(NewRuleLongitudeString)
        && int.TryParse(NewRuleLongitudeString, out _)
        //Latitude string validation
        && !string.IsNullOrWhiteSpace(NewRuleLatitudeString)
        && int.TryParse(NewRuleLatitudeString, out _)
        //GeoId string validation
        && !string.IsNullOrWhiteSpace(NewRuleGeoId)
        //Min Salary validation
        && NewRuleMinSalary is not null
        && !NewRuleMinSalary.Contains('-')
        && int.TryParse(NewRuleMinSalary, out _)
        //Max Salary validation
        && NewRuleMaxSalary is not null
        && !NewRuleMaxSalary.Contains('-')
        && int.TryParse(NewRuleMaxSalary, out _)
        //BuiltIn Job Category validation
        && !string.IsNullOrWhiteSpace(NewRuleJobCategory)
        ;
    /// <summary>
    /// This command is used to add a new Rule to the List
    /// </summary>
    [RelayCommand (CanExecute = nameof(CanAddRule))]
    private void AddRule()
    {
        // Add a new item to the list
        Rules.Add(new RuleViewModel()
        {
            Name = NewRuleName,
            Interval = int.Parse(NewRuleIntervalString!),
            DailyStartTime = TimeSpan.Parse(NewRuleDailyStartTimeString!),
            DailyEndTime = TimeSpan.Parse(NewRuleDailyEndTimeString!),
            RequestSpecifications = new()
            {
                Source = NewRuleSource!,
                CutoffTime = DateTime.Now,
                IsRemote = NewRuleIsRemote,
                Radius = int.Parse(NewRuleRadiusString!),
                SearchTerms = NewRuleSearchTerms!,
                CultureInfoString = NewRuleCulture!,
                City = NewRuleCity!,
                State = NewRuleState!,
                StateAbbrev = NewRuleStateAbbrev!,
                Longitude = NewRuleLongitudeString!,
                Latitude = NewRuleLatitudeString!,
                GeoId = NewRuleGeoId!,
                MaxSalary = int.Parse(NewRuleMaxSalary!),
                MinSalary = int.Parse(NewRuleMinSalary!),
                BuiltInJobCategory = NewRuleJobCategory!,
                CompanyFilterTerms = NewRuleCompanyFilterArrayString!.Split("||"),
                TitleFilterTerms = NewRuleTitleFilterArrayString!.Split("||")
            }
            
        });

        // reset the NewRuleName
        NewRuleName = null;
    }



    /// <summary>
    /// Removes the given rule from the list
    /// </summary>
    /// <param name="rule">the rule to remove</param>
    [RelayCommand]
    public void RemoveRule(RuleViewModel rule)
    {
        // Remove the given rule from the list
        Rules.Remove(rule);
    }

    /// <summary>
    /// Removes all Rules from the list
    /// </summary>
    [RelayCommand]
    public void ClearRules()
    {
        Rules.Clear();
    }
    /// <summary>
    /// Appends all ScheduleRules of a list that are not already in Rules
    /// </summary>
    /// <param name="newRules">the ScheduleRules to append if unique</param>
    public bool AppendRules(IEnumerable<ScheduleRule> newRules, bool PushToFront)
    {
        foreach (ScheduleRule scheduleRule in newRules)
        {   //Check that our rule name is not null, and that a rule with that name is not already present
            var firstOrDefaultRule = Rules.FirstOrDefault(a => a.GetScheduleRule().Name == scheduleRule.Name) ?? null;
            if (scheduleRule.Name is not null && firstOrDefaultRule is null)
            {
                if (PushToFront) Rules.Insert(0, new RuleViewModel(scheduleRule));
                else Rules.Add(new RuleViewModel(scheduleRule));
            }
        }
        return true;
    }
}
