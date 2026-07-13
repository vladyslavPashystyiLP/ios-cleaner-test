namespace IosCleanerTest.Services;

// Stubs for non-iOS platforms: allow building and debugging the UI on Windows.
public class UnsupportedPhotoCleanerService : IPhotoCleanerService
{
    private static ScanResult Empty(string category) => new(category, []);

    public Task<bool> RequestAccessAsync() => Task.FromResult(false);
    public Task<ScanResult> FindScreenshotsAsync() => Task.FromResult(Empty("Screenshots"));
    public Task<ScanResult> FindLivePhotosAsync() => Task.FromResult(Empty("Live Photos"));
    public Task<ScanResult> FindLargeVideosAsync(long minBytes) => Task.FromResult(Empty("Large videos"));
    public Task<ScanResult> FindHeaviestAssetsAsync(int topN) => Task.FromResult(Empty("Heaviest files"));
    public Task<ScanResult> FindDuplicatesAsync() => Task.FromResult(Empty("Duplicates"));
}

public class UnsupportedTestDataSeeder : ITestDataSeeder
{
    private const string Message = "Available on iOS only";

    public Task<string> AddScreenshotLikeImageAsync() => Task.FromResult(Message);
    public Task<string> AddLargeVideoAsync() => Task.FromResult(Message);
    public Task<string> AddHeavyImagesAsync(int count) => Task.FromResult(Message);
    public Task<string> AddDuplicatesAsync() => Task.FromResult(Message);
}
