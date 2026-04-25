using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gbf.Models;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        ViewBag.Nombre = User.Identity.Name;

        // 🔹 CONTADORES REALES
        ViewBag.Empresas = _context.Empresas.Count();
        ViewBag.Vehiculos = _context.Vehiculos.Count();

        // Si ya tienes tabla Incidentes
        ViewBag.Incidentes = _context.Incidentes.Count();

        return View();
    }
}