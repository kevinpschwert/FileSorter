using FileSorter.Data;
using FileSorter.Helpers;
using FileSorter.Interfaces;
using FileSorter.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FileSorter.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DBContext _db;
        private readonly IConfiguration _configuration;
        private readonly IUnzipFiles _unzipFiles;
        private readonly IValidateClients _validateClients;

        public HomeController(ILogger<HomeController> logger, DBContext db, IConfiguration configuration, IUnzipFiles unzipFiles, IValidateClients validateClients)
        {
            _logger = logger;
            _db = db;
            _configuration = configuration;
            _unzipFiles = unzipFiles;
            _validateClients = validateClients;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFiles([FromBody] List<string> files)
        {
            //var data = await _unzipFiles.ExtractData(files);
            UploadZohoClientMapping uploadZohoClientMapping = new UploadZohoClientMapping(_db);
            uploadZohoClientMapping.UploadCsv();
            var data = new List<GroupedData>();
            return PartialView("~/Views/Home/Partials/GroupedClientData.cshtml", data);
        }

        public IActionResult ClientValidator()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ValidateClients([FromBody] List<string> files)
        {
            var data = _validateClients.FindMissingClients(files);
            return PartialView("~/Views/Home/Partials/MissingClients.cshtml", data);
        }

        [HttpPost]
        public async Task<JsonResult> RetryUploadFiles([FromBody] List<string> files)
        {
            await _unzipFiles.RetryUploadFiles(files);
            return new JsonResult(true);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
