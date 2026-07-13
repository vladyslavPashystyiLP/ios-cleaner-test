namespace IosCleanerTest.Services;

// Заглушки для не-iOS платформ: дозволяють збирати й дебажити UI на Windows.
public class UnsupportedPhotoCleanerService : IPhotoCleanerService
{
    private static ScanResult Empty(string category) => new(category, []);

    public Task<bool> RequestAccessAsync() => Task.FromResult(false);
    public Task<ScanResult> FindScreenshotsAsync() => Task.FromResult(Empty("Скриншоти"));
    public Task<ScanResult> FindLivePhotosAsync() => Task.FromResult(Empty("Live Photos"));
    public Task<ScanResult> FindLargeVideosAsync(long minBytes) => Task.FromResult(Empty("Великі відео"));
    public Task<ScanResult> FindHeaviestAssetsAsync(int topN) => Task.FromResult(Empty("Найважчі файли"));
}

public class UnsupportedTestDataSeeder : ITestDataSeeder
{
    private const string Message = "Доступно лише на iOS";

    public Task<string> AddScreenshotLikeImageAsync() => Task.FromResult(Message);
    public Task<string> AddLargeVideoAsync() => Task.FromResult(Message);
    public Task<string> AddHeavyImagesAsync(int count) => Task.FromResult(Message);
}
