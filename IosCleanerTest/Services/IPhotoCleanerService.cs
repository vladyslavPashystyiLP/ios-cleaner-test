namespace IosCleanerTest.Services;

public interface IPhotoCleanerService
{
    /// <summary>Запитує доступ до фотобібліотеки. Повертає true, якщо доступ надано (повний або limited).</summary>
    Task<bool> RequestAccessAsync();

    /// <summary>Скриншоти: PHAssetMediaSubtype.PhotoScreenshot або PNG з роздільністю екрана (heuristic для тестових даних).</summary>
    Task<ScanResult> FindScreenshotsAsync();

    /// <summary>Live Photos за PHAssetMediaSubtype.PhotoLive.</summary>
    Task<ScanResult> FindLivePhotosAsync();

    /// <summary>Відео з розміром файлу не менше minBytes.</summary>
    Task<ScanResult> FindLargeVideosAsync(long minBytes);

    /// <summary>Топ-N найважчих асетів бібліотеки (фото і відео).</summary>
    Task<ScanResult> FindHeaviestAssetsAsync(int topN);
}
