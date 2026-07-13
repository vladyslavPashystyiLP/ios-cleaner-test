using System.Runtime.InteropServices;
using AVFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using Foundation;
using Photos;
using UIKit;

namespace IosCleanerTest.Services;

public class TestDataSeeder : ITestDataSeeder
{
    public async Task<string> AddScreenshotLikeImageAsync()
    {
        var bounds = UIScreen.MainScreen.Bounds;
        var format = UIGraphicsImageRendererFormat.DefaultFormat;
        format.Scale = UIScreen.MainScreen.Scale;
        format.Opaque = true;

        var renderer = new UIGraphicsImageRenderer(bounds.Size, format);
        var png = renderer.CreatePng(ctx =>
        {
            UIColor.SystemIndigo.SetFill();
            ctx.FillRect(bounds);
        });

        await PerformChangesAsync(() =>
        {
            var request = PHAssetCreationRequest.CreationRequestForAsset();
            request.AddResource(PHAssetResourceType.Photo, png, new PHAssetResourceCreationOptions
            {
                OriginalFilename = $"TestScreenshot_{DateTime.Now:HHmmss}.png",
            });
        });

        return $"Saved PNG {bounds.Size.Width * format.Scale}x{bounds.Size.Height * format.Scale} px ({png.Length / 1024} KB)";
    }

    public async Task<string> AddHeavyImagesAsync(int count)
    {
        var rnd = Random.Shared;
        long totalBytes = 0;

        for (var i = 0; i < count; i++)
        {
            var size = new CGSize(3000, 2000);
            var format = UIGraphicsImageRendererFormat.DefaultFormat;
            format.Scale = 1;
            format.Opaque = true;

            // Color noise compresses poorly — the PNG ends up several megabytes
            var renderer = new UIGraphicsImageRenderer(size, format);
            var png = renderer.CreatePng(ctx =>
            {
                for (var y = 0; y < size.Height; y += 20)
                    for (var x = 0; x < size.Width; x += 20)
                    {
                        UIColor.FromRGB(rnd.Next(256), rnd.Next(256), rnd.Next(256)).SetFill();
                        ctx.FillRect(new CGRect(x, y, 20, 20));
                    }
            });
            totalBytes += (long)png.Length;

            await PerformChangesAsync(() =>
            {
                var request = PHAssetCreationRequest.CreationRequestForAsset();
                request.AddResource(PHAssetResourceType.Photo, png, new PHAssetResourceCreationOptions
                {
                    OriginalFilename = $"TestHeavy_{DateTime.Now:HHmmss}_{i}.png",
                });
            });
        }

        return $"Saved {count} photos, {totalBytes / 1024.0 / 1024.0:F1} MB total";
    }

    public async Task<string> AddLargeVideoAsync()
    {
        const int width = 1280, height = 720, fps = 30, seconds = 8;

        var path = Path.Combine(Path.GetTempPath(), $"test_video_{DateTime.Now:HHmmss}.mp4");
        var url = NSUrl.FromFilename(path);

        var writer = AVAssetWriter.FromUrl(url, AVFileTypes.Mpeg4.GetConstant()!, out var error);
        if (error is not null || writer is null)
            throw new InvalidOperationException($"AVAssetWriter: {error?.LocalizedDescription}");

        var input = new AVAssetWriterInput(AVMediaTypes.Video.GetConstant()!, new AVVideoSettingsCompressed
        {
            Codec = AVVideoCodec.H264,
            Width = width,
            Height = height,
            CodecSettings = new AVVideoCodecSettings { AverageBitRate = 25_000_000 },
        })
        {
            ExpectsMediaDataInRealTime = false,
        };

        var adaptor = new AVAssetWriterInputPixelBufferAdaptor(input, new CVPixelBufferAttributes
        {
            PixelFormatType = CVPixelFormatType.CV32BGRA,
            Width = width,
            Height = height,
        });

        writer.AddInput(input);
        writer.StartWriting();
        writer.StartSessionAtSourceTime(CMTime.Zero);

        var rnd = new Random();
        var noise = new byte[width * height * 4];

        for (var frame = 0; frame < fps * seconds; frame++)
        {
            while (!input.ReadyForMoreMediaData)
                await Task.Delay(10);

            using var buffer = adaptor.PixelBufferPool?.CreatePixelBuffer()
                ?? new CVPixelBuffer(width, height, CVPixelFormatType.CV32BGRA);

            buffer.Lock(CVPixelBufferLock.None);
            rnd.NextBytes(noise);
            Marshal.Copy(noise, 0, buffer.BaseAddress, noise.Length);
            buffer.Unlock(CVPixelBufferLock.None);

            adaptor.AppendPixelBufferWithPresentationTime(buffer, new CMTime(frame, fps));
        }

        input.MarkAsFinished();
        await writer.FinishWritingAsync();

        if (writer.Status != AVAssetWriterStatus.Completed)
            throw new InvalidOperationException($"Video writing failed: {writer.Error?.LocalizedDescription}");

        await PerformChangesAsync(() =>
        {
            PHAssetChangeRequest.FromVideo(url);
        });

        var sizeMb = new FileInfo(path).Length / 1024.0 / 1024.0;
        File.Delete(path);
        return $"Saved video {seconds}s, ~{sizeMb:F1} MB";
    }

    public async Task<string> AddDuplicatesAsync()
    {
        var rnd = Random.Shared;
        var size = new CGSize(800, 600);
        var format = UIGraphicsImageRendererFormat.DefaultFormat;
        format.Scale = 1;
        format.Opaque = true;

        // Large solid blocks (not noise) — dHash stays stable after JPEG re-encoding
        var renderer = new UIGraphicsImageRenderer(size, format);
        var png = renderer.CreatePng(ctx =>
        {
            UIColor.FromRGB(rnd.Next(256), rnd.Next(256), rnd.Next(256)).SetFill();
            ctx.FillRect(new CGRect(0, 0, size.Width, size.Height));
            for (var i = 0; i < 5; i++)
            {
                UIColor.FromRGB(rnd.Next(256), rnd.Next(256), rnd.Next(256)).SetFill();
                ctx.FillRect(new CGRect(rnd.Next(600), rnd.Next(400), 150 + rnd.Next(150), 150 + rnd.Next(150)));
            }
        });

        var stamp = DateTime.Now.ToString("HHmmss");
        for (var copy = 0; copy < 2; copy++)
        {
            await PerformChangesAsync(() =>
            {
                var request = PHAssetCreationRequest.CreationRequestForAsset();
                request.AddResource(PHAssetResourceType.Photo, png, new PHAssetResourceCreationOptions
                {
                    OriginalFilename = $"TestDup_{stamp}_copy{copy}.png",
                });
            });
        }

        var jpeg = UIImage.LoadFromData(png)!.AsJPEG(0.7f)!;
        await PerformChangesAsync(() =>
        {
            var request = PHAssetCreationRequest.CreationRequestForAsset();
            request.AddResource(PHAssetResourceType.Photo, jpeg, new PHAssetResourceCreationOptions
            {
                OriginalFilename = $"TestDup_{stamp}_reencoded.jpg",
            });
        });

        return "Saved 3 files: 2 exact PNG copies + 1 re-encoded JPEG";
    }

    // The PHPhotoLibrary binding only has the callback version of PerformChanges
    private static Task PerformChangesAsync(Action changes)
    {
        var tcs = new TaskCompletionSource();
        PHPhotoLibrary.SharedPhotoLibrary.PerformChanges(changes, (success, error) =>
        {
            if (success)
                tcs.SetResult();
            else
                tcs.SetException(new InvalidOperationException(error?.LocalizedDescription ?? "PerformChanges failed"));
        });
        return tcs.Task;
    }
}
