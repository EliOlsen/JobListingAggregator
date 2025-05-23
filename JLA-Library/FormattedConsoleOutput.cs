namespace JLALibrary;

public static class FormattedConsoleOuptut
{
    public static void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(" [.] " + message);
        Console.ForegroundColor = ConsoleColor.White;
    }
    public static void Warning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(" [.] " + message);
        Console.ForegroundColor = ConsoleColor.White;
    }
}