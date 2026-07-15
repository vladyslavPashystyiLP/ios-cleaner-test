using IosCleanerTest.Services;

namespace IosCleanerTest
{
    public partial class Level3Page : ContentPage
    {
        private readonly IPhotoCleanerService _cleaner;
        private readonly ITestDataSeeder _seeder;

        public Level3Page(IPhotoCleanerService cleaner, ITestDataSeeder seeder)
        {
            InitializeComponent();
            _cleaner = cleaner;
            _seeder = seeder;
        }

        private async void OnScanSimilar(object? sender, EventArgs e)
        {
            if (!await _cleaner.RequestAccessAsync())
            {
                SimilarResult.Text = "No photo library access";
                return;
            }

            SimilarResult.Text = "Scanning (computing Vision embeddings)…";
            SimilarList.Clear();
            try
            {
                var result = await _cleaner.FindSimilarPhotosAsync();
                CleanerUi.ShowResult(SimilarResult, SimilarList, result);

                if (result.Items.Count == 0 && result.Diagnostics is not null)
                    await DisplayAlert("Scan diagnostics", result.Diagnostics, "OK");
            }
            catch (Exception ex)
            {
                SimilarResult.Text = $"Error: {ex.Message}";
            }
        }

        private async void OnSeedSimilar(object? sender, EventArgs e)
        {
            if (!await _cleaner.RequestAccessAsync())
            {
                SimilarResult.Text = "No photo library access";
                return;
            }

            SimilarResult.Text = "Adding test data…";
            try
            {
                SimilarResult.Text = await _seeder.AddSimilarPhotosAsync();
            }
            catch (Exception ex)
            {
                SimilarResult.Text = $"Error: {ex.Message}";
            }
        }
    }
}
