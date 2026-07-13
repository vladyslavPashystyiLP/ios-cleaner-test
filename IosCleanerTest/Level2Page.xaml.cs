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
                DuplicatesResult.Text = result.Items.Count == 0
                    ? "Nothing found"
                    : $"Found {result.Items.Count} items, {result.TotalBytes / 1024.0 / 1024.0:F1} MB:";

                foreach (var item in result.Items)
                    DuplicatesList.Add(BuildItemRow(item));
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

        private static IView BuildItemRow(CleanerItem item)
        {
            var row = new HorizontalStackLayout { Spacing = 10 };

            if (item.Thumbnail is { Length: > 0 } bytes)
            {
                row.Add(new Image
                {
                    WidthRequest = 90,
                    HeightRequest = 90,
                    Aspect = Aspect.AspectFill,
                    Source = ImageSource.FromStream(() => new MemoryStream(bytes)),
                });
            }

            row.Add(new Label
            {
                Text = $"{item.Name}\n{item.SizeBytes / 1024.0 / 1024.0:F1} MB",
                FontSize = 12,
                TextColor = Colors.Gray,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.CharacterWrap,
                MaximumWidthRequest = 220,
            });

            return row;
        }
    }
}
