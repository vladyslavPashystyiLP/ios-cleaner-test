namespace IosCleanerTest.Services;

public interface ITestDataSeeder
{
    /// <summary>Зберігає в галерею PNG з роздільністю екрана — підпадає під heuristic-критерій скриншота.</summary>
    Task<string> AddScreenshotLikeImageAsync();

    /// <summary>Генерує й зберігає відео з шумових кадрів (високий бітрейт → великий файл).</summary>
    Task<string> AddLargeVideoAsync();

    /// <summary>Зберігає кілька великих PNG для тесту категорії «найважчі файли».</summary>
    Task<string> AddHeavyImagesAsync(int count);

    /// <summary>Для тесту дублікатів: одне випадкове фото двічі (точні копії) + перекодована JPEG-версія.</summary>
    Task<string> AddDuplicatesAsync();
}
