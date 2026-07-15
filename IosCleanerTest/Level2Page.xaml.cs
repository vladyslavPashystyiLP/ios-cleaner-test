using IosCleanerTest.Services;

namespace IosCleanerTest
{
    public partial class Level2Page : ContentPage
    {
        private readonly IPhotoCleanerService _cleaner;
        private readonly ITestDataSeeder _seeder;

        public Level2Page(IPhotoCleanerService cleaner, ITestDataSeeder seeder)
        {
            InitializeComponent();
            _cleaner = cleaner;
            _seeder = seeder;
        }

        private async void OnScanDuplicates(object? sender, EventArgs e)
        {
            if (!await _cleaner.RequestAccessAsync())
            {
                DuplicatesResult.Text = "No photo library access";
                return;
            }

            DuplicatesResult.Text = "Scanning (hashing all photos)…";
            DuplicatesList.Clear();
            try
            {
                var result = await _cleaner.FindDuplicatesAsync();
                CleanerUi.ShowResult(DuplicatesResult, DuplicatesList, result);

                if (result.Items.Count == 0 && result.Diagnostics is not null)
                    await DisplayAlert("Scan diagnostics", result.Diagnostics, "OK");
            }
            catch (Exception ex)
            {
                DuplicatesResult.Text = $"Error: {ex.Message}";
            }
        }

        private async void OnSeedDuplicates(object? sender, EventArgs e)
        {
            if (!await _cleaner.RequestAccessAsync())
            {
                DuplicatesResult.Text = "No photo library access";
                return;
            }

            DuplicatesResult.Text = "Adding test data…";
            try
            {
                DuplicatesResult.Text = await _seeder.AddDuplicatesAsync();
            }
            catch (Exception ex)
            {
                DuplicatesResult.Text = $"Error: {ex.Message}";
            }
        }
    }
}
