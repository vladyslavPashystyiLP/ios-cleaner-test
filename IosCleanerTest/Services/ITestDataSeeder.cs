namespace IosCleanerTest.Services;

public interface ITestDataSeeder
{
    /// <summary>Saves a screen-resolution PNG to the library — matches the screenshot heuristic.</summary>
    Task<string> AddScreenshotLikeImageAsync();

    /// <summary>Generates and saves a video of noise frames (high bitrate → large file).</summary>
    Task<string> AddLargeVideoAsync();

    /// <summary>Saves several large PNGs to test the "heaviest files" category.</summary>
    Task<string> AddHeavyImagesAsync(int count);

    /// <summary>For duplicate testing: one random photo saved twice (exact copies) + a re-encoded JPEG version.</summary>
    Task<string> AddDuplicatesAsync();

    /// <summary>For similar-photos testing: one scene saved twice with a small visual difference.</summary>
    Task<string> AddSimilarPhotosAsync();
}
