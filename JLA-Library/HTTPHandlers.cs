using System.Globalization;
using JLALibrary.Data;
using JLALibrary.Models;
namespace JLALibrary;
public static class HTTPHandlers
{
    /// <summary>
    /// The placeholder function with a handler-compatible signature, for when I have no method of dealing with a given jobsite. 
    /// Should go unused except for when I'm actively fixing / developing something.
    /// </summary>
    /// <param name="client">The HTTPClient to make calls with</param>
    /// <param name="jobsite">The jobsite enum I'm working with at the moment</param>
    /// <param name="url">The URL of the jobsite, pre-filled with its own parameters and ready to call</param>
    /// <param name="parseApproachDictionary">The dictionary I'll extract my parse approach data from, for parsing the HTTP call result</param>
    /// <param name="request">The original request object, which contains various bits of data I need to parse and format properly</param>
    public static Task<List<GenericJobListing>> Placeholder(HttpClient client, Jobsite jobsite, string url, Dictionary<string, List<ParseApproach>> parseApproachDictionary, RequestSpecifications request)
    {
        return Task.FromResult(new List<GenericJobListing> { });
    }
    /// <summary>
    /// The handler function for when I know a given jobsite won't allow my preferred approaches; 403 errors, forbidden, etc. 
    /// This function instead crafts a placeholder singular job listing to stand in and allow the client to still access the overall search.
    /// </summary>
    /// <param name="client">The HTTPClient to make calls with</param>
    /// <param name="jobsite">The jobsite enum I'm working with at the moment</param>
    /// <param name="url">The URL of the jobsite, pre-filled with its own parameters and ready to call</param>
    /// <param name="parseApproachDictionary">The dictionary I'll extract my parse approach data from, for parsing the HTTP call result</param>
    /// <param name="request">The original request object, which contains various bits of data I need to parse and format properly</param>
    public static Task<List<GenericJobListing>> GenerateProxyListing(HttpClient client, Jobsite jobsite, string url, Dictionary<string, List<ParseApproach>> parseApproachDictionary, RequestSpecifications request)
    {
        List<GenericJobListing> output = [];
        Random rnd = new Random();
        output.Add(new GenericJobListing
        {
            Title = jobsite.ToString() + " Jobs",
            Company = jobsite.ToString() + " - etc",
            JobsiteId = jobsite.ToString() + rnd.Next(0, 9999999), //I intend to use these ids to ensure uniqueness on the client side (compared against jobs already in memory). Since these proxy listings are meant to pop up every time they're called, here I give them sufficiently unique identifiers.
            Location = "None / various",
            PostDateTime = DateTime.Now.ToString(),
            LinkToJobListing = url
        }
        );
        return Task.FromResult(output);
    }
    /// <summary>
    /// The handler function for polling and parsing a jobsite's html for job listings.
    /// Specifically, this considers and parses a single page of said response.
    /// </summary>
    /// <param name="client">The HTTPClient to make calls with</param>
    /// <param name="jobsite">The jobsite enum I'm working with at the moment</param>
    /// <param name="url">The URL of the jobsite, pre-filled with its own parameters and ready to call</param>
    /// <param name="parseApproachDictionary">The dictionary I'll extract my parse approach data from, for parsing the HTTP call result</param>
    /// <param name="request">The original request object, which contains various bits of data I need to parse and format properly</param>
    public static async Task<List<GenericJobListing>> PollAndParseJobSiteForListings(HttpClient client, Jobsite jobsite, string url, Dictionary<string, List<ParseApproach>> parseApproachDictionary, RequestSpecifications request)
    {
        //Ensure that passed parameters are usable, before doing anything else.
        if (!parseApproachDictionary.ContainsKey("master"))
        {
            FormattedConsoleOutput.Warning("PollAndParseJobsiteForListings was not passed a master parseApproach and cannot process. Returning empty list.");
            return [];
        }
        //First step, which is to actually call the URL
        string rawHTML = await client.GetStringAsync(url);
        //Second step, break up the giant block of html into chunks, per listing
        List<string> brokenUpListings = [];
        for (int i = 0; i < parseApproachDictionary["master"].Count; i++)
        {
            ParseApproach currentApproach = parseApproachDictionary["master"][i];
            brokenUpListings = StringMunging.BreakStringIntoStringsOnStartAndEndSubstrings(rawHTML, currentApproach.PreSubstring, currentApproach.PostSubstring, currentApproach.KeepPreSubstring, currentApproach.KeepPostSubstring);
            if (brokenUpListings.Count > 0) break;
        }
        //if, having gone through the entire list, we still have nothing? toss out a warning and return empty list.
        if (brokenUpListings.Count < 1)
        {
            FormattedConsoleOutput.Warning("PollAndParseJobsiteForListings found no listings. Returning empty list.");
            return [];
        }
        //Here is where we know we're going to return SOMETHING.
        List<GenericJobListing> output = [];
        foreach (string listing in brokenUpListings)
        {
            //Third step, get the individual values
            string fallback = "ERROR";
            string fallbackId = "ERROR" + new Random().Next(0, 9999999); //Because I don't want id errors to register as the same error and then get discarded by frontend
            GenericJobListing job = new() //If I cannot parse one of these, I want the fallback value to display as error in client - better for me when using, so I can easily spot problems
            {
                Title = StringMunging.TryParseList(listing, "Title", fallback, parseApproachDictionary),
                Company = StringMunging.TryParseList(listing, "Company", fallback, parseApproachDictionary),
                JobsiteId = jobsite.ToString() + StringMunging.TryParseList(listing, "JobsiteId", fallbackId, parseApproachDictionary),
                Location = StringMunging.TryParseList(listing, "Location", fallback, parseApproachDictionary),
                PostDateTime = StringMunging.TryParseList(listing, "PostDateTime", fallback, parseApproachDictionary),
                LinkToJobListing = StringMunging.TryParseList(listing, "LinkToJobListing", fallback, parseApproachDictionary),
            };
            //Fourth step, filter listing based on request specifications beyond what is in the URL
            DateTime theoreticalJobPostTime = StringMunging.PostDateTimeEstimateFromVagueString(job.PostDateTime);
            job.PostDateTime = theoreticalJobPostTime.ToString();//Doing this in line was, honestly, a bunch of unnecessary conversions
            TimeSpan gracePeriod = new(1, 0, 0);// In an ideal world this would be 0, but right now I want it high to trend toward seeing mistakes, not missing mistakes.
            if (StringMunging.StringContainsNoneOfSubstringsInArray(job.Title, request.TitleFilterTerms)
            && Array.IndexOf(request.CompanyFilterTerms, job.Company) == -1
            && DateTime.Compare(theoreticalJobPostTime.Add(gracePeriod), request.CutoffTime) >= 0)
            {
                output.Add(job);
            }
        }
        return output;
    }
    /// <summary>
    /// The handler function for polling and parsing a jobsite's html for job listings.
    /// Specifically, this considers and parses a multiple pages, if the response indicates them.
    /// Currently only supports Dice.
    /// </summary>
    /// <param name="client">The HTTPClient to make calls with</param>
    /// <param name="jobsite">The jobsite enum I'm working with at the moment</param>
    /// <param name="url">The URL of the jobsite, pre-filled with its own parameters and ready to call</param>
    /// <param name="parseApproachDictionary">The dictionary I'll extract my parse approach data from, for parsing the HTTP call result</param>
    /// <param name="request">The original request object, which contains various bits of data I need to parse and format properly</param>
    public static async Task<List<GenericJobListing>> DicePaginatedPollAndParseJobSiteForListings(HttpClient client, Jobsite jobsite, string url, Dictionary<string, List<ParseApproach>> parseApproachDictionary, RequestSpecifications request)
    {//I'll fully genericize this later; LinkedIn and BuiltIn theoretically have multiple pages, but unlike Dice don't have the frequency of new listings to absolutely require parsing more than the first page...
        Random rnd = new Random();
        List<GenericJobListing> output = [];
        string rawHTMLPageOne = await client.GetStringAsync(url);
        string numPagesString = StringMunging.TryGetSubString(rawHTMLPageOne, "pageCount\\\":", ",", false, false);
        numPagesString = numPagesString == String.Empty ? "1" : numPagesString;
        int numberOfPages = int.Parse(numPagesString);
        for (int i = 1; i <= numberOfPages; i++)
        {
            string currentURL = url + $"&page={i}";
            output.AddRange(await PollAndParseJobSiteForListings(client, jobsite, currentURL, parseApproachDictionary, request));
            //Spacer to avoid spamming Dice with 10+ requests all at once - don't know if this is necessary, but it fits the design philosophy
            if (i != numberOfPages) Thread.Sleep(new TimeSpan(0, 0, rnd.Next(1, 5)));
        }
        return output;
    }
    /// <summary>
    /// The master function that takes the request and the client, determines which calls to make, makes the HTTP calls, and then uses the other handlers.
    /// </summary>
    /// <param name="request">The request specifications</param>
    /// <param name="client">The HttpClient</param>
    public static async Task<List<GenericJobListing>> ReceivedAsyncJobRequest(RequestSpecifications? request, HttpClient client)
    {
        //A dictionary relates each dictionary of properties and approaches to the jobsite they're meant for.
        Dictionary<Jobsite, Dictionary<string, List<ParseApproach>>>? parseByJobsite = DefaultParserApproach.GetData();
        //Establishing the function to enum relationship, another thing I only want to do once
        Dictionary<Jobsite, Func<HttpClient, Jobsite, string, Dictionary<string, List<ParseApproach>>, RequestSpecifications, Task<List<GenericJobListing>>>> handlerDictionary = DefaultJobsiteParserDictionary.GetData();
        List<GenericJobListing> listings = [];
        try
        {
            //Here is where I will actually do stuff with the incoming request, to formulate a response to send back.
            //Step 1: Having this setup, and expecting a RequestSpecification object in the message, I can pull out the expected Jobsite option.
            Jobsite jobsite = Jobsite.Error;
            if (request is not null) Enum.TryParse(request.Source, true, out jobsite);
            //From that Jobsite, I can use a switch to perform the expected behavior. Also, so long as Jobsite isn't Error, we know Request isn't null.
            //Before that, I need to establish variables for use within various switch branches
            RegionInfo regionInfo = new RegionInfo(new CultureInfo(request?.CultureInfoString is not null ? request.CultureInfoString : "en-US", false).LCID);
            Dictionary<Jobsite, string> urlDictionary = new Dictionary<Jobsite, string>//Needs further refinement, but will do for now.
                {//These are the links to the specific URLs I'll either be calling or using as placeholder listings.
                 //Not sure if I should shift these out to a data class. Probably not - proper interjection of request variables should mean the only time the actual format changes is when the URL format itself is changed, and that's something I'll have to maintain myself
                 //Note: Location is, uh, a crapshoot. Everything from city (easy) to 'geoid', which, what? I can obtain my own, personal location values by the same method I obtained these URLs, but that's a black box for the geoid.
                    {Jobsite.LinkedIn, $"https://www.linkedin.com/jobs/search/?distance={request!.Radius}&f_E=2%2C3&f_TPR=r86400&geoId={request.GeoId}&keywords={request.SearchTerms.Replace(" ", "%20")}&origin=JOB_SEARCH_PAGE_JOB_FILTER" },
                    {Jobsite.BuiltIn, $"https://builtin.com/jobs/remote/hybrid/office/{request.BuiltInJobCategory}/entry-level?search={request.SearchTerms.Replace(" ", "%20")}&daysSinceUpdated=1&city={request.City.Replace(" ", "%20")}&state={request.State.Replace(" ", "%20")}&country={regionInfo.ThreeLetterISORegionName}"},
                    {Jobsite.Dice, $"https://www.dice.com/platform/jobs?filters.postedDate=ONE&filters.employmentType=FULLTIME&filters.employerType=Direct+Hire&filters.workplaceTypes=Remote%7COn-Site%7CHybrid&radius={request.Radius}&countryCode={regionInfo.TwoLetterISORegionName}&latitude={request.Latitude}&location={request.City.Replace(" ", "+")}%2C+{request.StateAbbrev}%2C+{regionInfo.ThreeLetterISORegionName}&locationPrecision=City&longitude={request.Longitude}&q={request.SearchTerms.Replace(" ", "+")}&radiusUnit=mi"},
                    {Jobsite.Indeed, $"https://www.indeed.com/jobs?q={request.SearchTerms.Replace(" ", "+")}&l={request.City.ToLower().Replace(" ", "+")}%2C+{request.StateAbbrev.ToLower()}&sc=0kf%3Aexplvl%28ENTRY_LEVEL%29%3B&fromage=1&vjk=53ed07a6128717ad"},
                    {Jobsite.Glassdoor, $"https://www.glassdoor.com/Job/{request.City.ToLower().Replace(" ", "-")}-{request.StateAbbrev.ToLower().Replace(" ", "-")}-{request.SearchTerms.Replace(" ", "-")}-jobs-SRCH_IL.0,14_IC1142551_KO15,33.htm?maxSalary={request.MaxSalary}&minSalary={request.MinSalary}&fromAge=7"}
                    //TODO: Some of these URLS can be further expanded to use the variables from the others, and I might just want to see if LinkedIn can function without its GeoId altogether.
                };
            //Then switch!
            switch (jobsite)
            {
                case Jobsite.Error:
                    //error - send empty string
                    FormattedConsoleOutput.Warning("Request Jobsite enum does not parse.");
                    listings = [];
                    break;

                case Jobsite.Dummy:
                    //dummy - send some dummy data. This will never change, so I've not included it in the Dictionary and it gets its own case here.
                    listings =
                    [
                        new GenericJobListing{Title="dummytitle1", Company="dummycompany1", JobsiteId="dummy0001", LinkToJobListing="dummylinktojoblisting1", Location="dummylocation1", PostDateTime=DateTime.Now.ToString()},
                            new GenericJobListing{Title="dummytitle2", Company="dummycompany2", JobsiteId="dummy0002", LinkToJobListing="dummylinktojoblisting2", Location="dummylocation2", PostDateTime=DateTime.Now.ToString()},
                        ];
                    break;

                case Jobsite.All:
                    //all - combine the outputs of all of the normal jobsites
                    foreach (Jobsite js in Enum.GetValues(typeof(Jobsite)))
                    {
                        if (js != Jobsite.Error && js != Jobsite.Dummy && js != Jobsite.All)
                        {
                            listings.AddRange(await handlerDictionary[js](client, js, urlDictionary[js], parseByJobsite[js], request));
                        }
                    }
                    break;

                default:
                    //default - this is a normal jobsite, so call its handler function
                    listings = await handlerDictionary[jobsite](client, jobsite, urlDictionary[jobsite], parseByJobsite[jobsite], request);
                    break;
            }
        }
        catch (Exception e)
        {
            FormattedConsoleOutput.Warning("Error in the main switch: " + e);
            listings = []; //This is already the default, but the try block may have altered it before hitting an exception so we'll set it here.
        }
        return listings;
    }
}