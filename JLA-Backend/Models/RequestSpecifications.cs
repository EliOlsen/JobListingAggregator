namespace Backend.Models;

public class RequestSpecifications
{
    public required string Source { get; set; }
    public required DateTime CutoffTime { get; set; }
    public required bool IsRemote { get; set; }
    public required int Radius { get; set; }
    public required string SearchTerms { get; set; }
    public required string[] CompanyFilterTerms { get; set; }
    public required string[] TitleFilterTerms { get; set; }
}