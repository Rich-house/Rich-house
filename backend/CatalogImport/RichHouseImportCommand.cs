namespace Marketify.CatalogImport;

public sealed record RichHouseImportCommand(
    bool ShouldRun,
    string? ZipPath,
    string AssetsRootPath,
    string? ReportPath)
{
    public static RichHouseImportCommand Parse(string[] args, string contentRootPath)
    {
        if (args.Length == 0 || !string.Equals(args[0], "import-rich-house-assets", StringComparison.OrdinalIgnoreCase))
        {
            return new RichHouseImportCommand(
                false,
                null,
                Path.Combine(contentRootPath, "App_Data", "rich-house-assets"),
                null);
        }

        string? zipPath = null;
        var assetsRootPath = Path.Combine(contentRootPath, "App_Data", "rich-house-assets");
        string? reportPath = null;

        for (var index = 1; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--zip" when index + 1 < args.Length:
                    zipPath = args[++index];
                    break;
                case "--assets-root" when index + 1 < args.Length:
                    assetsRootPath = args[++index];
                    break;
                case "--report" when index + 1 < args.Length:
                    reportPath = args[++index];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(zipPath))
        {
            throw new InvalidOperationException(
                "The Rich House importer requires a zip path. Use 'import-rich-house-assets --zip <path-to-zip>'.");
        }

        return new RichHouseImportCommand(true, zipPath, assetsRootPath, reportPath);
    }
}
