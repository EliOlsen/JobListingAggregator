using CommunityToolkit.Mvvm.ComponentModel;
using JLAClient.Models;
using JLALibrary.Models;

namespace JLAClient.ViewModels;
/// <summary>
/// This is a ViewModel which represents a <see cref="DisplayableJobListing"/>
/// </summary>
public partial class ListingViewModel : ViewModelBase
{
    /// <summary>
    /// Gets or sets the viewed status of each listing
    /// </summary>
    [ObservableProperty]
    private bool _hasBeenViewed;

    /// <summary>
    /// Gets or sets the listing object
    /// </summary>
    [ObservableProperty]
    private GenericJobListing? _listing;


    /// <summary>
    /// Creates a new blank JobListingViewModel
    /// </summary>
    public ListingViewModel()
    {
        // empty
    }

    /// <summary>
    /// Creates a new JobListingViewModel for the given <see cref="DisplayableJobListing"/>
    /// </summary>
    /// <param name="DisplayableJobListing">The job listing to load</param>
    public ListingViewModel(DisplayableJobListing jobListing)
    {
        // Init the properties with the given values
        HasBeenViewed = jobListing.HasBeenViewed;
        Listing = jobListing.Listing;

    }
    /// <summary>
    /// Gets a JobListing of this ViewModel
    /// </summary>
    /// <returns>The JobListing</returns>
    public DisplayableJobListing GetJobListing()
    {
        return new DisplayableJobListing()
        {
            HasBeenViewed = this.HasBeenViewed,
            Listing = this.Listing,
        };
    }

    
}
