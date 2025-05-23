namespace JLALogger;

public class JLALoggerConfiguration
{
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public required string HostName { get; set; }
    public required string LogExchangeName { get; set; }
    public required string LogFolderPath { get; set; }
    public required string[] LogQueueBindingKeys { get; set; }

    public bool ReplaceEmptyStrings(JLALoggerConfiguration replacements)
    {
        bool output = false;
        if (this.UserName is null)
        {
            this.UserName = replacements.UserName;
            output = true;
        }
        if (this.Password is null)
        {
            this.Password = replacements.Password;
            output = true;
        }
        if (this.HostName is null)
        {
            this.HostName = replacements.HostName;
            output = true;
        }
        if (this.LogExchangeName is null)
        {
            this.LogExchangeName = replacements.LogExchangeName;
            output = true;
        }
        if (this.LogFolderPath is null)
        {
            this.LogFolderPath = replacements.LogFolderPath;
            output = true;
        }
        if (this.LogQueueBindingKeys is null)
        {
            this.LogQueueBindingKeys = replacements.LogQueueBindingKeys;
            output = true;
        }
        else if (this.LogQueueBindingKeys.Length == 0)
        {
            this.LogQueueBindingKeys = replacements.LogQueueBindingKeys;
            output = true;
        }

        return output;
    }
}