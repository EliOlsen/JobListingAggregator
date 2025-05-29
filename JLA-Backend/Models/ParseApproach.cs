namespace JLABackend.Models;
public class ParseApproach
{
    /// <summary>
    /// Gets or sets the substring intended to immediately precede the desired substring in a larger given string
    /// </summary>
    public required string PreSubstring { get; set; }
    /// <summary>
    /// Gets or sets the substring intended to immediately follow the desired substring in a larger given string
    /// </summary>
    public required string PostSubstring { get; set; }
    /// <summary>
    /// Gets or sets the boolean intended to indicate whether the preSubstring should be included in the target substring, or discarded
    /// </summary>
    public required bool KeepPreSubstring { get; set; }
    /// <summary>
    /// Gets or sets the boolean intended to indicate whether the postSubstring should be included in the target substring, or discarded
    /// </summary>
    public required bool KeepPostSubstring { get; set; }
}