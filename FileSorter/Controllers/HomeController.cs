using FileSorter.Data;
using FileSorter.Helpers;
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

        public HomeController(ILogger<HomeController> logger, DBContext db, IConfiguration configuration)
        {
            _logger = logger;
            _db = db;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UploadFiles([FromBody] ClientFileInfo fileInfo)
        {
            UnzipFiles unzipFiles = new UnzipFiles(_db, _configuration);
            var data = unzipFiles.ExtractActualData(fileInfo);
            return PartialView("~/Views/Home/Partials/GroupedClientData.cshtml", data);
        }

        public Object DeleteFolders()
        {
            UnzipFiles unzipFiles = new UnzipFiles(_db, _configuration);
            unzipFiles.DeleteActualFolders();
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
