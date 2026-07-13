namespace IosCleanerTest.Services;

public record CleanerItem(string Id, string Name, long SizeBytes, byte[]? Thumbnail = null);

public record ScanResult(string Category, IReadOnlyList<CleanerItem> Items, string? Diagnostics = null)
{
    public long TotalBytes => Items.Sum(i => i.SizeBytes);

    public string Summary
    {
        get
        {
            if (Items.Count == 0)
                return "Nothing found";

            var lines = Items.Take(5)
                .Select(i => $"• {i.Name} — {i.SizeBytes / 1024.0 / 1024.0:F1} MB");
            var list = string.Join(Environment.NewLine, lines);
            if (Items.Count > 5)
                list += $"{Environment.NewLine}… and {Items.Count - 5} more";

            return $"Found {Items.Count} items, {TotalBytes / 1024.0 / 1024.0:F1} MB:{Environment.NewLine}{list}";
        }
    }
}
