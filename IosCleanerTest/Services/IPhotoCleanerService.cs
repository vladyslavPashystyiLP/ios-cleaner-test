namespace IosCleanerTest.Services;

public interface IPhotoCleanerService
{
    /// <summary>Requests photo library access. Returns true if granted (full or limited).</summary>
    Task<bool> RequestAccessAsync();

    /// <summary>Screenshots: PHAssetMediaSubtype.Screenshot, or a PNG with exact screen resolution (heuristic for seeded test data).</summary>
    Task<ScanResult> FindScreenshotsAsync();

    /// <summary>Live Photos via PHAssetMediaSubtype.PhotoLive.</summary>
    Task<ScanResult> FindLivePhotosAsync();

    /// <summary>Videos with file size of at least minBytes.</summary>
    Task<ScanResult> FindLargeVideosAsync(long minBytes);

    /// <summary>Top-N heaviest assets in the library (photos and videos).</summary>
    Task<ScanResult> FindHeaviestAssetsAsync(int topN);

    /// <summary>
    /// Level 2: duplicates via perceptual dHash of previews.
    /// Hashes within the Hamming distance threshold are treated as copies; all files
    /// of a group except the first one (the "original") end up in the result.
    /// </summary>
    Task<ScanResult> FindDuplicatesAsync();

    /// <summary>
    /// Level 3: visually similar photos via Vision feature print embeddings.
    /// Photos whose embedding distance is below the threshold are grouped;
    /// the first file of each group stays as the reference.
    /// </summary>
    Task<ScanResult> FindSimilarPhotosAsync();
}
