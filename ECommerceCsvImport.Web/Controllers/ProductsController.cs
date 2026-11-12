using Microsoft.AspNetCore.Mvc;
using ECommerceCsvImport.Core.Models;
using ECommerceCsvImport.Core.Services;

namespace ECommerceCsvImport.Web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductRepository _repository;
        private readonly IProductImportService _importService;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(
            IProductRepository repository,
            IProductImportService importService,
            IWebHostEnvironment environment)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        [HttpGet]
        public ActionResult Index()
        {
            var viewModel = new ProductsIndexViewModel
            {
                Products = _repository.GetAll()
            };
            return View(viewModel);
        }

        [HttpGet]
        public ActionResult Import()
        {
            return View(new ImportViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(IFormFile? file, string? filePath)
        {
            var viewModel = new ImportViewModel { FilePath = filePath ?? string.Empty };
            string pathToRead = string.Empty;
            bool isTempFile = false;

            if (file != null && file.Length > 0)
            {
                pathToRead = Path.GetTempFileName();
                using (var stream = new FileStream(pathToRead, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                isTempFile = true;
            }
            else if (!string.IsNullOrWhiteSpace(filePath))
            {
                var resolvedPath = ResolveFilePath(filePath);
                if (string.IsNullOrEmpty(resolvedPath))
                {
                    viewModel.Result = new ImportResult();
                    viewModel.Result.Errors.Add(new ImportError { RowNumber = 0, Message = "Неверный путь к файлу" });
                    return View(viewModel);
                }
                pathToRead = resolvedPath;
            }
            else
            {
                viewModel.Result = new ImportResult();
                viewModel.Result.Errors.Add(new ImportError { RowNumber = 0, Message = "Выберите файл или укажите путь" });
                return View(viewModel);
            }

            viewModel.Result = _importService.ImportFromCsv(pathToRead);

            if (isTempFile && System.IO.File.Exists(pathToRead))
            {
                System.IO.File.Delete(pathToRead);
            }

            return View(viewModel);
        }

        private string? ResolveFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;

            if (filePath.Length > 1 && filePath[1] == ':')
            {
                return filePath;
            }

            string cleanPath = filePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(_environment.WebRootPath, cleanPath);
        }
    }
}
