namespace JLALibrary;

public static class StringMunging
{
    public static string TryGetSubString(string input, string preSubstring, string postSubstring)
    {
        if (input.Length == 0 || preSubstring.Length == 0 || postSubstring.Length == 0 || preSubstring.Length + postSubstring.Length >= input.Length)
        {
            //then we've been passed an invalid set of variables.
            FormattedConsoleOuptut.Warning("Invalid variables passed to TryGetSubstring");
            return string.Empty;
        }
        int cutOneIndex = input.IndexOf(preSubstring); //grab our start index
        if (cutOneIndex == -1)
        {
            //then our input does not contain enough information to pull a valid substring out
            FormattedConsoleOuptut.Warning("PreSubstring does not exist in input string");
            return string.Empty;
        }
        int cutTwoIndex = input[(cutOneIndex + preSubstring.Length)..].IndexOf(postSubstring); //grab our end index, working to ensure it cannot be found before start. (not absolute index yet!)
        if (cutTwoIndex == -1)
        {
            //then our input does not contain enough information to pull a valid substring out
            FormattedConsoleOuptut.Warning("PostSubString does not exist in input string past PreSubstring");
            return string.Empty;
        }
        return input[(cutOneIndex + preSubstring.Length)..(cutOneIndex + preSubstring.Length + cutTwoIndex)].Trim();
    }
}