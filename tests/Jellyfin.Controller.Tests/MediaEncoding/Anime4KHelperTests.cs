using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Xunit;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class Anime4KHelperTests
{
    [Theory]
    [InlineData(Anime4KQualityProfile.Balanced, 21, "p4", null)]
    [InlineData(Anime4KQualityProfile.High, 19, "p5", "qres")]
    [InlineData(Anime4KQualityProfile.Maximum, 17, "p6", "fullres")]
    public void GetEncoderSettings_MapsProfile(
        Anime4KQualityProfile profile,
        int expectedCq,
        string expectedPreset,
        string? expectedMultipass)
    {
        var settings = Anime4KHelper.GetEncoderSettings(profile);

        Assert.Equal(expectedCq, settings.ConstantQuality);
        Assert.Equal(expectedPreset, settings.Preset);
        Assert.Equal(expectedMultipass, settings.Multipass);
    }

    [Theory]
    [InlineData(0, Anime4KHelper.DefaultMaxBitrate)]
    [InlineData(1_000_000, Anime4KHelper.MinMaxBitrate)]
    [InlineData(90_000_000, 90_000_000)]
    [InlineData(300_000_000, Anime4KHelper.MaxMaxBitrate)]
    public void NormalizeMaxBitrate_UsesDefaultAndBounds(int input, int expected)
        => Assert.Equal(expected, Anime4KHelper.NormalizeMaxBitrate(input));

    [Theory]
    [InlineData(80_000_000, null, 192_000, 80_000_000)]
    [InlineData(80_000_000, 20_000_000, 192_000, 19_808_000)]
    [InlineData(80_000_000, 100_000_000, 192_000, 80_000_000)]
    public void GetSessionVideoBitrate_RespectsAdminAndSessionCeilings(
        int configured,
        int? session,
        int audio,
        int expected)
        => Assert.Equal(expected, Anime4KHelper.GetSessionVideoBitrate(configured, session, audio));

    [Fact]
    public void IsAnimation_InheritsSeriesGenre()
    {
        var episode = new Video();
        var series = new Folder { Genres = ["Animation"] };

        Assert.True(Anime4KHelper.IsAnimation(episode, series));
    }

    [Fact]
    public void IsAnimation_NoAnime4KTagWins()
    {
        var item = new Video { Genres = ["Animation"], Tags = ["Anime4K", "NoAnime4K"] };

        Assert.False(Anime4KHelper.IsAnimation(item, null));
    }

    [Theory]
    [InlineData(1280, 720, 3840, 2160)]
    [InlineData(1440, 1080, 2880, 2160)]
    [InlineData(1920, 800, 3840, 1600)]
    public void TryGetTargetSize_PreservesAspectRatio(int width, int height, int expectedWidth, int expectedHeight)
    {
        var source = CreateSource(width, height);

        Assert.True(Anime4KHelper.TryGetTargetSize(source, out var outputWidth, out var outputHeight));
        Assert.Equal(expectedWidth, outputWidth);
        Assert.Equal(expectedHeight, outputHeight);
    }

    [Fact]
    public void TryGetTargetSize_RejectsHdr()
    {
        var source = CreateSource(1920, 1080);
        source.VideoStream.ColorTransfer = "smpte2084";

        Assert.False(Anime4KHelper.TryGetTargetSize(source, out _, out _));
    }

    [Fact]
    public void TryGetTargetSize_RejectsHighFrameRate()
    {
        var source = CreateSource(1920, 1080);
        source.VideoStream.AverageFrameRate = 60;

        Assert.False(Anime4KHelper.TryGetTargetSize(source, out _, out _));
    }

    private static MediaSourceInfo CreateSource(int width, int height)
        => new()
        {
            Protocol = MediaProtocol.File,
            MediaStreams =
            [
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Width = width,
                    Height = height,
                    AverageFrameRate = 24,
                    ColorTransfer = "bt709"
                }
            ]
        };
}
