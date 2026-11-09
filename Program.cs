using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<RaceDayContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDay")));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
// Part 3: Azure Blob Storage (falls back to local disk when no connection string is set)
builder.Services.AddSingleton<IBlobStorage>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg["Storage:ConnectionString"];
    return !string.IsNullOrWhiteSpace(conn)
        ? new AzureBlobStorage(conn)
        : new LocalDiskBlobStorage(cfg, sp.GetRequiredService<IWebHostEnvironment>());
});
builder.Services.AddScoped<PasswordService>();
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseSession();
app.Use(async (context, next) =>
{
    var userId = context.Session.GetInt32("UserId");
    var role = context.Session.GetString("Role");
    if (userId.HasValue && !string.IsNullOrWhiteSpace(role))
    {
        context.User = SessionPrincipal.Create(userId.Value, role, context.Session.GetString("Email") ?? "");
    }
    await next();
});
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
