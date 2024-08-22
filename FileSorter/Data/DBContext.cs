using FileSorter.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileSorter.Data
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options) { }

        public DbSet<Files> Files { get; set; }

        public DbSet<ClientFiles> ClientFiles { get; set; }

        public DbSet<Clients> Clients { get; set; }
        public DbSet<FolderMapping> FolderMappings { get; set; }
    }
}
