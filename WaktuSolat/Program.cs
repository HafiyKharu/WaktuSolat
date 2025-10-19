using WaktuSolat.Services;
using WaktuSolat.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ScrapWaktuSolatService>();
builder.Services.AddScoped<WaktuSolatService>();
builder.Services.AddScoped<ZoneService>();
builder.Services.AddScoped<WaktuSolatRepository>();
builder.Services.AddScoped<ZoneRepository>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();