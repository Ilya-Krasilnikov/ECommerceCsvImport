using ECommerceCsvImport.Core.Models;

namespace ECommerceCsvImport.Core.Services
{
    public interface IProductRepository
    {
        void Add(Product product);
        IReadOnlyList<Product> GetAll();
        Product? GetBySku(string sku);
        int Count { get; }
        void Clear();
    }
}
