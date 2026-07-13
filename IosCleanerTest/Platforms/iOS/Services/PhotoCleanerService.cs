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
