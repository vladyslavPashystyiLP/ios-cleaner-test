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
                DuplicatesResult.Text = "Немає доступу до галереї";
                return;
            }

            DuplicatesResult.Text = "Сканую (рахую хеші всіх фото)…";
            try
            {
                var result = await _cleaner.FindDuplicatesAsync();
                DuplicatesResult.Text = result.Summary;
            }
            catch (Exception ex)
            {
                DuplicatesResult.Text = $"Помилка: {ex.Message}";
            }
        }

        private async void OnSeedDuplicates(object? sender, EventArgs e)
        {
            if (!await _cleaner.RequestAccessAsync())
            {
                DuplicatesResult.Text = "Немає доступу до галереї";
                return;
            }

            DuplicatesResult.Text = "Додаю тестові дані…";
            try
            {
                DuplicatesResult.Text = await _seeder.AddDuplicatesAsync();
            }
            catch (Exception ex)
            {
                DuplicatesResult.Text = $"Помилка: {ex.Message}";
            }
        }
    }
}
