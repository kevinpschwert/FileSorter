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

        public HomeController(ILogger<HomeController> logger, DBContext db, IConfiguration configuration, IUnzipFiles unzipFiles)
        {
            _logger = logger;
            _db = db;
            _configuration = configuration;
            _unzipFiles = unzipFiles;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UploadFiles([FromBody] ClientFileInfo fileInfo)
        {
            var data = _unzipFiles.ExtractData(fileInfo);
            return PartialView("~/Views/Home/Partials/GroupedClientData.cshtml", data);
        }

        public Object DeleteFolders()
        {
            _unzipFiles.DeleteFolders();
            return Ok("success");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
