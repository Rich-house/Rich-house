namespace Marketify.CatalogImport;

public interface IRichHouseCatalogImporter
{
    Task<RichHouseCatalogImportSummary> ImportAsync(RichHouseImportCommand command, CancellationToken cancellationToken);
}
