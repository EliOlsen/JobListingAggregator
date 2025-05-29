using JLALibrary.Models;
namespace JLABackend.Data;
public class DefaultParserApproach
{//This is where I'm going to keep the default parser approach; 
 //it might change with various maintenance updates, and in fact this is the MOST likely thing I'll need to update to keep basic functionality intact over time.
 //I considered storing it in an editable configuration file, but it would get overwritten whenever I updated the application and it doesn't seem reasonable
 //to otherwise accomodate manual changes to a specific instance. Any changes to this should be propogated to all instances anyway.
    /// <summary>
    /// Provides the hardcoded dictionary of parse approaches
    /// </summary>
    public static Dictionary<Jobsite, Dictionary<string, List<ParseApproach>>> GetDefault()
    {
        return new Dictionary<Jobsite, Dictionary<string, List<ParseApproach>>>
                {
                    {Jobsite.Glassdoor, new()},
                    {Jobsite.Indeed, new()},
                    { Jobsite.LinkedIn, new Dictionary<string, List<ParseApproach>>
                        {
                            {"master", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "base-search-card base-search-card--link job-search-card",
                                        PostSubstring = "</time>",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = true,
                                    }
                                }
                            },
                            {"Title", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "<span class=\"sr-only\">",
                                        PostSubstring = "</span>",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"Company", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "trk=public_jobs_jserp-result_job-search-card-subtitle\">",
                                        PostSubstring = "</a>",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"JobsiteId", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "data-entity-urn=\"urn:li:jobPosting:",
                                        PostSubstring = "\" ",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"Location", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "<span class=\"job-search-card__location\">",
                                        PostSubstring = "</span>",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"PostDateTime", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "datetime=\"",
                                        PostSubstring = "</time>",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"LinkToJobListing", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "href=\"",
                                        PostSubstring = "\" ",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            }
                        }
                    },
                    {Jobsite.BuiltIn, new Dictionary<string, List<ParseApproach>>
                        {
                            {"master", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "data-id=\"job-card\"",
                                        PostSubstring = ">Top Skills:<",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"Title", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "class=\"card-alias-after-overlay text-break\">",
                                        PostSubstring = "</a>",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"Company", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "class=\"font-barlow fw-medium fs-xl d-inline-block m-0 text-pretty-blue hover-underline cursor-pointer z-1\"><span>",
                                        PostSubstring = "</span>",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"JobsiteId", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "entity-id=\"",
                                        PostSubstring = "\" ",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"Location", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "<span class=\"font-barlow text-gray-04\">",
                                        PostSubstring = "</span>",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"PostDateTime", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "class=\"fa-regular fa-clock fs-xs text-gray-03 d-inline-block me-sm d-lg-none d-xl-inline-block\"></i>",
                                        PostSubstring = "</span>",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"LinkToJobListing", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "https://builtin.com/job/",
                                        PostSubstring = "\" ",
                                        KeepPreSubstring = true,
                                        KeepPostSubstring = false,
                                    }
                                }
                            }
                        }
                    },
                    {Jobsite.Dice, new Dictionary<string, List<ParseApproach>>
                        {
                            {"master", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "<div class=\"box mr-2 inline-flex h-6 items-center justify-center rounded bg-zinc-100 px-2 \" aria-labelledby=\"employmentType-label\">",
                                        PostSubstring = "</p></div></span>",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"Title", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "data-rac=\"\" data-testid=\"job-search-job-detail-link\" aria-label=\"",
                                        PostSubstring = "\" ",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"Company", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "companyname=",
                                        PostSubstring = "\"",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    },
                                    new ParseApproach
                                    {
                                        PreSubstring = "data-testid=\"job-card-company-name\">",
                                        PostSubstring = "</p></span>",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"JobsiteId", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "data-id=\"",
                                        PostSubstring = "\"",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"Location", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "<div class=\"inline-flex flex-col items-center justify-start gap-2.5\"><div class=\"inline-flex items-center justify-start gap-1.5\"><p class=\"text-sm font-normal text-zinc-600\">",
                                        PostSubstring = "</p>",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"PostDateTime", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "<div class=\"flex items-center justify-center gap-2.5\"><p class=\"text-sm font-normal text-zinc-600\">",
                                        PostSubstring = "</p>",
                                        KeepPreSubstring = false,
                                        KeepPostSubstring = false,
                                    }
                                }
                            },
                            {"LinkToJobListing", new List<ParseApproach>
                                {
                                    new ParseApproach
                                    {
                                        PreSubstring = "https://www.dice.com/",
                                        PostSubstring = "\" ",
                                        KeepPreSubstring = true,
                                        KeepPostSubstring = false,
                                    }
                                }
                            }
                        }
                    }
                };
    }
}