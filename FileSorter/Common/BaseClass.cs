using FileSorter.Data;
using FileSorter.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FileSorter.Common
{
    public class BaseClass : Controller
    {
        private readonly DBContext _db;

        public BaseClass(DBContext db)
        {
            _db = db;
        }

        private FolderMapping _folderMapping { get; set; }

        
    }
}
