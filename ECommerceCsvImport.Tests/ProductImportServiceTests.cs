using ECommerceCsvImport.Core.Models;
using ECommerceCsvImport.Core.Services;

namespace ECommerceCsvImport.Tests
{
    public class ProductImportServiceTests
    {
        private const string Header = "sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number";

        [Fact]
        public void Import_SingleCorrectRow_AddsProduct()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1001,Ноутбук,Бюджетный,45990,RUB,Компьютеры,5,img.jpg,true,Lenovo,24,IdeaPad" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(1, result.TotalRows);
            Assert.Equal(1, result.ImportedCount);
            Assert.Empty(result.Errors);
            Assert.NotNull(repo.GetBySku("1001"));
        }

        [Fact]
        public void Import_MultipleCorrectRows_AddsAllProducts()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] {
                Header,
                "1001,Ноутбук,Бюджетный,45990,RUB,Компьютеры,5,img.jpg,true,Lenovo,24,IdeaPad",
                "1002,Мышь,Беспроводная,1290,RUB,Периферия,10,img.jpg,true,Logitech,12,M185"
            };

            var result = service.ImportFromLines(lines);

            Assert.Equal(2, result.TotalRows);
            Assert.Equal(2, result.ImportedCount);
            Assert.Empty(result.Errors);
            Assert.Equal(2, repo.Count);
        }

        [Fact]
        public void ImportFromCsv_FileNotFound_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");

            var result = service.ImportFromCsv(path);

            Assert.Equal(0, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Файл не найден", result.Errors[0].Message);
        }

        [Fact]
        public void ImportFromCsv_EmptyFile_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csv");
            File.WriteAllText(path, string.Empty);

            var result = service.ImportFromCsv(path);

            Assert.Equal(0, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
        }

        [Fact]
        public void Import_OnlyHeader_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.TotalRows);
            Assert.Single(result.Errors);
            Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
        }

        [Fact]
        public void Import_InvalidHeader_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { "sku,name,price", "1001,Ноутбук,45990" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.TotalRows);
            Assert.Single(result.Errors);
            Assert.Contains("Неверные заголовки CSV", result.Errors[0].Message);
        }

        [Fact]
        public void Import_WrongFieldCount_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1001,Ноутбук Lenovo" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(1, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Неверное количество полей в строке", result.Errors[0].Message);
        }

        [Fact]
        public void Import_EmptySku_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, ",Ноутбук,Бюджетный,45990,RUB,Компьютеры,5,img.jpg,true,Lenovo,24,IdeaPad" };

            var result = service.ImportFromLines(lines);

            Assert.Contains(result.Errors, e => e.Message.Contains("Поле sku не может быть пустым"));
        }

        [Fact]
        public void Import_EmptyName_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1001,,Бюджетный,45990,RUB,Компьютеры,5,img.jpg,true,Lenovo,24,IdeaPad" };

            var result = service.ImportFromLines(lines);

            Assert.Contains(result.Errors, e => e.Message.Contains("Поле name не может быть пустым"));
        }

        [Fact]
        public void Import_NegativePrice_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1001,Ноутбук,Бюджетный,-500,RUB,Компьютеры,5,img.jpg,true,Lenovo,24,IdeaPad" };

            var result = service.ImportFromLines(lines);

            Assert.Contains(result.Errors, e => e.Message.Contains("Поле price должно быть числом больше 0"));
        }

        [Fact]
        public void Import_NonNumericPrice_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1001,Ноутбук,Бюджетный,abc,RUB,Компьютеры,5,img.jpg,true,Lenovo,24,IdeaPad" };

            var result = service.ImportFromLines(lines);

            Assert.Contains(result.Errors, e => e.Message.Contains("Поле price должно быть числом больше 0"));
        }

        [Fact]
        public void Import_NegativeStock_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1001,Ноутбук,Бюджетный,45990,RUB,Компьютеры,-5,img.jpg,true,Lenovo,24,IdeaPad" };

            var result = service.ImportFromLines(lines);

            Assert.Contains(result.Errors, e => e.Message.Contains("Остаток на складе должен быть целым числом не меньше 0"));
        }

        [Fact]
        public void Import_InvalidIsActive_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1001,Ноутбук,Бюджетный,45990,RUB,Компьютеры,5,img.jpg,yes,Lenovo,24,IdeaPad" };

            var result = service.ImportFromLines(lines);

            Assert.Contains(result.Errors, e => e.Message.Contains("Поле is_active должно иметь значение true, false, 1 или 0"));
        }

        [Fact]
        public void Import_MissingBrand_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1001,Ноутбук,Бюджетный,45990,RUB,Компьютеры,5,img.jpg,true,,24,IdeaPad" };

            var result = service.ImportFromLines(lines);

            Assert.Contains(result.Errors, e => e.Message.Contains("Поле brand обязательно"));
        }

        [Fact]
        public void Import_WarrantyLessThanZero_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1001,Ноутбук,Бюджетный,45990,RUB,Компьютеры,5,img.jpg,true,Lenovo,-1,IdeaPad" };

            var result = service.ImportFromLines(lines);

            Assert.Contains(result.Errors, e => e.Message.Contains("Гарантия должна быть целым числом от 0 до 60 месяцев"));
        }

        [Fact]
        public void Import_WarrantyMoreThan60_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1001,Ноутбук,Бюджетный,45990,RUB,Компьютеры,5,img.jpg,true,Lenovo,61,IdeaPad" };

            var result = service.ImportFromLines(lines);

            Assert.Contains(result.Errors, e => e.Message.Contains("Гарантия должна быть целым числом от 0 до 60 месяцев"));
        }

        [Fact]
        public void Import_DuplicateSkuInFile_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] {
                Header,
                "1001,Ноутбук,Бюджетный,45990,RUB,Компьютеры,5,img.jpg,true,Lenovo,24,IdeaPad",
                "1001,Другой ноутбук,Другой,52990,RUB,Компьютеры,7,img.jpg,true,ASUS,24,VivoBook"
            };

            var result = service.ImportFromLines(lines);

            Assert.Equal(2, result.TotalRows);
            Assert.Equal(1, result.ImportedCount);
            Assert.Contains(result.Errors, e => e.Message.Contains("Артикул товара уже существует"));
            Assert.Equal("Ноутбук", repo.GetBySku("1001")?.Name);
        }

        [Fact]
        public void Import_DuplicateSkuInRepo_ReturnsError()
        {
            var repo = new InMemoryProductRepository();
            repo.Add(new Product { Sku = "1001", Name = "Существующий" });
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1001,Ноутбук,Бюджетный,45990,RUB,Компьютеры,5,img.jpg,true,Lenovo,24,IdeaPad" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(1, result.TotalRows);
            Assert.Equal(0, result.ImportedCount);
            Assert.Contains(result.Errors, e => e.Message.Contains("Артикул товара уже существует"));
            Assert.Equal(1, repo.Count);
        }

        [Fact]
        public void Import_MultipleErrorsInRow_ReturnsAllErrors()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, " , ,Некорректная,-10,RUB,Компьютеры,-5,img.jpg,yes,,61," };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.True(result.Errors.Count >= 4);
        }

        [Fact]
        public void Import_EmptyOptionalFields_Succeeds()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1011,Блок питания,,4990,RUB,Компьютеры,9,,true,Corsair,60,CV550" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(1, result.ImportedCount);
            Assert.Empty(result.Errors);
            Assert.Equal("", repo.GetBySku("1011")?.Description);
        }

        [Fact]
        public void Import_EmptyCurrency_DefaultsToRUB()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1011,Блок питания,,4990,,Компьютеры,9,,true,Corsair,60,CV550" };

            service.ImportFromLines(lines);

            Assert.Equal("RUB", repo.GetBySku("1011")?.Currency);
        }

        [Fact]
        public void Import_EmptyCategory_DefaultsToBezKategorii()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1011,Блок питания,,4990,RUB,,9,,true,Corsair,60,CV550" };

            service.ImportFromLines(lines);

            Assert.Equal("Без категории", repo.GetBySku("1011")?.CategoryName);
        }

        [Fact]
        public void Import_Cyrillic_SavesCorrectly()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1001,Ноутбук Lenovo,Бюджетный ноутбук для работы,45990,RUB,Компьютеры,5,img.jpg,true,Lenovo,24,IdeaPad" };

            service.ImportFromLines(lines);

            var product = repo.GetBySku("1001");
            Assert.Equal("Ноутбук Lenovo", product?.Name);
            Assert.Equal("Бюджетный ноутбук для работы", product?.Description);
        }
    }
}
