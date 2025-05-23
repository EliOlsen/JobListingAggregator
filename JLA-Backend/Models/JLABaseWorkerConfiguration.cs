namespace JLABaseWorker.Models;

public class JLABaseWorkerConfiguration
{
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public required string HostName { get; set; }
    public required string LogExchangeName { get; set; }
    public required string QueueName { get; set; }

    public bool ReplaceEmptyStrings(JLABaseWorkerConfiguration replacements)
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
        if (this.QueueName is null)
        {
            this.QueueName = replacements.QueueName;
            output = true;
        }
        return output;
    }
}