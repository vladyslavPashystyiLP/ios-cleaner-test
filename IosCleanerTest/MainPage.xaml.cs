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
            RunScan(ScreenshotsResult, ScreenshotsList, () => _cleaner.FindScreenshotsAsync());

        private void OnScanLivePhotos(object? sender, EventArgs e) =>
            RunScan(LivePhotosResult, LivePhotosList, () => _cleaner.FindLivePhotosAsync());

        private void OnScanLargeVideos(object? sender, EventArgs e) =>
            RunScan(LargeVideosResult, LargeVideosList, () => _cleaner.FindLargeVideosAsync(LargeVideoMinBytes));

        private void OnScanHeaviest(object? sender, EventArgs e) =>
            RunScan(HeaviestResult, HeaviestList, () => _cleaner.FindHeaviestAssetsAsync(HeaviestTopN));

        private void OnSeedScreenshot(object? sender, EventArgs e) =>
            RunSeed(ScreenshotsResult, () => _seeder.AddScreenshotLikeImageAsync());

        private async void OnSeedLivePhoto(object? sender, EventArgs e) =>
            await DisplayAlert("Live Photos",
                "Live Photos can't be created programmatically: the system requires paired photo+video metadata. " +
                "Test this category on a real device with captured Live Photos.", "OK");

        private void OnSeedLargeVideo(object? sender, EventArgs e) =>
            RunSeed(LargeVideosResult, () => _seeder.AddLargeVideoAsync());

        private void OnSeedHeavyImages(object? sender, EventArgs e) =>
            RunSeed(HeaviestResult, () => _seeder.AddHeavyImagesAsync(3));

        private async void RunScan(Label output, VerticalStackLayout list, Func<Task<ScanResult>> scan)
        {
            if (!await EnsureAccessAsync())
                return;

            output.Text = "Scanning…";
            list.Clear();
            try
            {
                var result = await scan();
                CleanerUi.ShowResult(output, list, result);
            }
            catch (Exception ex)
            {
                output.Text = $"Error: {ex.Message}";
            }
        }

        private async void RunSeed(Label output, Func<Task<string>> seed)
        {
            if (!await EnsureAccessAsync())
                return;

            output.Text = "Adding test data…";
            try
            {
                output.Text = await seed();
            }
            catch (Exception ex)
            {
                output.Text = $"Error: {ex.Message}";
            }
        }

        private async Task<bool> EnsureAccessAsync()
        {
            var granted = await _cleaner.RequestAccessAsync();
            AccessStatusLabel.Text = granted
                ? "Photo library access: granted"
                : "Photo library access: denied (check settings or platform)";
            return granted;
        }
    }
}
