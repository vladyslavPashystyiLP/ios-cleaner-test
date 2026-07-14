using IosCleanerTest.Services;

namespace IosCleanerTest
{
    public partial class Level4Page : ContentPage
    {
        private readonly IPhotoCleanerService _cleaner;
        private readonly ITestDataSeeder _seeder;

        public Level4Page(IPhotoCleanerService cleaner, ITestDataSeeder seeder)
        {
            InitializeComponent();
            _cleaner = cleaner;
            _seeder = seeder;
        }

        private async void OnScanBlurry(object? sender, EventArgs e)
        {
            if (!await _cleaner.RequestAccessAsync())
            {
                BlurryResult.Text = "No photo library access";
                return;
            }

            BlurryResult.Text = "Scanning (computing sharpness)…";
            BlurryList.Clear();
            try
            {
                var result = await _cleaner.FindBlurryPhotosAsync();
                CleanerUi.ShowResult(BlurryResult, BlurryList, result);
            }
            catch (Exception ex)
            {
                BlurryResult.Text = $"Error: {ex.Message}";
            }
        }

        private async void OnSeedBlurry(object? sender, EventArgs e)
        {
            if (!await _cleaner.RequestAccessAsync())
            {
                BlurryResult.Text = "No photo library access";
                return;
            }

            BlurryResult.Text = "Adding test data…";
            try
            {
                BlurryResult.Text = await _seeder.AddBlurryImageAsync();
            }
            catch (Exception ex)
            {
                BlurryResult.Text = $"Error: {ex.Message}";
            }
        }
    }
}
