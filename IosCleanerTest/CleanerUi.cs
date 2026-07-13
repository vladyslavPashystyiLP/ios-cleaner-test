using IosCleanerTest.Services;

namespace IosCleanerTest;

/// <summary>Shared rendering of scan results: summary line + rows with thumbnails.</summary>
public static class CleanerUi
{
    private const int MaxRows = 20;

    public static void ShowResult(Label summary, VerticalStackLayout list, ScanResult result)
    {
        list.Clear();
        summary.Text = result.Items.Count == 0
            ? "Nothing found"
            : $"Found {result.Items.Count} items, {result.TotalBytes / 1024.0 / 1024.0:F1} MB:";

        if (result.Diagnostics is not null)
            summary.Text += Environment.NewLine + result.Diagnostics;

        foreach (var item in result.Items.Take(MaxRows))
            list.Add(BuildItemRow(item));

        if (result.Items.Count > MaxRows)
            list.Add(new Label
            {
                Text = $"… and {result.Items.Count - MaxRows} more",
                FontSize = 12,
                TextColor = Colors.Gray,
            });
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
