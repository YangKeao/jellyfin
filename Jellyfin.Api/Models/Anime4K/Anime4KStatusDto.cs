namespace Jellyfin.Api.Models.Anime4K;

/// <summary>
/// Describes Anime4K configuration and runtime availability.
/// </summary>
public sealed class Anime4KStatusDto
{
    /// <summary>
    /// Gets or sets a value indicating whether Anime4K is enabled in encoding configuration.
    /// </summary>
    public bool Configured { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the startup runtime probe succeeded.
    /// </summary>
    public bool Available { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Anime4K can be used for new sessions.
    /// </summary>
    public bool Effective { get; set; }

    /// <summary>
    /// Gets or sets the selected Anime4K profile.
    /// </summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bundled Anime4K shader version.
    /// </summary>
    public string ShaderVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output target description.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a human-readable status reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
