namespace IosCleanerTest.Services;

public record CleanerItem(string Id, string Name, long SizeBytes);

public record ScanResult(string Category, IReadOnlyList<CleanerItem> Items)
{
    public long TotalBytes => Items.Sum(i => i.SizeBytes);

    public string Summary
    {
        get
        {
            if (Items.Count == 0)
                return "Нічого не знайдено";

            var lines = Items.Take(5)
                .Select(i => $"• {i.Name} — {i.SizeBytes / 1024.0 / 1024.0:F1} МБ");
            var list = string.Join(Environment.NewLine, lines);
            if (Items.Count > 5)
                list += $"{Environment.NewLine}… і ще {Items.Count - 5}";

            return $"Знайдено {Items.Count} шт., {TotalBytes / 1024.0 / 1024.0:F1} МБ:{Environment.NewLine}{list}";
        }
    }
}
