using System.Globalization;
using System.Text;
using System.Text.Json;
using Backend.Models;
using JLABackend.Models;
using JLALibrary;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace JLABackend;

public class JLABackend
{
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
            QueueName = "scratchjobs_queue"
        };
        JLABackendConfiguration settings = await new UserJsonConfiguration<JLABackendConfiguration>().RetrieveAndValidateSettings(DefaultConfiguration, configFilePath);
        //Configuration acquired. Now, to establish the RabbitMQ setup we need.

        string instanceId = Guid.NewGuid().ToString();
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
                //Step 1: associate each supposed job site with a function. This function will be the one to use to return data.
                //Some will go to parse the sites. Others will generate fallback dummy listings to link to sites. This dictionary controls which are which.
                //All functions will have to have the same parameters using this approach.
                //I know I want to return a list of job listings, but I don't know what I want the common parameters to be yet.
                Dictionary<Jobsite, Func<Jobsite, Dictionary<Jobsite, string>, Dictionary<string, List<ParseApproach>>, Task<List<GenericJobListing>>>> handlerDictionary = new()
                {
                    {Jobsite.LinkedIn, PollAndParseJobSiteForListings}, //For now, since I don't have any of these functions, I'll leave them with a default option.
                    {Jobsite.BuiltIn, PollAndParseJobSiteForListings},
                    {Jobsite.Dice, Placeholder},
                    {Jobsite.Indeed, GenerateProxyListing},
                    {Jobsite.Glassdoor, GenerateProxyListing}
                };
                //Now, having this setup, and expecting a RequestSpecification object in the message, I can pull out the expected Jobsite option.
                Jobsite jobsite = Jobsite.Error;
                message = Encoding.UTF8.GetString(body);
                RequestSpecifications? request = JsonSerializer.Deserialize<RequestSpecifications>(message);
                if (request is not null) Enum.TryParse(request.Source, true, out jobsite);
                //From that Jobsite, I can use a switch to perform the expected behavior. Also, so long as Jobsite isn't Error, we know Request isn't null.
                //establish variables for use within various switch branches
                RegionInfo regionInfo = new RegionInfo(new CultureInfo(request?.CultureInfoString is not null ? request.CultureInfoString : "en-us", false).LCID);
                Dictionary<Jobsite, string> urlDictionary = new Dictionary<Jobsite, string>//Needs further refinement, but will do for now.
                {//These are the links to the specific URLs I'll either be calling or using as placeholder listings.
                 //Not sure if I should shift these out to a config file. Probably not - proper interjection of request variables should mean the only time the actual format changes is when the URL format itself is changed, and that's something I'll have to maintain myself
                 //Note: Location is, uh, a crapshoot. Everything from city (easy) to 'geoid', which, what? I can obtain my own, personal location values by the same method I obtained these URLs, but that's a black box for the geoid.
                    {Jobsite.LinkedIn, $"https://www.linkedin.com/jobs/search/?distance={request.Radius}&f_E=2%2C3&f_TPR=r86400&geoId={request.GeoId}&keywords={request.SearchTerms.Replace(" ", "%20")}&origin=JOB_SEARCH_PAGE_JOB_FILTER" },
                    {Jobsite.BuiltIn, $"https://builtin.com/jobs/remote/hybrid/office/{request.BuiltInJobCategory}/entry-level?search={request.SearchTerms.Replace(" ", "%20")}&daysSinceUpdated=1&city={request.City.Replace(" ", "%20")}&state={request.State.Replace(" ", "%20")}&country={regionInfo.ThreeLetterISORegionName}"},
                    {Jobsite.Dice, $"https://www.dice.com/platform/jobs?filters.postedDate=ONE&filters.employmentType=FULLTIME&filters.employerType=Direct+Hire&filters.workplaceTypes=Remote%7COn-Site%7CHybrid&radius={request.Radius}&countryCode={regionInfo.TwoLetterISORegionName}&latitude={request.Latitude}&location={request.City.Replace(" ", "+")}%2C+{request.StateAbbrev}%2C+{regionInfo.ThreeLetterISORegionName}&locationPrecision=City&longitude={request.Longitude}&q={request.SearchTerms.Replace(" ", "+")}&radiusUnit=mi"},
                    {Jobsite.Indeed, $"https://www.indeed.com/jobs?q={request.SearchTerms.Replace(" ", "+")}&l={request.City.ToLower().Replace(" ", "+")}%2C+{request.StateAbbrev.ToLower()}&sc=0kf%3Aexplvl%28ENTRY_LEVEL%29%3B&fromage=1&vjk=53ed07a6128717ad"},
                    {Jobsite.Glassdoor, $"https://www.glassdoor.com/Job/{request.State.ToLower().Replace(" ", "-")}-{request.SearchTerms.Replace(" ", "-")}-jobs-SRCH_IL.0,11_IC1142551_KO12,29.htm?fromAge=1&maxSalary={request.MaxSalary}&minSalary={request.MinSalary}"}
                };

                //Finally, I definitely want the exact specifics of parsing individual pieces of data to be configurable outside the program. Standardizing will also make it much more concise. As such: Parse approach!
                //A ParseApproach is a set of parameters to feed to StringMunging.TryGetSubstring along with the input
                //A list of ParseApproach is the order in which they should be tried, flowing down to the next if the previous didn't give a valid output, and only submitting an empty string if all fail
                //A dictionary relates each list of ParseApproaches with the property they're meant for
                //A dictionary relates each dictionary of properties and approaches to the jobsite they're meant for.
                Dictionary<Jobsite, Dictionary<string, List<ParseApproach>>> parseByJobsite = new Dictionary<Jobsite, Dictionary<string, List<ParseApproach>>>
                {
                    {Jobsite.LinkedIn, new Dictionary<string, List<ParseApproach>>
                        {
                            {"Company", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "",
                                        PostSubstring = "",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            }
                        }
                    }
                };




                List<GenericJobListing> listings = new List<GenericJobListing>();
                //Then switch!
                switch (jobsite)
                {
                    case Jobsite.Error:
                        //error - send nothing
                        FormattedConsoleOuptut.Warning("Request Jobsite enum does not parse. Returning empty list.");
                        response = JsonSerializer.Serialize(new List<GenericJobListing>());
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
                                listings.AddRange(await handlerDictionary[js](js, urlDictionary, parseByJobsite[js]));
                            }
                        }
                        response = JsonSerializer.Serialize(listings);
                        break;

                    default:
                        //default - this is a normal jobsite, so call its handler function
                        listings = await handlerDictionary[jobsite](jobsite, urlDictionary, parseByJobsite[jobsite]);
                        response = JsonSerializer.Serialize(listings);
                        break;
                }
            }
            catch (Exception e)
            {
                FormattedConsoleOuptut.Warning(e.Message);
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

    static Task<List<GenericJobListing>> Placeholder(Jobsite jobsite, Dictionary<Jobsite, string> urlDictionary, Dictionary<string, List<ParseApproach>> parseApproachDictionary)
    {
        return Task.FromResult(new List<GenericJobListing> { });
    }

    static Task<List<GenericJobListing>> GenerateProxyListing(Jobsite jobsite, Dictionary<Jobsite, string> urlDictionary, Dictionary<string, List<ParseApproach>> parseApproachDictionary)
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
            LinkToJobListing = urlDictionary[jobsite]
        }
        );
        return Task.FromResult(output);
    }

    static async Task<List<GenericJobListing>> PollAndParseJobSiteForListings(Jobsite jobsite, Dictionary<Jobsite, string> urlDictionary, Dictionary<string, List<ParseApproach>> parseApproachDictionary)
    {
        //To do: Ensure that passed parameters are usable, before doing anything else.
        if (!parseApproachDictionary.ContainsKey("master") || !urlDictionary.ContainsKey(jobsite))
        {
            //We don't have a master parsing approach, and therefore cannot process our data.
            FormattedConsoleOuptut.Warning("PollAndParseJobsiteForListings was passed invalid data, and cannot process. Returning empty list.");
            return [];
        }
        //To do: First step, which is to actually call the URL
        string rawHTML = null; //Gotta fill this in with a fetch request using the URL
        //Second step, break up the giant block of html into chunks, per listing
        List<string> brokenUpListings = [];
        for (int i = 0; i < parseApproachDictionary["master"].Count; i++)
        {
            //iterate through parse approaches; stop when one works.
            ParseApproach currentApproach = parseApproachDictionary["master"][i];
            brokenUpListings = StringMunging.BreakStringIntoStringsOnStartAndEndSubstrings(rawHTML, currentApproach.PreSubstring, currentApproach.PostSubstring);
            if (brokenUpListings.Count > 0) break;
        }
        //if, having gone through the entire list, we still have nothing? toss out a warning and return empty list.
        if (brokenUpListings.Count < 1)
        {
            FormattedConsoleOuptut.Warning("PollAndParseJobsiteForListings found no listings. Returning empty list.");
            return [];
        }
        //Here is where we know we're going to return SOMETHING.
        List<GenericJobListing> output = [];
        foreach (string listing in brokenUpListings)
        {
            //Third step, get the individual values
            GenericJobListing job = new() //If I cannot parse one of these, I want the fallback value to display as error in client - better for me when using, so I can easily spot problems
            {
                Title = TryParseList(listing, "Title", "ERROR", parseApproachDictionary),
                Company = TryParseList(listing, "Company", "ERROR", parseApproachDictionary),
                JobsiteId = TryParseList(listing, "JobsiteId", "ERROR", parseApproachDictionary),
                Location = TryParseList(listing, "Location", "ERROR", parseApproachDictionary),
                PostDateTime = TryParseList(listing, "PostDateTime", "ERROR", parseApproachDictionary),
                LinkToJobListing = TryParseList(listing, "LinkToJobListing", "ERROR", parseApproachDictionary),
            };
            //To do: Fourth step, filter listing based on request specifications beyond what is in the URL

            //Assuming all is well,
            output.Add(job);
        }
        return output;
    }

    public static string TryParseList(string input, string propertyName, string fallback, Dictionary<string, List<ParseApproach>> parseApproachDictionary)
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
        return output != string.Empty ? output : fallback;
    }
    
}