namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// NVENC settings associated with an Anime4K quality profile.
/// </summary>
/// <param name="ConstantQuality">NVENC constant-quality value.</param>
/// <param name="Preset">NVENC encoder preset.</param>
/// <param name="Multipass">Optional NVENC multipass mode.</param>
public readonly record struct Anime4KEncoderSettings(int ConstantQuality, string Preset, string? Multipass);
