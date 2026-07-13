using System.Numerics;
using CoreGraphics;
using Foundation;
using Photos;
using UIKit;
using Vision;

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
        // UIKit must be touched on the UI thread only — capture screen resolution before Task.Run
        // (NativeBounds is always portrait-oriented, in pixels)
        var native = UIScreen.MainScreen.NativeBounds;
        var screenW = (long)native.Width;
        var screenH = (long)native.Height;

        return Task.Run(() =>
        {
            var items = new List<CleanerItem>();
            foreach (var asset in FetchAssets(PHAssetMediaType.Image))
            {
                var isScreenshotSubtype = asset.MediaSubtypes.HasFlag(PHAssetMediaSubtype.Screenshot);

                // Heuristic: a PNG with exact screen resolution. Needed because a programmatically
                // created test asset cannot get the subtype — only the system sets it.
                var resource = PrimaryResource(asset);
                var isPng = resource?.UniformTypeIdentifier == "public.png";
                var w = (long)asset.PixelWidth;
                var h = (long)asset.PixelHeight;
                var matchesScreenSize =
                    (w == screenW && h == screenH) ||
                    (w == screenH && h == screenW);

                if (isScreenshotSubtype || (isPng && matchesScreenSize))
                    items.Add(ToItem(asset, resource) with { Thumbnail = GetThumbnailJpeg(asset) });
            }

            return new ScanResult("Screenshots", items);
        });
    }

    public Task<ScanResult> FindLivePhotosAsync() => Task.Run(() =>
    {
        var items = FetchAssets(PHAssetMediaType.Image)
            .Where(a => a.MediaSubtypes.HasFlag(PHAssetMediaSubtype.PhotoLive))
            .Select(a => ToItem(a, PrimaryResource(a)) with { Thumbnail = GetThumbnailJpeg(a) })
            .ToList();

        return new ScanResult("Live Photos", items);
    });

    public Task<ScanResult> FindLargeVideosAsync(long minBytes) => Task.Run(() =>
    {
        // Thumbnails are attached after filtering so we don't fetch previews of the whole library
        var items = FetchAssets(PHAssetMediaType.Video)
            .Select(a => (Asset: a, Item: ToItem(a, PrimaryResource(a))))
            .Where(x => x.Item.SizeBytes >= minBytes)
            .OrderByDescending(x => x.Item.SizeBytes)
            .Select(x => x.Item with { Thumbnail = GetThumbnailJpeg(x.Asset) })
            .ToList();

        return new ScanResult("Large videos", items);
    });

    public Task<ScanResult> FindHeaviestAssetsAsync(int topN) => Task.Run(() =>
    {
        var items = FetchAssets(PHAssetMediaType.Image)
            .Concat(FetchAssets(PHAssetMediaType.Video))
            .Select(a => (Asset: a, Item: ToItem(a, PrimaryResource(a))))
            .OrderByDescending(x => x.Item.SizeBytes)
            .Take(topN)
            .Select(x => x.Item with { Thumbnail = GetThumbnailJpeg(x.Asset) })
            .ToList();

        return new ScanResult("Heaviest files", items);
    });

    // Hashes within the Hamming distance threshold are treated as one group.
    // 0 catches only bit-identical pairs; 6 is a typical dHash threshold that
    // also covers re-encoded (PNG→JPEG) and slightly compressed copies.
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

        // Simple O(n²) clustering — enough for test-scale libraries
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
                    Name = $"{item.Name} (duplicate of {original.Name})",
                    Thumbnail = GetThumbnailJpeg(asset),
                });
            }
        }

        return new ScanResult("Duplicates", items);
    });

    // Euclidean distance between Vision feature print vectors: 0 = identical.
    // The absolute scale depends on the feature print revision, so the scan reports
    // observed min/max distances in Diagnostics — use them to calibrate this value.
    private const float SimilarDistanceThreshold = 0.6f;

    public Task<ScanResult> FindSimilarPhotosAsync() => Task.Run(() =>
    {
        var total = 0;
        string? firstError = null;
        var entries = new List<(PHAsset Asset, float[] Vector)>();

        foreach (var asset in FetchAssets(PHAssetMediaType.Image))
        {
            total++;
            var vector = ComputeFeatureVector(asset, ref firstError);
            if (vector is not null)
                entries.Add((asset, vector));
        }

        var groupIndex = new int[entries.Count];
        Array.Fill(groupIndex, -1);
        var groups = new List<List<int>>();
        float minDist = float.MaxValue, maxDist = 0;

        for (var i = 0; i < entries.Count; i++)
        {
            if (groupIndex[i] == -1)
            {
                groupIndex[i] = groups.Count;
                groups.Add([i]);
            }

            for (var j = i + 1; j < entries.Count; j++)
            {
                if (entries[i].Vector.Length != entries[j].Vector.Length)
                    continue;

                var distance = EuclideanDistance(entries[i].Vector, entries[j].Vector);
                minDist = Math.Min(minDist, distance);
                maxDist = Math.Max(maxDist, distance);

                if (groupIndex[j] == -1 && distance <= SimilarDistanceThreshold)
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
                var asset = entries[index].Asset;
                var item = ToItem(asset, PrimaryResource(asset));
                items.Add(item with
                {
                    Name = $"{item.Name} (similar to {original.Name})",
                    Thumbnail = GetThumbnailJpeg(asset),
                });
            }
        }

        var stats = entries.Count > 1 && minDist < float.MaxValue
            ? $", distances {minDist:F2}–{maxDist:F2} (threshold {SimilarDistanceThreshold:F2})"
            : "";
        var error = firstError is null ? "" : $", first error: {firstError}";
        var diagnostics = $"[photos {total}, embeddings {entries.Count}{stats}{error}]";

        return new ScanResult("Similar photos", items, diagnostics);
    });

    private static float[]? ComputeFeatureVector(PHAsset asset, ref string? firstError)
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
            asset, new CGSize(224, 224), PHImageContentMode.AspectFill, options,
            (result, _) => image = result);

        if (image?.CGImage is not { } cgImage)
        {
            firstError ??= "no preview image";
            return null;
        }

        using var handler = new VNImageRequestHandler(cgImage, new VNImageOptions());
        using var request = new VNGenerateImageFeaturePrintRequest((VNRequestCompletionHandler?)null);
        request.UsesCpuOnly = true; // no GPU/ANE on the simulator

        if (!handler.Perform([request], out var error))
        {
            firstError ??= error?.LocalizedDescription ?? "Perform failed";
            return null;
        }

        var observation = request.GetResults<VNFeaturePrintObservation>()?.FirstOrDefault();
        if (observation is null)
        {
            firstError ??= "no feature print observation";
            return null;
        }

        // Copy the raw float vector out of the observation
        var count = (int)observation.ElementCount;
        var vector = new float[count];
        System.Runtime.InteropServices.Marshal.Copy(observation.Data.Bytes, vector, 0, count);
        return vector;
    }

    private static float EuclideanDistance(float[] a, float[] b)
    {
        double sum = 0;
        for (var i = 0; i < a.Length; i++)
        {
            var d = a[i] - b[i];
            sum += d * d;
        }

        return (float)Math.Sqrt(sum);
    }

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

    /// <summary>64-bit dHash: preview → 9x8 grayscale → one bit per adjacent pixel pair in a row.</summary>
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
        // No public size property; "fileSize" via KVC is the established approach in photo utilities
        long size = 0;
        try
        {
            if (resource?.ValueForKey(new NSString("fileSize")) is NSNumber n)
                size = n.Int64Value;
        }
        catch (Exception)
        {
            // key unavailable — keep 0, don't fail the scan
        }

        return new CleanerItem(asset.LocalIdentifier, name, size);
    }
}
