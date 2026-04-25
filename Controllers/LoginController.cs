using Microsoft.AspNetCore.Mvc;
using System.Linq;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

public class LoginController : Controller
{
    private readonly ApplicationDbContext _context;

    public LoginController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string email, string password)
    {
        var usuario = _context.Usuarios
            .FirstOrDefault(u => u.Email == email && u.Activo && !u.Bloqueado);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
        {
            ViewBag.Error = "Credenciales inválidas";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim("Rol", usuario.Rol ?? ""),
            new Claim("EmpresaId", usuario.EmpresaId?.ToString() ?? "")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity)
        );

        return RedirectToAction("Index", "Dashboard");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index");
    }
}