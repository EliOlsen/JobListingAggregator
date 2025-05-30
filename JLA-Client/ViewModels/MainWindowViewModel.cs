using System;
using System.Linq;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JLAClient.Models;
using JLAClient.Converters;
using JLALibrary.Models;
namespace JLAClient.ViewModels;
public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Gets a collection of <see cref="DisplayableJobListing"/>, which allows adding and removing listings
    /// </summary>
    public ObservableCollection<ListingViewModel> Listings { get; } = [];
    /// <summary>
    /// This command is used to add a new ListingViewModel to the List
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
    /// Appends all DisplayableJobListings of a list that are not already in Listings, with a boolean whether to put them at the top or bottom of the existing list.
    /// </summary>
    /// <param name="newListings">the DisplayableJobListings to append if unique</param>
    /// /// <param name="pushToFront">the bool to determine whether new listings are inserted at index 0. If not, they are added to the end.</param>
    public bool AppendListings(IEnumerable<DisplayableJobListing> newListings, bool pushToFront)
    {
        foreach (DisplayableJobListing displayableListing in newListings)
        {   //Check that our listing id is not null, and that a listing with that id is not already present
            var firstOrDefaultListing = Listings.FirstOrDefault(a => a.GetDisplayableJobListing().Listing.JobsiteId == displayableListing.Listing.JobsiteId) ?? null;
            if (displayableListing.Listing.JobsiteId is not null && firstOrDefaultListing is null)
            {
                if (pushToFront) Listings.Insert(0, new ListingViewModel(displayableListing));
                else Listings.Add(new ListingViewModel(displayableListing));
            }
        }
        return true; //Could be void, but I'm passing this function around and in that case it's not allowed to have a void return type so far as I understand things.
    }
    /// <summary>
    /// Determines whether the expand the editor form resides within is expanded or not
    /// </summary>
    [ObservableProperty]
    public bool _isRuleEditorExpanded;
    /// <summary>
    /// Gets a collection of <see cref="ScheduleRule"/> which allows adding and removing scheduled rules
    /// </summary>
    public ObservableCollection<RuleViewModel> Rules { get; } = [];
    /// <summary>
    /// Gets or set the name for a new rule. If this string is not empty and unique, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleName;
    /// <summary>
    /// Gets or set the interval in seconds for a new rule. If this decimal is not null and positive, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private decimal? _newRuleInterval;
    // <summary>
    /// Gets or set the daily start time for a new rule. If this TimeSpan is not null, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private TimeSpan? _newRuleDailyStartTime;
    // <summary>
    /// Gets or set the daily end time for a new rule. If this TimeSpan is not null, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private TimeSpan? _newRuleDailyEndTime;
    /// <summary>
    /// Gets or set the jobsite source for a new rule. If this string equals one of the accepted values, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private string? _newRuleSource;
    /// <summary>
    /// List of acceptable jobsite source strings (taken from jobsite enum), for use in the dropdown.
    /// </summary>
    [ObservableProperty]
    private string[] _acceptedSources = [.. Enum.GetNames(typeof(Jobsite)).Where(x => x != "Error")];
    /// <summary>
    /// Gets or set the radius for a new rule. If this Decimal is not null and positive, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private decimal? _newRuleRadius;
    // <summary>
    /// Gets or set the isRemote bool for the new rule. (no need for validation, will always be true or false and either is valid)
    /// </summary>
    [ObservableProperty]
    private bool _newRuleIsRemote;
    /// <summary>
    /// Gets or set the search terms for a new rule. If this string is not null, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleSearchTerms;
    /// <summary>
    /// Gets or set the cultural string for a new rule. If this string is not null, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleCulture; //TODO: Make this a dropdown... with probably just the one option for now. It's dumb to write it out every time.
    /// <summary>
    /// Gets or set the city for a new rule. If this string is not null, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleCity;
    /// <summary>
    /// Gets or set the State for a new rule. If this string is not null, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleState; //I think it's out of the scope of this project to accomodate other geographical address schemas... for now. We'll stick to the US.
    //TODO: Convert this to an autocomplete dropdown. Misspelling a state is an entirely avoidable issue.
    /// <summary>
    /// Gets or set the State Abbreviation for a new rule. If this string is not null, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleStateAbbrev; //TODO: Remove this from visibility outright, and grab it via dictionary from the state variable.
    /// <summary>
    /// Gets or set the LinkedIn GeoId for a new rule. If this string is not null, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleGeoId; //TODO: Figure out what the heck this value actually means, and how to generate it programatically. Then remove it from visibility and do that instead.
    /// <summary>
    /// Gets or set the min salary for a new rule. If this Decimal is not null and positive, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private decimal? _newRuleMinSalary;//TODO: I KNOW for a fact that I'm only using this for BuiltIn on the backend, but that some of the other sites can take it to. Fix that. If I have it, I should use it.
    /// <summary>
    /// Gets or set the max salary for a new rule. If this Decimal is not null and positive, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))]
    private decimal? _newRuleMaxSalary;//TODO: I KNOW for a fact that I'm only using this for BuiltIn on the backend, but that some of the other sites can take it to. Fix that. If I have it, I should use it.
    /// <summary>
    /// Gets or set the BuiltIn Job Category for a new rule. If this string is not null, the AddOrUpdateRuleCommand will be enabled automatically
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOrUpdateRuleCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newRuleJobCategory;//TODO: Figure out how BuiltIn generates these, or at least assemble a list, and make them something other than typing in a string.
    /// <summary>
    /// Gets or set the company filter array for a new rule.
    /// </summary>
    [ObservableProperty]
    private string? _newRuleCompanyFilterArrayString; //TODO: I need a better way of having the user enter these.
    /// <summary>
    /// Gets or set the title filter array for a new rule.
    /// </summary>
    [ObservableProperty]
    private string? _newRuleTitleFilterArrayString; //TODO: I need a better way of having the user enter these.
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
    /// This command is used to add a new Rule to the List using the variables set by the form
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddOrUpdateRule))]
    private void AddOrUpdateRule()
    {
        RuleViewModel newRule = new(new ScheduleRule
        {
            Name = NewRuleName!,
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
                GeoId = NewRuleGeoId!,
                MaxSalary = decimal.ToInt32(NewRuleMaxSalary ?? decimal.Zero),
                MinSalary = decimal.ToInt32(NewRuleMinSalary ?? decimal.Zero),
                BuiltInJobCategory = NewRuleJobCategory!,
                CompanyFilterTerms = NewRuleCompanyFilterArrayString is not null ? NewRuleCompanyFilterArrayString!.Split("||") : [],
                TitleFilterTerms = NewRuleTitleFilterArrayString is not null ? NewRuleTitleFilterArrayString.Split("||") : []
            }
        });

        //If existing rule (as determined by name equality), replace. Else, add.
        if (Rules.Where(x => x.Name == newRule.Name).Any())
        {
            Rules[Rules.IndexOf(Rules.Where(x => x.Name == newRule.Name).First())] = newRule;
        }
        else
        {
            Rules.Add(newRule);
        }
        //Now reset the form's many fields.
        ClearForm();
    }
    /// <summary>
    /// This command is used to bring an existing rule's fields into the rule form
    /// </summary>
    /// <param name="rule">the rule to copy fields from</param>
    [RelayCommand]
    public void ImportRuleToForm(RuleViewModel rule)
    {
        NewRuleName = rule.Name;
        NewRuleInterval = rule.Interval;
        NewRuleDailyEndTime = rule.DailyEndTime;
        NewRuleDailyStartTime = rule.DailyStartTime;
        NewRuleSource = rule.RequestSpecifications!.Source;
        NewRuleRadius = rule.RequestSpecifications!.Radius;
        NewRuleSearchTerms = rule.RequestSpecifications!.SearchTerms;
        NewRuleCulture = rule.RequestSpecifications!.CultureInfoString;
        NewRuleIsRemote = rule.RequestSpecifications!.IsRemote;
        NewRuleCity = rule.RequestSpecifications!.City;
        NewRuleState = rule.RequestSpecifications!.State;
        NewRuleStateAbbrev = rule.RequestSpecifications!.StateAbbrev;
        NewRuleGeoId = rule.RequestSpecifications!.GeoId;
        NewRuleMaxSalary = rule.RequestSpecifications!.MaxSalary;
        NewRuleMinSalary = rule.RequestSpecifications!.MinSalary;
        NewRuleJobCategory = rule.RequestSpecifications!.BuiltInJobCategory;
        NewRuleCompanyFilterArrayString = (string?)new StringArrayToString().Convert(rule.RequestSpecifications!.CompanyFilterTerms, typeof(string), null, CultureInfo.CurrentCulture) ?? "";
        NewRuleTitleFilterArrayString = (string?)new StringArrayToString().Convert(rule.RequestSpecifications!.TitleFilterTerms, typeof(string), null, CultureInfo.CurrentCulture) ?? "";
        IsRuleEditorExpanded = true; //Automatically expand the form if it's not already expanded
    }
    /// <summary>
    /// This command is used to clear the form of all values
    /// </summary>
    [RelayCommand]
    public void ClearForm()
    {
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
        //Unlike with adding new rules during runtime, I don't care if two rules in the rule file have the same id. Keep the first, ignore the following copies
            var firstOrDefaultRule = Rules.FirstOrDefault(a => a.GetScheduleRule().Name == scheduleRule.Name) ?? null;
            if (scheduleRule.Name is not null && firstOrDefaultRule is null)
            {
                //Based on PushToFront, either add each new rule at the front of the list (inverting the given order of new rules, btw) or add at the end, preserving given order of new rules
                if (PushToFront) Rules.Insert(0, new RuleViewModel(scheduleRule));
                else Rules.Add(new RuleViewModel(scheduleRule));
            }
        }
        return true; //Could be void, but I'm passing this function around and in that case it's not allowed to have a void return type so far as I understand things.
    }
}