using FileSorter.Cached.Interfaces;
using FileSorter.Cached.Models;
using FileSorter.Data;
using FileSorter.Helpers;
using FileSorter.Interfaces;
using FileSorter.Logging.Interfaces;
using FileSorter.Logging.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Data.Entity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<DBContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUnzipFiles, UnzipFiles>();
builder.Services.AddScoped<IFileConsolidator, FileConsolidator>();
builder.Services.AddScoped<ILogging, Logging>();
builder.Services.AddScoped<IValidateClients, ValidateClients>();
builder.Services.AddScoped<ISharePointUploader, SharePointUploader>();
builder.Services.AddSingleton<ICachedService, CachedService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var cacheService = scope.ServiceProvider.GetRequiredService<ICachedService>();
    var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();

    var folderMapping = dbContext.FolderMappings.ToList();
    var zohoClientIdMapping = dbContext.ZohoClientIdMappings.ToList();
    var clients = dbContext.Clients.ToList();
    cacheService.FolderMapping = folderMapping;
    cacheService.ZohoClientIdMappings = zohoClientIdMapping;
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
