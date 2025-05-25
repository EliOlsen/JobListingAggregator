namespace JLALibrary.Models;
public class GenericJobListing
{
    public required string Title { get; set; }
    public required string Company { get; set; }
    public required string JobsiteId { get; set; }
    public required string Location { get; set; }
    public required string PostDateTime { get; set; }
    public required string LinkToJobListing { get; set; }
}