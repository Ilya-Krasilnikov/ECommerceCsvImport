using ECommerceCsvImport.Core.Models;

namespace ECommerceCsvImport.Core.Services
{
    public class ProductImportService : IProductImportService
    {
        private readonly IProductRepository _repository;

        public ProductImportService(IProductRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public ImportResult ImportFromCsv(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                var res = new ImportResult();
                res.Errors.Add(new ImportError { RowNumber = 0, Message = "Файл не найден" });
                return res;
            }

            if (!File.Exists(filePath))
            {
                var res = new ImportResult();
                res.Errors.Add(new ImportError { RowNumber = 0, Message = "Файл не найден" });
                return res;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                return ImportFromLines(lines);
            }
            catch (Exception)
            {
                var res = new ImportResult();
                res.Errors.Add(new ImportError { RowNumber = 0, Message = "Ошибка при чтении файла" });
                return res;
            }
        }

        public ImportResult ImportFromLines(IEnumerable<string> lines)
        {
            var result = new ImportResult();
            var lineList = lines.ToList();

            if (lineList.Count == 0 || lineList.All(string.IsNullOrWhiteSpace))
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = "Данные для импорта отсутствуют" });
                return result;
            }

            int headerIndex = lineList.FindIndex(l => !string.IsNullOrWhiteSpace(l));
            var headerFields = CsvLineParser.Parse(lineList[headerIndex]);
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headerFields.Count; i++)
            {
                string header = headerFields[i].Trim();
                if (!columnMap.ContainsKey(header))
                {
                    columnMap[header] = i;
                }
            }

            string[] requiredHeaders = { "sku", "name", "price", "stock", "is_active", "brand", "warranty_months" };
            bool headersValid = true;
            foreach (var req in requiredHeaders)
            {
                if (!columnMap.ContainsKey(req))
                {
                    headersValid = false;
                    break;
                }
            }

            if (!headersValid)
            {
                result.Errors.Add(new ImportError { RowNumber = headerIndex + 1, Message = "Неверные заголовки CSV" });
                return result;
            }

            var importedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = headerIndex + 1; i < lineList.Count; i++)
            {
                string line = lineList[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                result.TotalRows++;
                int rowNumber = i + 1;
                var fields = CsvLineParser.Parse(line);

                if (fields.Count != headerFields.Count)
                {
                    result.Errors.Add(new ImportError { RowNumber = rowNumber, Message = "Неверное количество полей в строке" });
                    continue;
                }

                string GetValue(string col)
                {
                    if (columnMap.TryGetValue(col, out int idx) && idx < fields.Count)
                        return fields[idx];
                    return string.Empty;
                }

                var rowErrors = new List<ImportError>();

                string sku = GetValue("sku");
                if (string.IsNullOrWhiteSpace(sku))
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле sku не может быть пустым" });

                string name = GetValue("name");
                if (string.IsNullOrWhiteSpace(name))
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле name не может быть пустым" });

                decimal price = 0;
                if (!decimal.TryParse(GetValue("price"), out price) || price <= 0)
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле price должно быть числом больше 0" });

                int stock = 0;
                if (!int.TryParse(GetValue("stock"), out stock) || stock < 0)
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Остаток на складе должен быть целым числом не меньше 0" });

                bool isActive = false;
                string activeStr = GetValue("is_active").Trim().ToLower();
                if (activeStr == "true" || activeStr == "1") isActive = true;
                else if (activeStr == "false" || activeStr == "0") isActive = false;
                else rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле is_active должно иметь значение true, false, 1 или 0" });

                string brand = GetValue("brand");
                if (string.IsNullOrWhiteSpace(brand))
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле brand обязательно для варианта «Электроника»" });

                int warranty = 0;
                if (!int.TryParse(GetValue("warranty_months"), out warranty) || warranty < 0 || warranty > 60)
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Гарантия должна быть целым числом от 0 до 60 месяцев" });

                if (rowErrors.Count > 0)
                {
                    result.Errors.AddRange(rowErrors);
                    continue;
                }

                if (_repository.GetBySku(sku) != null || importedSkus.Contains(sku))
                {
                    result.Errors.Add(new ImportError { RowNumber = rowNumber, Message = "Артикул товара уже существует" });
                    continue;
                }

                string currency = GetValue("currency");
                if (string.IsNullOrWhiteSpace(currency)) currency = "RUB";
                else currency = currency.Trim().ToUpper();

                string category = GetValue("category_name");
                if (string.IsNullOrWhiteSpace(category)) category = "Без категории";
                else category = category.Trim();

                var product = new Product
                {
                    Sku = sku.Trim(),
                    Name = name.Trim(),
                    Description = GetValue("description").Trim(),
                    Price = price,
                    Currency = currency,
                    CategoryName = category,
                    StockQuantity = stock,
                    ImageUrl = GetValue("image_url").Trim(),
                    IsActive = isActive,
                    Brand = brand.Trim(),
                    WarrantyMonths = warranty,
                    ModelNumber = GetValue("model_number").Trim()
                };

                _repository.Add(product);
                importedSkus.Add(sku);
                result.ImportedCount++;
            }

            if (result.TotalRows == 0 && result.Errors.Count == 0)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = "Данные для импорта отсутствуют" });
            }

            return result;
        }
    }
}
