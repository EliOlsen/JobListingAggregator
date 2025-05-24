namespace JLALibrary;

public static class StringMunging
{
    public static string TryGetSubString(string input, string preSubstring, string postSubstring, bool KeepPreSubstring, bool KeepPostSubstring)
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
            FormattedConsoleOuptut.Warning("Invalid calculation in TryGetSubstring: " + e);
            return string.Empty;
        }
    }

    public static List<string> BreakStringIntoStringsOnStartAndEndSubstrings(string input, string startString, string endString)
    {
        string[] intermediate;
        List<string> output = [];
        if (startString.Length < 1 || endString.Length < 1 || input.Length < (startString.Length + endString.Length))
        {
            FormattedConsoleOuptut.Warning("Invalid variables passed to BreakStringIntoStringsOnStartAndEndSubstrings");
            return output;
        }
        intermediate = input.Split(startString);
        if (intermediate.Length < 1)
        {
            return output;
        }
        for (int i = 0; i < intermediate.Length; i++)
        {
            string chunk = intermediate[i];
            string[] bits = chunk.Split(endString);
            if (bits.Length > 1)
            {
                output.Add(bits[0]);
            }
        }
        return output;
    }
}