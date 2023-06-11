using FluentAssertions;
using HttpDataClient;
using HttpDataClient.Settings;
using NUnit.Framework;

namespace HttpDataClientTests;

[TestFixture]
public class Tests
{
    [SetUp]
    public void Setup()
    {
        httpDataLoader = new HttpDataLoader(new HttpDataLoaderSettings
        {
            BaseUrl = "https://www.google.com"
        });
        testPicture = File.ReadAllBytes("GoogleLogo.jpg");
    }

    [Test]
    public void TestForJustGet()
    {
        var downloadResult = HttpDataLoader.JustGet("https://www.google.com/images/branding/googlelogo/2x/googlelogo_color_92x30dp.png");
        downloadResult.Data.Should().Equal(testPicture);
    }

    [Test]
    public void TestForJustGetSuccess()
    {
        var downloadResult = HttpDataLoader.JustGetSuccess("https://www.google.com/images/branding/googlelogo/2x/googlelogo_color_92x30dp.png");
        downloadResult.Data.Should().Equal(testPicture);
    }

    [Test]
    public void TestForGet()
    {
        var downloadResult = httpDataLoader.Get("/images/branding/googlelogo/2x/googlelogo_color_92x30dp.png");
        downloadResult.Data.Should().Equal(testPicture);
    }

    [Test]
    public void TestForGetSuccess()
    {
        var downloadResult = httpDataLoader.GetSuccess("/images/branding/googlelogo/2x/googlelogo_color_92x30dp.png");
        downloadResult.Data.Should().Equal(testPicture);
    }

    [Test]
    public void TestForGetStream()
    {
        var downloadStreamResult = httpDataLoader.GetStream("/images/branding/googlelogo/2x/googlelogo_color_92x30dp.png");
        using var dataStream = downloadStreamResult.Stream;
        var dataBytes = GetBytes(dataStream!);
        dataBytes.Should().Equal(testPicture);
    }

    [Test]
    public void TestForGetStreamSuccess()
    {
        var downloadStreamResult = httpDataLoader.GetStreamSuccess("/images/branding/googlelogo/2x/googlelogo_color_92x30dp.png");
        using var dataStream = downloadStreamResult.Stream;
        var dataBytes = GetBytes(dataStream!);
        dataBytes.Should().Equal(testPicture);
    }

    private static IEnumerable<byte> GetBytes(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

#pragma warning disable CS8618
    private HttpDataLoader httpDataLoader;
    private byte[] testPicture;
#pragma warning restore CS8618
}
