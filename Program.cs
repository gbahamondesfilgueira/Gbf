using Gbf.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Obtener cadenas
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var localConnection = builder.Configuration.GetConnectionString("LocalConnection");

// 🔹 Método para probar conexión
bool ProbarConexion(string connectionString)
{
    try
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        using var context = new ApplicationDbContext(options);
        return context.Database.CanConnect();
    }
    catch
    {
        return false;
    }
}

// 🔹 Selección de conexión
string connectionFinal = null;

if (ProbarConexion(defaultConnection))
{
    connectionFinal = defaultConnection;
    Console.WriteLine("✔ Conectado a DefaultConnection");
}
else if (ProbarConexion(localConnection))
{
    connectionFinal = localConnection;
    Console.WriteLine("✔ Conectado a LocalConnection");
}
else
{
    Console.WriteLine("❌ ERROR: No se pudo conectar a ninguna base de datos.");
    throw new Exception("No hay conexión a base de datos configurada correctamente.");
}

// 🔹 DB CONTEXT FINAL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionFinal));

// 🔹 MVC
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
    });

var app = builder.Build();


// 🔹 CREACIÓN DB
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (app.Environment.IsDevelopment())
    {
        Console.WriteLine("⚠️ MODO DESARROLLO: Eliminando y recreando base de datos...");

        //context.Database.EnsureDeleted();   // 🔴 elimina todo
        context.Database.EnsureCreated();   // 🟢 crea desde cero

        Console.WriteLine("✔ Base de datos recreada correctamente.");
    }
    else
    {
        // 🔹 Producción o entorno normal
        context.Database.EnsureCreated();
    }
}


// 🔹 SEED ADMIN
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!context.Usuarios.Any())
    {
        var admin = new Usuario
        {
            Nombre = "Admin",
            Apellido = "Sistema",
            Email = "admin@gfc.cl",
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Rol = "ADMIN",
            Activo = true,
            Bloqueado = false,
            FechaCreacion = DateTime.Now,
            IntentosFallidos = 0
        };

        context.Usuarios.Add(admin);
        context.SaveChanges();
    }
}


// 🔹 PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


// 🔹 RUTAS
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();