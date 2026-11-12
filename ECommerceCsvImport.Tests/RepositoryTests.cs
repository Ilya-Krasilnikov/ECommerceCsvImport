using ECommerceCsvImport.Core.Models;
using ECommerceCsvImport.Core.Services;

namespace ECommerceCsvImport.Tests
{
    public class RepositoryTests
    {
        [Fact]
        public void Add_AddsProduct_GetAllContainsIt()
        {
            var repository = new InMemoryProductRepository();
            var product = new Product { Sku = "1001", Name = "Ноутбук Lenovo IdeaPad 3", Price = 45990m, Brand = "Lenovo" };

            repository.Add(product);
            var all = repository.GetAll();

            Assert.Single(all);
            Assert.Equal("1001", all[0].Sku);
        }

        [Fact]
        public void GetBySku_ReturnsProduct_IgnoringCaseAndSpaces()
        {
            var repository = new InMemoryProductRepository();
            repository.Add(new Product { Sku = "1001", Name = "Тест" });

            var result = repository.GetBySku(" 1001 ");

            Assert.NotNull(result);
            Assert.Equal("1001", result.Sku);
        }

        [Fact]
        public void EmptyRepository_ReturnsZeroCountAndNull()
        {
            var repository = new InMemoryProductRepository();

            Assert.Equal(0, repository.Count);
            Assert.Empty(repository.GetAll());
            Assert.Null(repository.GetBySku("1001"));
        }
    }
}
