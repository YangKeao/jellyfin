using System;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Provides Anime4K eligibility and output sizing helpers.
/// </summary>
public static class Anime4KHelper
{
    /// <summary>
    /// Default maximum Anime4K video bitrate.
    /// </summary>
    public const int DefaultMaxBitrate = 80_000_000;

    /// <summary>
    /// Minimum configurable Anime4K video bitrate.
    /// </summary>
    public const int MinMaxBitrate = 10_000_000;

    /// <summary>
    /// Maximum configurable Anime4K video bitrate.
    /// </summary>
    public const int MaxMaxBitrate = 200_000_000;

    /// <summary>
    /// The target width boundary.
    /// </summary>
    public const int TargetWidth = 3840;

    /// <summary>
    /// The target height boundary.
    /// </summary>
    public const int TargetHeight = 2160;

    /// <summary>
    /// The bundled Anime4K shader path.
    /// </summary>
    public const string ShaderPath = "/usr/share/anime4k/Anime4K_ModeA_Fast.glsl";

    private static readonly string[] AnimationGenres = ["Animation", "Anime", "动画", "動畫"];

    /// <summary>
    /// Gets a value indicating whether the container startup probe succeeded.
    /// </summary>
    public static bool IsRuntimeAvailable
        => string.Equals(Environment.GetEnvironmentVariable("JELLYFIN_ANIME4K_AVAILABLE"), "1", StringComparison.Ordinal)
           && File.Exists(ShaderPath);

    /// <summary>
    /// Gets the startup probe reason exposed by the container.
    /// </summary>
    public static string RuntimeReason
        => Environment.GetEnvironmentVariable("JELLYFIN_ANIME4K_REASON") ?? "Anime4K runtime probe did not run.";

    /// <summary>
    /// Gets the NVENC settings for an Anime4K quality profile.
    /// </summary>
    /// <param name="profile">Configured quality profile.</param>
    /// <returns>The corresponding encoder settings.</returns>
    public static Anime4KEncoderSettings GetEncoderSettings(Anime4KQualityProfile profile)
        => profile switch
        {
            Anime4KQualityProfile.Balanced => new(21, "p4", null),
            Anime4KQualityProfile.Maximum => new(17, "p6", "fullres"),
            _ => new(19, "p5", "qres")
        };

    /// <summary>
    /// Normalizes the configured maximum Anime4K video bitrate.
    /// </summary>
    /// <param name="bitrate">Configured bitrate in bits per second.</param>
    /// <returns>A valid bitrate in bits per second.</returns>
    public static int NormalizeMaxBitrate(int bitrate)
    {
        if (bitrate <= 0)
        {
            return DefaultMaxBitrate;
        }

        return Math.Clamp(bitrate, MinMaxBitrate, MaxMaxBitrate);
    }

    /// <summary>
    /// Calculates the Anime4K video bitrate ceiling for a playback session.
    /// </summary>
    /// <param name="configuredMaxBitrate">Administrator configured video ceiling.</param>
    /// <param name="sessionMaxBitrate">Optional client or user total bitrate ceiling.</param>
    /// <param name="audioBitrate">Optional output audio bitrate.</param>
    /// <returns>The effective video bitrate ceiling.</returns>
    public static int GetSessionVideoBitrate(int configuredMaxBitrate, int? sessionMaxBitrate, int? audioBitrate)
    {
        var configuredCeiling = NormalizeMaxBitrate(configuredMaxBitrate);
        if (sessionMaxBitrate is not > 0)
        {
            return configuredCeiling;
        }

        return Math.Max(1, Math.Min(configuredCeiling, sessionMaxBitrate.Value - Math.Max(audioBitrate ?? 0, 0)));
    }

    /// <summary>
    /// Determines whether the item is animation. NoAnime4K always wins and Anime4K forces inclusion.
    /// </summary>
    /// <param name="item">Playback item.</param>
    /// <param name="series">Parent series, when applicable.</param>
    /// <returns><see langword="true"/> when Anime4K metadata rules match.</returns>
    public static bool IsAnimation(BaseItem item, BaseItem? series)
    {
        var tags = (item.Tags ?? [])
            .Concat(series?.Tags ?? [])
            .ToArray();

        if (tags.Contains("NoAnime4K", StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (tags.Contains("Anime4K", StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return (item.Genres ?? [])
            .Concat(series?.Genres ?? [])
            .Any(genre => AnimationGenres.Contains(genre, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines whether a media source is safe and useful to upscale.
    /// </summary>
    /// <param name="mediaSource">Media source.</param>
    /// <param name="targetWidth">Calculated even output width.</param>
    /// <param name="targetHeight">Calculated even output height.</param>
    /// <returns><see langword="true"/> when the source meets the v1 safety gates.</returns>
    public static bool TryGetTargetSize(MediaSourceInfo mediaSource, out int targetWidth, out int targetHeight)
    {
        targetWidth = 0;
        targetHeight = 0;

        var stream = mediaSource.VideoStream;
        if (mediaSource.Protocol != MediaProtocol.File
            || mediaSource.IsRemote
            || mediaSource.IsInfiniteStream
            || stream?.Width is not > 0
            || stream.Height is not > 0
            || stream.VideoRange != VideoRange.SDR)
        {
            return false;
        }

        var frameRate = stream.AverageFrameRate ?? stream.RealFrameRate;
        if (!frameRate.HasValue || frameRate.Value <= 0 || frameRate.Value > 30.01F)
        {
            return false;
        }

        var inputWidth = stream.Width.Value;
        var inputHeight = stream.Height.Value;
        if (Math.Abs(stream.Rotation ?? 0) == 90)
        {
            (inputWidth, inputHeight) = (inputHeight, inputWidth);
        }

        var scale = Math.Min((double)TargetWidth / inputWidth, (double)TargetHeight / inputHeight);
        if (scale <= 1D)
        {
            return false;
        }

        targetWidth = Math.Max(2, ((int)Math.Floor(inputWidth * scale) / 2) * 2);
        targetHeight = Math.Max(2, ((int)Math.Floor(inputHeight * scale) / 2) * 2);
        return targetWidth > inputWidth || targetHeight > inputHeight;
    }
}
