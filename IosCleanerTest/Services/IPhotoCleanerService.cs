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

    /// <summary>
    /// Рівень 2: дублікати через перцептивний dHash на прев'ю.
    /// Хеші з відстанню Геммінга ≤ порога вважаються копіями; у результат
    /// потрапляють усі файли групи, крім першого («оригінал» лишається).
    /// </summary>
    Task<ScanResult> FindDuplicatesAsync();
}
