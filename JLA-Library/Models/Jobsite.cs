namespace JLALibrary.Models;
/// <summary>
/// Defines the supported job sites, the special cases of dummy and all, and the fallback of error
/// </summary>
public enum Jobsite
{
    All,
    BuiltIn,
    Dice,
    Glassdoor,
    Indeed,
    LinkedIn,
    Dummy,
    Error
}