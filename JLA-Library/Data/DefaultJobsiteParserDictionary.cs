using JLALibrary.Models;
namespace JLALibrary.Data;

public class DefaultJobsiteParserDictionary
{
    public static Dictionary<Jobsite, Func<HttpClient, Jobsite, string, Dictionary<string, List<ParseApproach>>, RequestSpecifications, Task<List<GenericJobListing>>>> GetData()
    {
        return new()
                {
                    {Jobsite.LinkedIn, HTTPHandlers.PollAndParseJobSiteForListings},
                    {Jobsite.BuiltIn, HTTPHandlers.PollAndParseJobSiteForListings},
                    {Jobsite.Dice, HTTPHandlers.DicePaginatedPollAndParseJobSiteForListings}, //Dice is tricky one; it tends to display an order of magnitude more listings than the others.
                    {Jobsite.Indeed, HTTPHandlers.GenerateProxyListing},
                    {Jobsite.Glassdoor, HTTPHandlers.GenerateProxyListing}
                };
    }
}