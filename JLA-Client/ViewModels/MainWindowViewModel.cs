using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System;
using JLAClient.Models;

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
    /// This command is used to add a new Rule to the List
    /// </summary>
    /// <param name="rule">the rule to add</param>
    [RelayCommand]
    public void AddRule(RuleViewModel rule)
    {
        // Add a new rule to the list
        Rules.Add(rule);
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
