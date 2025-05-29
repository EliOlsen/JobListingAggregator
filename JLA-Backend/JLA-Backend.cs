using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using JLALibrary;
using JLALibrary.Models;
using JLABackend.Models;
using JLABackend.Data;
namespace JLABackend;
public class JLABackend
{
    /// <summary>
    /// The man function, this is the backbone of the Backend.
    /// </summary>
    public static async Task Main()
    {
        //Before anything else: Get configuration data from config file.
        string configFilePath = Path.Combine(Environment.CurrentDirectory, "config.json");
        JLABackendConfiguration DefaultConfiguration = new()
        {
            UserName = "guest", //RabbitMQ default username upon installation
            Password = "guest", //RabbitMQ default password upon installation
            HostName = "localhost",
            LogExchangeName = "scratchjobs_log",
            QueueName = "scratchjobs_queue",
            UserAgent = "??"
        };
        JLABackendConfiguration settings = await new UserJsonConfiguration<JLABackendConfiguration>().RetrieveAndValidateSettings(DefaultConfiguration, configFilePath);
        //Configuration acquired. Establishing the default HTTP information is next
        HttpClient client = new();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        client.DefaultRequestHeaders.Add("User-Agent", settings.UserAgent); //Haven't decided exactly how I'm formatting this yet.
        //A ParseApproach is a set of parameters to feed to StringMunging.TryGetSubstring along with the input
        //A list of ParseApproach is the order in which they should be tried, flowing down to the next if the previous didn't give a valid output, and only submitting an empty string if all fail
        //A dictionary relates each list of ParseApproaches with the property they're meant for
        //A dictionary relates each dictionary of properties and approaches to the jobsite they're meant for.
        Dictionary<Jobsite, Dictionary<string, List<ParseApproach>>>? parseByJobsite = DefaultParserApproach.GetDefault();
        //Establishing the function to enum relationship, another thing I only want to do once
        Dictionary<Jobsite, Func<HttpClient, Jobsite, string, Dictionary<string, List<ParseApproach>>, RequestSpecifications, Task<List<GenericJobListing>>>> handlerDictionary = new()
                {
                    {Jobsite.LinkedIn, PollAndParseJobSiteForListings},
                    {Jobsite.BuiltIn, PollAndParseJobSiteForListings},
                    {Jobsite.Dice, DicePaginatedPollAndParseJobSiteForListings}, //Dice is tricky one; it tends to display an order of magnitude more listings than the others.
                    {Jobsite.Indeed, GenerateProxyListing},
                    {Jobsite.Glassdoor, GenerateProxyListing}
                };
        //Finally, establish the RabbitMQ connections
        string instanceId = Guid.NewGuid().ToString(); //Right now this is purely for meta purposes, mainly logging. Each session has a unique ID to attach to log messages
        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            UserName = settings.UserName,
            Password = settings.Password
        };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        //Set up Queue for remote tasks to be assigned in
        await channel.QueueDeclareAsync(queue: settings.QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
        //Start up the logging exchange, and send a preliminary log message upon initialization of program
        await channel.ExchangeDeclareAsync(exchange: settings.LogExchangeName, type: ExchangeType.Topic);
        string routingKeyBase = "Backend";
        await RMQLog.LogAsync(routingKeyBase + ".info", "Backend Initialized", instanceId, channel, settings.LogExchangeName);
        var consumer = new AsyncEventingBasicConsumer(channel);
        //Now, to set up the receivedAsync logic
        consumer.ReceivedAsync += async (object sender, BasicDeliverEventArgs ea) =>
        {
            AsyncEventingBasicConsumer cons = (AsyncEventingBasicConsumer)sender;
            IChannel ch = cons.Channel;
            string response = string.Empty;//We default to an empty string
            byte[] body = ea.Body.ToArray();
            IReadOnlyBasicProperties props = ea.BasicProperties;
            var replyProps = new BasicProperties
            {
                CorrelationId = props.CorrelationId
            };
            var message = "";
            await RMQLog.LogAsync(routingKeyBase + ".info", "Request received by Backend for data from source: " + Encoding.UTF8.GetString(body), instanceId, channel, settings.LogExchangeName);
            try
            {
                //Here is where I will actually do stuff with the incoming request, to formulate a response to send back.
                //Step 1: Having this setup, and expecting a RequestSpecification object in the message, I can pull out the expected Jobsite option.
                Jobsite jobsite = Jobsite.Error;
                message = Encoding.UTF8.GetString(body);
                RequestSpecifications? request = JsonSerializer.Deserialize<RequestSpecifications>(message);
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
                List<GenericJobListing> listings = [];
                //Then switch!
                switch (jobsite)
                {
                    case Jobsite.Error:
                        //error - send empty string
                        FormattedConsoleOutput.Warning("Request Jobsite enum does not parse.");
                        response = string.Empty;
                        break;

                    case Jobsite.Dummy:
                        //dummy - send some dummy data. This will never change, so I've not included it in the Dictionary and it gets its own case here.
                        listings =
                        [
                            new GenericJobListing{Title="dummytitle1", Company="dummycompany1", JobsiteId="dummy0001", LinkToJobListing="dummylinktojoblisting1", Location="dummylocation1", PostDateTime=DateTime.Now.ToString()},
                            new GenericJobListing{Title="dummytitle2", Company="dummycompany2", JobsiteId="dummy0002", LinkToJobListing="dummylinktojoblisting2", Location="dummylocation2", PostDateTime=DateTime.Now.ToString()},
                        ];
                        response = JsonSerializer.Serialize(listings);
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
                        response = JsonSerializer.Serialize(listings);
                        break;

                    default:
                        //default - this is a normal jobsite, so call its handler function
                        listings = await handlerDictionary[jobsite](client, jobsite, urlDictionary[jobsite], parseByJobsite[jobsite], request);
                        response = JsonSerializer.Serialize(listings);
                        break;
                }
            }
            catch (Exception e)
            {
                FormattedConsoleOutput.Warning("Error in the main switch: " + e);
                response = string.Empty; //This is already the default, but the try block may have altered it before hitting an exception so we'll set it here.
            }
            finally
            {
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await ch.BasicPublishAsync(exchange: string.Empty, routingKey: props.ReplyTo!, mandatory: true, basicProperties: replyProps, body: responseBytes);
                await ch.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                await RMQLog.LogAsync(routingKeyBase + ".info", "Response sent by Backend about data from source: " + message + " on channel of " + ch, instanceId, channel, settings.LogExchangeName);
            }
        };
        await channel.BasicConsumeAsync(settings.QueueName, false, consumer);
        Console.WriteLine(" [x] Awaiting RPC requests");
        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
    /// <summary>
    /// The placeholder function with a handler-compatible signature, for when I have no method of dealing with a given jobsite. 
    /// Should go unused except for when I'm actively fixing / developing something.
    /// </summary>
    /// <param name="client">The HTTPClient to make calls with</param>
    /// <param name="jobsite">The jobsite enum I'm working with at the moment</param>
    /// <param name="url">The URL of the jobsite, pre-filled with its own parameters and ready to call</param>
    /// <param name="parseApproachDictionary">The dictionary I'll extract my parse approach data from, for parsing the HTTP call result</param>
    /// <param name="request">The original request object, which contains various bits of data I need to parse and format properly</param>
    static Task<List<GenericJobListing>> Placeholder(HttpClient client, Jobsite jobsite, string url, Dictionary<string, List<ParseApproach>> parseApproachDictionary, RequestSpecifications request)
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
    static Task<List<GenericJobListing>> GenerateProxyListing(HttpClient client, Jobsite jobsite, string url, Dictionary<string, List<ParseApproach>> parseApproachDictionary, RequestSpecifications request)
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
    static async Task<List<GenericJobListing>> PollAndParseJobSiteForListings(HttpClient client, Jobsite jobsite, string url, Dictionary<string, List<ParseApproach>> parseApproachDictionary, RequestSpecifications request)
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
                Title = TryParseList(listing, "Title", fallback, parseApproachDictionary),
                Company = TryParseList(listing, "Company", fallback, parseApproachDictionary),
                JobsiteId = jobsite.ToString() + TryParseList(listing, "JobsiteId", fallbackId, parseApproachDictionary),
                Location = TryParseList(listing, "Location", fallback, parseApproachDictionary),
                PostDateTime = TryParseList(listing, "PostDateTime", fallback, parseApproachDictionary),
                LinkToJobListing = TryParseList(listing, "LinkToJobListing", fallback, parseApproachDictionary),
            };
            //Fourth step, filter listing based on request specifications beyond what is in the URL
            DateTime theoreticalJobPostTime = PostDateTimeEstimateFromVagueString(job.PostDateTime);
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
    static async Task<List<GenericJobListing>> DicePaginatedPollAndParseJobSiteForListings(HttpClient client, Jobsite jobsite, string url, Dictionary<string, List<ParseApproach>> parseApproachDictionary, RequestSpecifications request)
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
    /// Runs through the given parseApproaches taken from the dictionary
    /// </summary>
    /// <param name="input">The input string to parse</param>
    /// <param name="propertyName">The name of the property we're trying to parse a value for</param>
    /// <param name="fallback">The string to return if every parse approach fails</param>
    /// <param name="parseApproachDictionary">The dictionary of parse approaches for a specific jobsite</param>
    private static string TryParseList(string input, string propertyName, string fallback, Dictionary<string, List<ParseApproach>> parseApproachDictionary)
    {
        if (!parseApproachDictionary.TryGetValue(propertyName, out List<ParseApproach>? value))
        {
            return fallback;
        }
        string output = string.Empty;
        for (int i = 0; i < value.Count; i++)
        {
            //iterate through parse approaches; stop when one works.
            ParseApproach currentApproach = value[i];
            output = StringMunging.TryGetSubString(input, currentApproach.PreSubstring, currentApproach.PostSubstring, currentApproach.KeepPreSubstring, currentApproach.KeepPostSubstring);
            if (output != string.Empty) break;
        }
        if (output == string.Empty) FormattedConsoleOutput.Warning("All parse approaches failed for " + propertyName);
        output = Uri.UnescapeDataString(output); //Some of the sites use URI encoding; I'm getting rid of that here.
        return output != string.Empty ? output : fallback;
    }
    /// <summary>
    /// Takes a string that is in no way consistently formatted, and pulls the lowest common denominator of relative time information out of it
    /// </summary>
    /// <param name="postDateTime">The input string to parse</param>
    private static DateTime PostDateTimeEstimateFromVagueString(string postDateTime)
    {
        int hoursAgo = 0;
        int minutesAgo = 0;
        if (postDateTime.Contains("hour", StringComparison.CurrentCultureIgnoreCase)) hoursAgo = 1;
        else if (postDateTime.Contains("minute", StringComparison.CurrentCultureIgnoreCase)) minutesAgo = 1;
        else if (postDateTime.Contains("today", StringComparison.CurrentCultureIgnoreCase)) hoursAgo = 0;
        else if (postDateTime.Contains("day", StringComparison.CurrentCultureIgnoreCase)) hoursAgo = 24;
        else if (postDateTime.Contains("week", StringComparison.CurrentCultureIgnoreCase)) hoursAgo = 24 * 7;
        else if (postDateTime.Contains("month", StringComparison.CurrentCultureIgnoreCase)) hoursAgo = 24 * 7 * 30;
        else if (postDateTime.Contains("year", StringComparison.CurrentCultureIgnoreCase)) hoursAgo = 24 * 7 * 30 * 12;
        return DateTime.Now.Subtract(new TimeSpan(hoursAgo, minutesAgo, 0));
    } 
}