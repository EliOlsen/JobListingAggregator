using System.Text.RegularExpressions;
using JLALibrary.Models;
namespace JLALibrary;

public static class StringMunging
{
    /// <summary>
    /// Attempts to pull a specific string out of a larger string, using two input substrings and two bools to determine exact trimming begavior. Returns empty if operation fails at any point.
    /// </summary>
    /// <param name="input">Input string to try and pull desired substring out of</param>
    /// <param name="preSubstring">Substring that (theoretically) directly precedes the desired substring within the input</param>
    /// <param name="postSubstring">Substring that (theoretically) directly follows the desired substring within the input</param>
    /// <param name="KeepPreSubstring">Boolean to indicate whether the pre-substring should be included in the final output string</param>
    /// <param name="KeepPostSubstring">Boolean to indicate whether the post-substring should be included in the final output string</param>
    public static string TryGetSubString(string input, string preSubstring, string postSubstring, bool KeepPreSubstring, bool KeepPostSubstring)
    {
        if (input.Length == 0 || preSubstring.Length == 0 || postSubstring.Length == 0 || preSubstring.Length + postSubstring.Length >= input.Length)
        {
            //then we've been passed an invalid set of variables.
            FormattedConsoleOutput.Warning("Invalid variables passed to TryGetSubstring");
            return string.Empty;
        }
        int cutOneIndex = input.IndexOf(preSubstring); //grab our start index
        if (cutOneIndex == -1)
        {
            //then our input does not contain enough information to pull a valid substring out
            return string.Empty;
        }
        int cutTwoIndex = input[(cutOneIndex + preSubstring.Length)..].IndexOf(postSubstring); //grab our end index, working to ensure it cannot be found before start. (not absolute index yet!)
        if (cutTwoIndex == -1)
        {
            //then our input does not contain enough information to pull a valid substring out
            return string.Empty;
        }
        int finalCutOneIndex = cutOneIndex + (KeepPreSubstring ? 0 : preSubstring.Length);
        int finalCutTwoIndex = cutOneIndex + preSubstring.Length + cutTwoIndex + (KeepPostSubstring ? (-1 * postSubstring.Length) : 0);

        try
        {
            return input[finalCutOneIndex..finalCutTwoIndex].Trim();
        }
        catch (Exception e)
        {
            FormattedConsoleOutput.Warning("Invalid calculation in TryGetSubstring: " + e);
            return string.Empty;
        }
    }
    /// <summary>
    /// Attempts to pull out a list of substrings from a single input string, defined by the substrings directly preceding and following each desired substring. Returns empty list if operation fails at any point.
    /// </summary>
    /// <param name="input">Input string to try and pull desired substrings out of</param>
    /// <param name="preSubstring">Substring that (theoretically) directly precedes each substring within the input</param>
    /// <param name="postSubstring">Substring that (theoretically) directly follows each substring within the input</param>
    /// <param name="KeepPreSubstring">Boolean to indicate whether the pre-substring should be included in each final output string</param>
    /// <param name="KeepPostSubstring">Boolean to indicate whether the post-substring should be included in each final output string</param>
    public static List<string> BreakStringIntoStringsOnStartAndEndSubstrings(string input, string startString, string endString, bool keepStartString, bool keepEndString)
    {
        List<string> output = [];
        if (startString.Length < 1 || endString.Length < 1 || input.Length < (startString.Length + endString.Length))
        {
            FormattedConsoleOutput.Warning("Invalid variables passed to BreakStringIntoStringsOnStartAndEndSubstrings");
            return output;
        }
        string[] intermediate = input.Split(startString);
        if (intermediate.Length < 1)
        {
            return output;
        }
        for (int i = 0; i < intermediate.Length; i++)
        {
            string[] bits = intermediate[i].Split(endString);
            if (bits.Length > 1)
            {
                output.Add((keepStartString ? startString : "") + bits[0] + (keepEndString ? endString : ""));
            }
        }
        return output;
    }
    /// <summary>
    /// Determines whether a given string contains none of the given substrings as full words; the substrings don't count if they're part of larger words.
    /// </summary>
    /// <param name="input">The string being tested</param>
    /// <param name="substrings">The substrings to test for</param>
    public static bool StringContainsNoneOfSubstringsInArray(string input, string[] substrings)
    {//This is a LITTLE more complicated than it appears; assume the substrings are all complete words, don't disqualify for word fragments that match, only full words.
        foreach (string substring in substrings)
        {
            string trimmedSubstring = substring.Trim();
            if (input.Contains(trimmedSubstring))
            {
                int occurence = input.IndexOf(trimmedSubstring);
                Regex reg = new((occurence == 0 ? "^" : "[^\\w]") + trimmedSubstring + (occurence + trimmedSubstring.Length == input.Length ? "$" : "[^\\w]"));
                if (reg.IsMatch(input)) return false; //If we match our regex, then the string does contain at least one of our substrings, so our answer is false.
            }
        }
        return true; //If we've gotten this far, we know our string does not contain any of the substrings as complete words.
    }
    /// <summary>
    /// Takes a string that is in no way consistently formatted, and pulls the lowest common denominator of relative time information out of it
    /// </summary>
    /// <param name="postDateTime">The input string to parse</param>
    public static DateTime PostDateTimeEstimateFromVagueString(string postDateTime)
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
    /// <summary>
    /// Runs through the given parseApproaches taken from the dictionary
    /// </summary>
    /// <param name="input">The input string to parse</param>
    /// <param name="propertyName">The name of the property we're trying to parse a value for</param>
    /// <param name="fallback">The string to return if every parse approach fails</param>
    /// <param name="parseApproachDictionary">The dictionary of parse approaches for a specific jobsite</param>
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
        if (output == string.Empty) FormattedConsoleOutput.Warning("All parse approaches failed for " + propertyName);
        output = Uri.UnescapeDataString(output); //Some of the sites use URI encoding; I'm getting rid of that here.
        return output != string.Empty ? output : fallback;
    }
}