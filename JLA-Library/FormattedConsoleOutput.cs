namespace JLALibrary;
public static class FormattedConsoleOutput
{
    /// <summary>
    /// Writes a message to console in red lettering, to indicate an error message
    /// </summary>
    /// <param name="message">The message to write to console</param>
    public static void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(" [.] " + message);
        Console.ForegroundColor = ConsoleColor.White;
    }
    /// <summary>
    /// Writes a message to console in orange lettering, to indicate a warning message
    /// </summary>
    /// <param name="message">The message to write to console</param>
    public static void Warning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(" [.] " + message);
        Console.ForegroundColor = ConsoleColor.White;
    }
    /// <summary>
    /// Writes a message to console in green lettering, to indicate a debug message
    /// </summary>
    /// <param name="message">The message to write to console</param>
    public static void Debug(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" [.] " + message);
        Console.ForegroundColor = ConsoleColor.White;
    }
}