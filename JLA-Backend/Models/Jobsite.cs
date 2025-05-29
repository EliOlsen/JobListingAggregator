namespace JLABackend.Models;
/// <summary>
/// Defines the supported job sites, the special cases of dummy and all, and the fallback of error
/// </summary>
public enum Jobsite
{
    LinkedIn,
    BuiltIn,
    Indeed,
    Glassdoor,
    Dice,
    Dummy,
    All,
    Error
}