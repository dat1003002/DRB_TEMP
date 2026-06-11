using DRB_TEMP.Data;
using DRB_TEMP.Reponsitory;
using DRB_TEMP.Service;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<KepwareService>();
builder.Services.AddSingleton<TemperatureCache>();

builder.Services.AddScoped<IHomeReponsitory, HomeReponsitory>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddHostedService<MidnightCleanupService>();
builder.Services.AddHostedService<TemperaturePollingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
