using Jellyfin.Api.Models.Anime4K;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Exposes Anime4K runtime status to administrators.
/// </summary>
[Route("System/Anime4K")]
[Authorize(Policy = Policies.RequiresElevation)]
public class Anime4KController : BaseJellyfinApiController
{
    private readonly IServerConfigurationManager _configurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Anime4KController"/> class.
    /// </summary>
    /// <param name="configurationManager">Server configuration manager.</param>
    public Anime4KController(IServerConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
    }

    /// <summary>
    /// Gets Anime4K configuration and runtime status.
    /// </summary>
    /// <returns>Anime4K status.</returns>
    [HttpGet("Status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<Anime4KStatusDto> GetStatus()
    {
        var options = _configurationManager.GetEncodingOptions();
        var available = Anime4KHelper.IsRuntimeAvailable;
        var nvenc = options.HardwareAccelerationType == HardwareAccelerationType.nvenc;
        var effective = options.EnableAnime4K && available && nvenc;
        var encoderSettings = Anime4KHelper.GetEncoderSettings(options.Anime4KQuality);

        var reason = !options.EnableAnime4K
            ? "Disabled in transcoding settings."
            : !nvenc
                ? "Anime4K requires NVIDIA NVENC hardware acceleration."
                : available
                    ? "Anime4K is ready for eligible new playback sessions."
                    : Anime4KHelper.RuntimeReason;

        return new Anime4KStatusDto
        {
            Configured = options.EnableAnime4K,
            Available = available,
            Effective = effective,
            Profile = "Mode A (Fast)",
            Quality = options.Anime4KQuality.ToString(),
            ConstantQuality = encoderSettings.ConstantQuality,
            EncoderPreset = encoderSettings.Preset,
            MaxBitrate = Anime4KHelper.NormalizeMaxBitrate(options.Anime4KMaxBitrate),
            ShaderVersion = "4.0.1",
            Target = $"{Anime4KHelper.TargetWidth}x{Anime4KHelper.TargetHeight} boundary",
            Reason = reason
        };
    }
}
