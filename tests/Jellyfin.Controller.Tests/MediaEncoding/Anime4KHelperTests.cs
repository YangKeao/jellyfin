using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Xunit;

namespace Jellyfin.Controller.Tests.MediaEncoding;

public class Anime4KHelperTests
{
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
