using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System;
using JLAClient.Models;
using System.Globalization;
using Microsoft.VisualBasic;

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
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleName;
    /// <summary>
    /// Gets or set the interval in seconds for a new rule. If this decimal is not null and positive, the AddRuleCommand will be enabled automatically (with rounding)
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private decimal? _newRuleInterval;
    // <summary>
    /// Gets or set the daily start time for a new rule. If this TimeSpan is not null, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private TimeSpan? _newRuleDailyStartTime;
    // <summary>
    /// Gets or set the daily end time for a new rule. If this TimeSpan is not null, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private TimeSpan? _newRuleDailyEndTime;
    /// <summary>
    /// Gets or set the jobsite source for a new rule. If this string equals one of the accepted values, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private string? _newRuleSource;
    /// <summary>
    /// Hardcoded list of acceptable source strings, for use in the dropdown.
    /// </summary>
    [ObservableProperty]
    private string[] _acceptedSources = ["dummy", "all", "linkedin", "builtin", "dice", "glassdoor", "indeed"];
    /// <summary>
    /// Gets or set the radius for a new rule. If this Decimal is not null and positive, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private decimal? _newRuleRadius;
    // <summary>
    /// Gets or set the isRemote bool for the new rule.
    /// </summary>
    [ObservableProperty]
    private bool _newRuleIsRemote;
    /// <summary>
    /// Gets or set the search terms for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleSearchTerms;
    /// <summary>
    /// Gets or set the cultural string for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleCulture;
    /// <summary>
    /// Gets or set the city for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleCity;
    /// <summary>
    /// Gets or set the State for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleState;
    /// <summary>
    /// Gets or set the State Abbreviation for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleStateAbbrev;
    /// <summary>
    /// Gets or set the Longitude for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleLongitudeString;
    /// <summary>
    /// Gets or set the Latitude for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleLatitudeString;
    /// <summary>
    /// Gets or set the LinkedIn GeoId for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleGeoId;
    /// <summary>
    /// Gets or set the min salary for a new rule. If this Decimal is not null and positive, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private decimal? _newRuleMinSalary;
    /// <summary>
    /// Gets or set the max salary for a new rule. If this Decimal is not null and positive, the AddRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private decimal? _newRuleMaxSalary;
    /// <summary>
    /// Gets or set the BuiltIn Job Category for a new rule.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
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
    private bool CanAddOrUpdateRule()
    {
        return
        //Name validation
        !string.IsNullOrWhiteSpace(NewRuleName)
        //Interval validation
        && NewRuleInterval is not null
        && NewRuleInterval >= 0
        //Daily Start Time validation
        && NewRuleDailyStartTime is not null
        //Daily End Time validation
        && NewRuleDailyEndTime is not null
        //Source validation
        && NewRuleSource is not null
        //Radius validation
        && NewRuleRadius is not null
        && NewRuleRadius > 0
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
        && NewRuleMinSalary >= 0
        //Max Salary validation
        && NewRuleMaxSalary is not null
        && NewRuleMaxSalary >= 0
        //BuiltIn Job Category validation
        && !string.IsNullOrWhiteSpace(NewRuleJobCategory);
    }
    /// <summary>
    /// This command is used to add a new Rule to the List
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddOrUpdateRule))]
    private void AddOrUpdateRule()
    {
        RuleViewModel newRule = new()
        {
            Name = NewRuleName,
            Interval = decimal.ToInt32(NewRuleInterval ?? decimal.Zero),
            DailyStartTime = NewRuleDailyStartTime ?? TimeSpan.Zero,
            DailyEndTime = NewRuleDailyEndTime ?? TimeSpan.Zero,
            RequestSpecifications = new()
            {
                Source = NewRuleSource!,
                CutoffTime = DateTime.Now,
                IsRemote = NewRuleIsRemote,
                Radius = decimal.ToInt32(NewRuleRadius ?? decimal.Zero),
                SearchTerms = NewRuleSearchTerms!,
                CultureInfoString = NewRuleCulture!,
                City = NewRuleCity!,
                State = NewRuleState!,
                StateAbbrev = NewRuleStateAbbrev!,
                Longitude = NewRuleLongitudeString!,
                Latitude = NewRuleLatitudeString!,
                GeoId = NewRuleGeoId!,
                MaxSalary = decimal.ToInt32(NewRuleMaxSalary ?? decimal.Zero),
                MinSalary = decimal.ToInt32(NewRuleMinSalary ?? decimal.Zero),
                BuiltInJobCategory = NewRuleJobCategory!,
                CompanyFilterTerms = NewRuleCompanyFilterArrayString is not null ? NewRuleCompanyFilterArrayString!.Split("||") : [],
                TitleFilterTerms = NewRuleTitleFilterArrayString is not null ? NewRuleTitleFilterArrayString.Split("||") : []
            }

        };
        //If existing rule (as determined by name equality) replace. Else, add.
        if (Rules.Where(x => x.Name == newRule.Name).Any())
        {
            Rules[Rules.IndexOf(Rules.Where(x => x.Name == newRule.Name).First())] = newRule;
        }
        else
        {
            Rules.Add(newRule);
        }

        // reset the fields
        NewRuleName = null;
        NewRuleInterval = null;
        NewRuleDailyEndTime = null;
        NewRuleDailyStartTime = null;
        NewRuleSource = null;
        NewRuleRadius = null;
        NewRuleSearchTerms = null;
        NewRuleCulture = null;
        NewRuleIsRemote = false;
        NewRuleCity = null;
        NewRuleState = null;
        NewRuleStateAbbrev = null;
        NewRuleLongitudeString = null;
        NewRuleLatitudeString = null;
        NewRuleGeoId = null;
        NewRuleMaxSalary = null;
        NewRuleMinSalary = null;
        NewRuleJobCategory = null;
        NewRuleCompanyFilterArrayString = null;
        NewRuleTitleFilterArrayString = null;

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
