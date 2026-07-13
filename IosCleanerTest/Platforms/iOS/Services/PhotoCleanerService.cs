using System.Numerics;
using CoreGraphics;
using Foundation;
using Photos;
using UIKit;

namespace IosCleanerTest.Services;

public class PhotoCleanerService : IPhotoCleanerService
{
    public async Task<bool> RequestAccessAsync()
    {
        var status = await PHPhotoLibrary.RequestAuthorizationAsync(PHAccessLevel.ReadWrite);
        return status is PHAuthorizationStatus.Authorized or PHAuthorizationStatus.Limited;
    }

    public Task<ScanResult> FindScreenshotsAsync()
    {
        // UIKit можна чіпати лише з UI-потоку — знімаємо роздільність екрана до Task.Run
        // (NativeBounds завжди в портретній орієнтації, у пікселях)
        var native = UIScreen.MainScreen.NativeBounds;
        var screenW = (long)native.Width;
        var screenH = (long)native.Height;

        return Task.Run(() =>
        {
            var items = new List<CleanerItem>();
            foreach (var asset in FetchAssets(PHAssetMediaType.Image))
            {
                var isScreenshotSubtype = asset.MediaSubtypes.HasFlag(PHAssetMediaSubtype.Screenshot);

                // Heuristic: PNG з точною роздільністю екрана. Потрібен, бо створеному
                // програмно тестовому асету неможливо виставити subtype — його ставить лише система.
                var resource = PrimaryResource(asset);
                var isPng = resource?.UniformTypeIdentifier == "public.png";
                var w = (long)asset.PixelWidth;
                var h = (long)asset.PixelHeight;
                var matchesScreenSize =
                    (w == screenW && h == screenH) ||
                    (w == screenH && h == screenW);

                if (isScreenshotSubtype || (isPng && matchesScreenSize))
                    items.Add(ToItem(asset, resource));
            }

            return new ScanResult("Скриншоти", items);
        });
    }

    public Task<ScanResult> FindLivePhotosAsync() => Task.Run(() =>
    {
        var items = FetchAssets(PHAssetMediaType.Image)
            .Where(a => a.MediaSubtypes.HasFlag(PHAssetMediaSubtype.PhotoLive))
            .Select(a => ToItem(a, PrimaryResource(a)))
            .ToList();

        return new ScanResult("Live Photos", items);
    });

    public Task<ScanResult> FindLargeVideosAsync(long minBytes) => Task.Run(() =>
    {
        var items = FetchAssets(PHAssetMediaType.Video)
            .Select(a => ToItem(a, PrimaryResource(a)))
            .Where(i => i.SizeBytes >= minBytes)
            .OrderByDescending(i => i.SizeBytes)
            .ToList();

        return new ScanResult("Великі відео", items);
    });

    public Task<ScanResult> FindHeaviestAssetsAsync(int topN) => Task.Run(() =>
    {
        var items = FetchAssets(PHAssetMediaType.Image)
            .Concat(FetchAssets(PHAssetMediaType.Video))
            .Select(a => ToItem(a, PrimaryResource(a)))
            .OrderByDescending(i => i.SizeBytes)
            .Take(topN)
            .ToList();

        return new ScanResult("Найважчі файли", items);
    });

    // Хеші з відстанню Геммінга ≤ порога вважаємо однією групою.
    // 0 ловить лише побітово стабільні пари; 6 — типовий поріг для dHash,
    // покриває перекодовані (PNG→JPEG) та злегка стиснуті копії.
    private const int DuplicateHammingThreshold = 6;

    public Task<ScanResult> FindDuplicatesAsync() => Task.Run(() =>
    {
        var entries = new List<(PHAsset Asset, ulong Hash)>();
        foreach (var asset in FetchAssets(PHAssetMediaType.Image))
        {
            var hash = ComputeDHash(asset);
            if (hash is not null)
                entries.Add((asset, hash.Value));
        }

        // Простий O(n²)-кластеринг — для тестових обсягів достатньо
        var groupIndex = new int[entries.Count];
        Array.Fill(groupIndex, -1);
        var groups = new List<List<int>>();

        for (var i = 0; i < entries.Count; i++)
        {
            if (groupIndex[i] == -1)
            {
                groupIndex[i] = groups.Count;
                groups.Add([i]);
            }

            for (var j = i + 1; j < entries.Count; j++)
            {
                if (groupIndex[j] == -1 &&
                    BitOperations.PopCount(entries[i].Hash ^ entries[j].Hash) <= DuplicateHammingThreshold)
                {
                    groupIndex[j] = groupIndex[i];
                    groups[groupIndex[i]].Add(j);
                }
            }
        }

        var items = new List<CleanerItem>();
        foreach (var group in groups.Where(g => g.Count >= 2))
        {
            var original = ToItem(entries[group[0]].Asset, PrimaryResource(entries[group[0]].Asset));
            foreach (var index in group.Skip(1))
            {
                var (asset, _) = entries[index];
                var item = ToItem(asset, PrimaryResource(asset));
                items.Add(item with
                {
                    Name = $"{item.Name} (дубль {original.Name})",
                    Thumbnail = GetThumbnailJpeg(asset),
                });
            }
        }

        return new ScanResult("Дублікати", items);
    });

    private static byte[]? GetThumbnailJpeg(PHAsset asset)
    {
        UIImage? image = null;
        var options = new PHImageRequestOptions
        {
            Synchronous = true,
            DeliveryMode = PHImageRequestOptionsDeliveryMode.HighQualityFormat,
            ResizeMode = PHImageRequestOptionsResizeMode.Fast,
            NetworkAccessAllowed = true,
        };
        PHImageManager.DefaultManager.RequestImageForAsset(
            asset, new CGSize(200, 200), PHImageContentMode.AspectFill, options,
            (result, _) => image = result);

        return image?.AsJPEG(0.8f)?.ToArray();
    }

    /// <summary>64-бітний dHash: прев'ю → 9x8 у відтінках сірого → біт на кожну пару сусідніх пікселів рядка.</summary>
    private static ulong? ComputeDHash(PHAsset asset)
    {
        UIImage? image = null;
        var options = new PHImageRequestOptions
        {
            Synchronous = true,
            DeliveryMode = PHImageRequestOptionsDeliveryMode.HighQualityFormat,
            ResizeMode = PHImageRequestOptionsResizeMode.Exact,
            NetworkAccessAllowed = true,
        };
        PHImageManager.DefaultManager.RequestImageForAsset(
            asset, new CGSize(64, 64), PHImageContentMode.AspectFill, options,
            (result, _) => image = result);

        if (image?.CGImage is not { } cgImage)
            return null;

        const int w = 9, h = 8;
        var pixels = new byte[w * h];
        using var colorSpace = CGColorSpace.CreateDeviceGray();
        using var context = new CGBitmapContext(pixels, w, h, 8, w, colorSpace, CGImageAlphaInfo.None);
        context.DrawImage(new CGRect(0, 0, w, h), cgImage);

        ulong hash = 0;
        var bit = 0;
        for (var row = 0; row < h; row++)
            for (var col = 0; col < w - 1; col++, bit++)
                if (pixels[row * w + col] > pixels[row * w + col + 1])
                    hash |= 1UL << bit;

        return hash;
    }

    private static IEnumerable<PHAsset> FetchAssets(PHAssetMediaType type)
    {
        var result = PHAsset.FetchAssets(type, new PHFetchOptions());
        for (nint i = 0; i < result.Count; i++)
            yield return (PHAsset)result[i];
    }

    private static PHAssetResource? PrimaryResource(PHAsset asset)
    {
        var resources = PHAssetResource.GetAssetResources(asset);
        return resources.FirstOrDefault(r => r.ResourceType
                   is PHAssetResourceType.Photo
                   or PHAssetResourceType.Video
                   or PHAssetResourceType.FullSizePhoto)
               ?? resources.FirstOrDefault();
    }

    private static CleanerItem ToItem(PHAsset asset, PHAssetResource? resource)
    {
        var name = resource?.OriginalFilename ?? asset.LocalIdentifier;
        // Публічної властивості розміру немає; "fileSize" через KVC — усталений підхід фото-утиліт
        long size = 0;
        try
        {
            if (resource?.ValueForKey(new NSString("fileSize")) is NSNumber n)
                size = n.Int64Value;
        }
        catch (Exception)
        {
            // ключ недоступний — залишаємо 0, скан не валимо
        }

        return new CleanerItem(asset.LocalIdentifier, name, size);
    }
}
