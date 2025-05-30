namespace JLALibrary.Models;

public class RequestSpecifications
{
    public required string Source { get; set; }
    public required DateTime CutoffTime { get; set; }
    public required bool IsRemote { get; set; }
    public required int Radius { get; set; }
    public required string SearchTerms { get; set; }
    public required string CultureInfoString { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string StateAbbrev { get; set; }
    public required string GeoId { get; set; }
    public required int MaxSalary { get; set; }
    public required int MinSalary { get; set; }
    public required string BuiltInJobCategory { get; set; }
    public required string[] CompanyFilterTerms { get; set; }
    public required string[] TitleFilterTerms { get; set; }
}