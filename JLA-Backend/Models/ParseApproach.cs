namespace JLABackend.Models;

public class ParseApproach
{
    public required string PreSubstring { get; set; }
    public required string PostSubstring { get; set; }
    public required bool KeepPreSubstring { get; set; }
    public required bool KeepPostSubstring { get; set; }
}