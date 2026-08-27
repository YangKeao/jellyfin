namespace MediaBrowser.Model.Configuration;

/// <summary>
/// Anime4K NVENC quality profiles.
/// </summary>
public enum Anime4KQualityProfile
{
    /// <summary>
    /// Lower GPU cost with good quality.
    /// </summary>
    Balanced,

    /// <summary>
    /// Recommended quality and GPU cost.
    /// </summary>
    High,

    /// <summary>
    /// Highest quality with the greatest GPU cost.
    /// </summary>
    Maximum
}
