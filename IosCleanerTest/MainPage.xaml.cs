using IosCleanerTest.Services;

namespace IosCleanerTest
{
    public partial class MainPage : ContentPage
    {
        private const long LargeVideoMinBytes = 10 * 1024 * 1024;
        private const int HeaviestTopN = 10;

        private readonly IPhotoCleanerService _cleaner;
        private readonly ITestDataSeeder _seeder;

        public MainPage(IPhotoCleanerService cleaner, ITestDataSeeder seeder)
        {
            InitializeComponent();
            _cleaner = cleaner;
            _seeder = seeder;
        }

        private void OnScanScreenshots(object? sender, EventArgs e) =>
            RunScan(ScreenshotsResult, () => _cleaner.FindScreenshotsAsync());

        private void OnScanLivePhotos(object? sender, EventArgs e) =>
            RunScan(LivePhotosResult, () => _cleaner.FindLivePhotosAsync());

        private void OnScanLargeVideos(object? sender, EventArgs e) =>
            RunScan(LargeVideosResult, () => _cleaner.FindLargeVideosAsync(LargeVideoMinBytes));

        private void OnScanHeaviest(object? sender, EventArgs e) =>
            RunScan(HeaviestResult, () => _cleaner.FindHeaviestAssetsAsync(HeaviestTopN));

        private void OnSeedScreenshot(object? sender, EventArgs e) =>
            RunSeed(ScreenshotsResult, () => _seeder.AddScreenshotLikeImageAsync());

        private async void OnSeedLivePhoto(object? sender, EventArgs e) =>
            await DisplayAlert("Live Photos",
                "Створити Live Photo програмно не можна: система вимагає спарених метаданих фото+відео. " +
                "Ця категорія тестується на реальному пристрої зі знятими Live Photos.", "OK");

        private void OnSeedLargeVideo(object? sender, EventArgs e) =>
            RunSeed(LargeVideosResult, () => _seeder.AddLargeVideoAsync());

        private void OnSeedHeavyImages(object? sender, EventArgs e) =>
            RunSeed(HeaviestResult, () => _seeder.AddHeavyImagesAsync(3));

        private async void RunScan(Label output, Func<Task<ScanResult>> scan)
        {
            if (!await EnsureAccessAsync())
                return;

            output.Text = "Сканую…";
            try
            {
                var result = await scan();
                output.Text = result.Summary;
            }
            catch (Exception ex)
            {
                output.Text = $"Помилка: {ex.Message}";
            }
        }

        private async void RunSeed(Label output, Func<Task<string>> seed)
        {
            if (!await EnsureAccessAsync())
                return;

            output.Text = "Додаю тестові дані…";
            try
            {
                output.Text = await seed();
            }
            catch (Exception ex)
            {
                output.Text = $"Помилка: {ex.Message}";
            }
        }

        private async Task<bool> EnsureAccessAsync()
        {
            var granted = await _cleaner.RequestAccessAsync();
            AccessStatusLabel.Text = granted
                ? "Доступ до галереї: надано"
                : "Доступ до галереї: відхилено (перевір налаштування або платформу)";
            return granted;
        }
    }
}
