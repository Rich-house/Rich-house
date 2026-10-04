using ImageMagick;
using Marketify.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

var suite = new ImagePipelineTestSuite();
await suite.RunAsync();

internal sealed class ImagePipelineTestSuite
{
    private int _passed;

    public async Task RunAsync()
    {
        await RunAsync("valid JPG and derivative contract", () => TestFormatAsync(MagickFormat.Jpeg, ".jpg", 1200, 1800,
            [(384, 576), (533, 800), (720, 1080), (1067, 1600)]));
        await RunAsync("valid PNG", () => TestFormatAsync(MagickFormat.Png, ".png", 600, 900,
            [(384, 576), (533, 800), (600, 900), (600, 900)]));
        await RunAsync("valid WebP", () => TestFormatAsync(MagickFormat.WebP, ".webp", 300, 450,
            [(300, 450), (300, 450), (300, 450), (300, 450)]));
        await RunAsync("delete removes original and derivatives", TestDeleteFamilyAsync);
        await RunAsync("processing failure cleans partial files", TestFailureCleanupAsync);
        await RunAsync("invalid image is rejected", TestInvalidImageAsync);
        await RunAsync("content and extension mismatch is rejected", TestMismatchedImageAsync);

        Console.WriteLine($"Image pipeline tests passed: {_passed}/7");
    }

    private async Task TestFormatAsync(
        MagickFormat format,
        string extension,
        uint width,
        uint height,
        (uint Width, uint Height)[] expectedDimensions)
    {
        using var sandbox = new Sandbox();
        var service = sandbox.CreateService();
        var bytes = CreateImage(format, width, height);
        var saved = await SaveAsync(service, bytes, $"sample{extension}", "Pipeline Test");
        var stem = Path.GetFileNameWithoutExtension(saved.AbsolutePath);
        Assert(stem.StartsWith("pipeline-test-", StringComparison.Ordinal), "sanitized prefix was not retained");
        Assert(File.Exists(saved.AbsolutePath), "original is missing");

        var paths = DerivativePaths(sandbox.WebRoot, stem);
        Assert(paths.Count == 4, "expected four derivatives");
        for (var index = 0; index < paths.Count; index++)
        {
            var path = paths[index];
            Assert(File.Exists(path), $"missing derivative {path}");
            Assert(Path.GetFileNameWithoutExtension(path) == stem, "derivative stem differs from original");
            using var image = new MagickImage(path);
            Assert(image.Format == MagickFormat.WebP, "derivative is not WebP");
            Assert((image.Width, image.Height) == expectedDimensions[index],
                $"unexpected dimensions {image.Width}x{image.Height} for {path}");
        }
    }

    private async Task TestDeleteFamilyAsync()
    {
        using var sandbox = new Sandbox();
        var service = sandbox.CreateService();
        var saved = await SaveAsync(service, CreateImage(MagickFormat.Jpeg, 1200, 1800), "delete.jpg", "Delete Test");
        var paths = new[] { saved.AbsolutePath }.Concat(DerivativePaths(sandbox.WebRoot, Path.GetFileNameWithoutExtension(saved.AbsolutePath))).ToArray();
        service.DeleteIfExists(saved.RelativePath);
        Assert(paths.All(path => !File.Exists(path)), "one or more image-family files remain after deletion");
    }

    private async Task TestFailureCleanupAsync()
    {
        using var sandbox = new Sandbox();
        var productDirectory = Path.Combine(sandbox.WebRoot, "images", "products");
        Directory.CreateDirectory(Path.Combine(productDirectory, "responsive"));
        await File.WriteAllTextAsync(Path.Combine(productDirectory, "responsive", "720"), "blocks directory creation");
        var service = sandbox.CreateService();
        await AssertThrowsAsync(() => SaveAsync(service, CreateImage(MagickFormat.Jpeg, 1200, 1800), "failure.jpg", "Failure Test"));
        var leaked = Directory.EnumerateFiles(productDirectory, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path) != "720")
            .ToArray();
        Assert(leaked.Length == 0, $"partial files leaked: {string.Join(", ", leaked)}");
    }

    private async Task TestInvalidImageAsync()
    {
        using var sandbox = new Sandbox();
        var service = sandbox.CreateService();
        await AssertThrowsAsync(() => SaveAsync(service, "not an image"u8.ToArray(), "invalid.jpg", "Invalid"));
        Assert(!Directory.Exists(Path.Combine(sandbox.WebRoot, "images", "products"))
               || !Directory.EnumerateFiles(Path.Combine(sandbox.WebRoot, "images", "products"), "*", SearchOption.AllDirectories).Any(),
            "invalid upload left files behind");
    }

    private async Task TestMismatchedImageAsync()
    {
        using var sandbox = new Sandbox();
        var service = sandbox.CreateService();
        await AssertThrowsAsync(() => SaveAsync(service, CreateImage(MagickFormat.Png, 100, 150), "wrong.jpg", "Mismatch"));
    }

    private async Task RunAsync(string name, Func<Task> test)
    {
        try
        {
            await test();
            _passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL {name}: {error}");
            Environment.ExitCode = 1;
            throw;
        }
    }

    private static async Task<StoredImageFile> SaveAsync(CatalogImageStorageService service, byte[] bytes, string fileName, string prefix)
    {
        using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "Images", fileName);
        return await service.SaveProductImageAsync(file, prefix);
    }

    private static byte[] CreateImage(MagickFormat format, uint width, uint height)
    {
        using var image = new MagickImage(MagickColors.DarkOrange, width, height) { Format = format, Quality = 92 };
        return image.ToByteArray();
    }

    private static List<string> DerivativePaths(string webRoot, string stem) =>
    [
        Path.Combine(webRoot, "images", "products", "responsive", "384", $"{stem}.webp"),
        Path.Combine(webRoot, "images", "products", "thumbnails", $"{stem}.webp"),
        Path.Combine(webRoot, "images", "products", "responsive", "720", $"{stem}.webp"),
        Path.Combine(webRoot, "images", "products", "optimized", $"{stem}.webp")
    ];

    private static async Task AssertThrowsAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch
        {
            return;
        }
        throw new Exception("Expected operation to throw.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class Sandbox : IDisposable
    {
        public Sandbox()
        {
            Root = Path.Combine(Path.GetTempPath(), $"marketify-image-tests-{Guid.NewGuid():N}");
            WebRoot = Path.Combine(Root, "wwwroot");
            Directory.CreateDirectory(WebRoot);
        }

        public string Root { get; }
        public string WebRoot { get; }

        public CatalogImageStorageService CreateService() => new(new TestWebHostEnvironment
        {
            ContentRootPath = Root,
            WebRootPath = WebRoot
        });

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Marketify.ImagePipeline.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
