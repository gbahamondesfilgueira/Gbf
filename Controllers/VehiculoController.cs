using Gbf.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gbf.Controllers
{
    public class VehiculoController : Controller
    {
        // 🔹 CONTEXTO
        private readonly ApplicationDbContext _context;

        // 🔹 CONSTRUCTOR (inyección de dependencia)
        public VehiculoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 CREAR VEHÍCULO
        [HttpPost]
        public async Task<IActionResult> ImportarCsv(IFormFile archivo, int empresaId)
        {
            if (archivo == null || archivo.Length == 0)
                return RedirectToAction("Index", "Empresa");

            var errores = new List<string>();
            var registros = new List<string[]>();

            using var reader = new StreamReader(archivo.OpenReadStream());

            bool primeraLinea = true;
            int fila = 1;

            while (!reader.EndOfStream)
            {
                var linea = await reader.ReadLineAsync();
                fila++;

                if (primeraLinea)
                {
                    primeraLinea = false;
                    continue;
                }

                var valores = linea.Split(',');

                if (valores.Length != 6)
                {
                    errores.Add($"Fila {fila}: formato incorrecto");
                    continue;
                }

                if (valores.Any(v => string.IsNullOrWhiteSpace(v)))
                {
                    errores.Add($"Fila {fila}: campos incompletos");
                    continue;
                }

                registros.Add(valores);
            }

            if (errores.Any())
            {
                TempData["Error"] = string.Join(" | ", errores);
                return RedirectToAction("Perfil", "Empresa", new { id = empresaId });
            }

            foreach (var valores in registros)
            {
                var patente = valores[0].Trim().ToUpper().Replace("-", "");
                var marca = valores[1].Trim();
                var modelo = valores[2].Trim();
                var color = valores[3].Trim();
                var tipo = valores[4].Trim();
                var anio = int.Parse(valores[5]);

                // 🔴 VALIDAR DUPLICADOS
                if (_context.Vehiculos.Any(v => v.Patente == patente && v.Activo))
                    continue;

                var vehiculo = new Vehiculo
                {
                    Patente = patente,
                    Marca = marca,
                    Modelo = modelo,
                    Color = color,
                    Tipo = tipo,
                    Año = anio,
                    EmpresaId = empresaId,
                    Activo = true
                };

                _context.Vehiculos.Add(vehiculo);
            }

            _context.SaveChanges();

            var empresa = _context.Empresas.Find(empresaId);

            return RedirectToAction("Perfil", "Empresa", new
            {
                id = empresa.Id,
                slug = empresa.Slug
            });
        }

        public IActionResult Desactivar(int id)
        {
            var vehiculo = _context.Vehiculos.Find(id);

            if (vehiculo != null)
            {
                vehiculo.Activo = false;
                _context.SaveChanges();

                var empresa = _context.Empresas.Find(vehiculo.EmpresaId);

                return RedirectToAction("Perfil", "Empresa", new
                {
                    id = empresa.Id,
                    slug = empresa.Slug
                });
            }

            return RedirectToAction("Index", "Empresa");
        }

        // 🔹 ACTIVAR VEHÍCULO
        public IActionResult Activar(int id)
        {
            var vehiculo = _context.Vehiculos.Find(id);

            if (vehiculo != null)
            {
                vehiculo.Activo = true;
                _context.SaveChanges();

                return RedirectToAction("Perfil", "Empresa", new { id = vehiculo.EmpresaId });
            }

            return RedirectToAction("Index", "Empresa");
        }
    }
}