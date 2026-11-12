using ECommerceCsvImport.Core.Models;

namespace ECommerceCsvImport.Core.Services
{
    public interface IProductImportService
    {
        ImportResult ImportFromCsv(string filePath);
        ImportResult ImportFromLines(IEnumerable<string> lines);
    }
}
