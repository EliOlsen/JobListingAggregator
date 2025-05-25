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
    public string Greeting { get; } = "Welcome to Avalonia!";
    /// <summary>
    /// Gets a collection of <see cref="DisplayableJobListing"/> which allows adding and removing listings
    /// </summary>
    public ObservableCollection<ListingViewModel> Listings { get; } = [];

    /// <summary>
    /// This command is used to add a new Listing to the List
    /// </summary>
    /// <param name="jobListing">the jobListing to add</param>
    [RelayCommand]
    private void AddListing(ListingViewModel jobListing)
    {
        // Add a new listing to the list
        Listings.Add(jobListing);
    }
    /// <summary>
    /// Removes the given Listing from the list
    /// </summary>
    /// <param name="jobListing">the jobListing to remove</param>
    [RelayCommand]
    private void RemoveListing(ListingViewModel jobListing)
    {
        // Remove the given listing from the list
        Listings.Remove(jobListing);
    }

    /// <summary>
    /// Removes all Listings from the list
    /// </summary>
    [RelayCommand]
    private void ClearListings()
    {
        Listings.Clear();
    }
    /// <summary>
    /// Appends all DisplayableJobListings of a list that are not already in Listings
    /// </summary>
    /// <param name="newListings">the DisplayableJobListings to append if unique</param>
    public bool AppendListings(List<DisplayableJobListing> newListings)
    {
        foreach (DisplayableJobListing displayableListing in newListings)
        {   //Check that our listing id is not null, and that a listing with that id is not already present
            var firstOrDefaultListing = Listings.FirstOrDefault(a => a.GetDisplayableJobListing().Listing.JobsiteId == displayableListing.Listing.JobsiteId) ?? null;
            if (displayableListing.Listing.JobsiteId is not null && firstOrDefaultListing is null)
            {
                Listings.Insert(0, new ListingViewModel(displayableListing));
            }
        }
        return true;
    }



    /// <summary>
    /// Gets a collection of <see cref="ScheduleRule"/> which allows adding and removing scheduled rules
    /// </summary>
    public ObservableCollection<RuleViewModel> Rules { get; } = [];

    /// <summary>
    /// This command is used to add a new Rules to the List
    /// </summary>
    /// <param name="rule">the rule to add</param>
    [RelayCommand]
    private void AddRule(RuleViewModel rule)
    {
        // Add a new rule to the list
        Rules.Add(rule);
    }
    /// <summary>
    /// Removes the given rule from the list
    /// </summary>
    /// <param name="rule">the rule to remove</param>
    [RelayCommand]
    private void RemoveRule(RuleViewModel rule)
    {
        // Remove the given rule from the list
        Rules.Remove(rule);
    }

    /// <summary>
    /// Removes all Rules from the list
    /// </summary>
    [RelayCommand]
    private void ClearRules()
    {
        Rules.Clear();
    }
    /// <summary>
    /// Appends all ScheduleRules of a list that are not already in Rules
    /// </summary>
    /// <param name="newRules">the ScheduleRules to append if unique</param>
    public bool AppendRules(List<ScheduleRule> newRules)
    {
        foreach (ScheduleRule scheduleRule in newRules)
        {   //Check that our rule name is not null, and that a rule with that name is not already present
            var firstOrDefaultRule = Rules.FirstOrDefault(a => a.GetScheduleRule().Name == scheduleRule.Name) ?? null;
            if (scheduleRule.Name is not null && firstOrDefaultRule is null)
            {
                Rules.Insert(0, new RuleViewModel(scheduleRule));
            }
        }
        return true;
    }
}
