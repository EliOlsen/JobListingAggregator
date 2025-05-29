using JLALibrary.Models;
namespace JLAClient.Models;

public class DisplayableJobListing()
{
    /// <summary>
    /// Gets or sets the bool that records whether a given listing has been marked as viewed
    /// </summary>
    public required bool HasBeenViewed { get; set; }
    /// <summary>
    /// Gets or sets the actual Generic Job Listing within this wrapper class
    /// </summary>
    public required GenericJobListing Listing { get; set; }
}