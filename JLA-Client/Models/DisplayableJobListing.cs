using JLALibrary.Models;
namespace JLAClient.Models;

public class DisplayableJobListing()
{
    public required bool HasBeenViewed { get; set; }
    public required GenericJobListing Listing { get; set; }
}