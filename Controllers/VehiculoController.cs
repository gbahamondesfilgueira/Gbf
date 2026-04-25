using Gbf.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gbf.Controllers
{
    public class VehiculoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VehiculoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 CREAR VEHÍCULO (DESDE MODAL)
        [HttpPost]
        public IActionResult Crear(Vehiculo vehiculo)
        {
            vehiculo.Patente = vehiculo.Patente.ToUpper().Trim().Replace("-", "");

            // 🔴 VALIDACIÓN DE DUPLICIDAD ACTIVA
            var existeActivo = _context.Vehiculos
                .Any(v => v.Patente == vehiculo.Patente && v.Activo);

            if (existeActivo)
            {
                TempData["Error"] = "Esta patente ya está activa en otra empresa.";

                var empresaError = _context.Empresas.Find(vehiculo.EmpresaId);

                return RedirectToAction("Perfil", "Empresa", new
                {
                    id = empresaError.Id,
                    slug = empresaError.Slug
                });
            }

            vehiculo.Activo = true;

            _context.Vehiculos.Add(vehiculo);
            _context.SaveChanges();

            var empresa = _context.Empresas.Find(vehiculo.EmpresaId);

            return RedirectToAction("Perfil", "Empresa", new
            {
                id = empresa.Id,
                slug = empresa.Slug
            });
        }

        // 🔹 IMPORTACIÓN MASIVA CSV
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

            // 🔴 Si hay errores estructurales, detener
            if (errores.Any())
            {
                TempData["Error"] = string.Join(" | ", errores);

                return RedirectToAction("Perfil", "Empresa", new { id = empresaId });
            }

            int agregados = 0;
            int rechazados = 0;

            foreach (var valores in registros)
            {
                var patente = valores[0].ToUpper().Trim().Replace("-", "");
                var marca = valores[1].Trim();
                var modelo = valores[2].Trim();
                var color = valores[3].Trim();
                var tipo = valores[4].Trim();
                var anio = int.Parse(valores[5]);

                // 🔴 VALIDACIÓN DE DUPLICIDAD
                var existeActivo = _context.Vehiculos
                    .Any(v => v.Patente == patente && v.Activo);

                if (existeActivo)
                {
                    rechazados++;
                    continue;
                }

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
                agregados++;
            }

            _context.SaveChanges();

            // 🔥 MENSAJES DE RESULTADO
            TempData["Success"] = $"Carga finalizada: {agregados} vehículos agregados correctamente.";

            if (rechazados > 0)
            {
                TempData["Warning"] = $"{rechazados} vehículos no fueron agregados. Si desconoce el motivo, contacte al administrador del sistema.";
            }

            var empresa = _context.Empresas.Find(empresaId);

            return RedirectToAction("Perfil", "Empresa", new
            {
                id = empresa.Id,
                slug = empresa.Slug
            });
        }

        // 🔹 DESACTIVAR
        public IActionResult Desactivar(int id)
        {
            var vehiculo = _context.Vehiculos.Find(id);

            if (vehiculo == null)
                return RedirectToAction("Index", "Empresa");

            vehiculo.Activo = false;
            _context.SaveChanges();

            var empresa = _context.Empresas.Find(vehiculo.EmpresaId);

            return RedirectToAction("Perfil", "Empresa", new
            {
                id = empresa.Id,
                slug = empresa.Slug
            });
        }

        // 🔹 ACTIVAR (CON VALIDACIÓN CRÍTICA)
        public IActionResult Activar(int id)
        {
            var vehiculo = _context.Vehiculos.Find(id);

            if (vehiculo == null)
                return RedirectToAction("Index", "Empresa");

            // 🔴 VALIDACIÓN DE DUPLICIDAD
            var existeActivo = _context.Vehiculos
                .Any(v => v.Patente == vehiculo.Patente && v.Activo);

            if (existeActivo)
            {
                TempData["Error"] = "No se puede activar. Esta patente ya está activa en otra empresa.";

                return RedirectToAction("Perfil", "Empresa", new
                {
                    id = vehiculo.EmpresaId
                });
            }

            vehiculo.Activo = true;
            _context.SaveChanges();

            var empresa = _context.Empresas.Find(vehiculo.EmpresaId);

            return RedirectToAction("Perfil", "Empresa", new
            {
                id = empresa.Id,
                slug = empresa.Slug
            });
        }
    }
}