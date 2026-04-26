using Gbf.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gbf.Controllers
{
    public class PionetaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PionetaController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult Crear(Pioneta pioneta)
        {
            if (!ModelState.IsValid)
            {
                var empresaError = _context.Empresas.Find(pioneta.EmpresaId);

                return RedirectToAction("Perfil", "Empresa", new
                {
                    id = empresaError.Id,
                    slug = empresaError.Slug
                });
            }

            // 🔹 NORMALIZACIÓN
            pioneta.Nombre = pioneta.Nombre.Trim();
            pioneta.Apellido = pioneta.Apellido.Trim();
            pioneta.Rut = pioneta.Rut.ToUpper().Trim();
            pioneta.Telefono = pioneta.Telefono?.Trim();

            // 🔴 VALIDACIÓN DE DUPLICIDAD (MISMA LÓGICA QUE CONDUCTORES)
            var existeActivo = _context.Pionetas
                .Any(p => p.Rut == pioneta.Rut && p.Activo);

            if (existeActivo)
            {
                TempData["Error"] = "Este pioneta ya está activo en otra empresa.";

                var empresaError = _context.Empresas.Find(pioneta.EmpresaId);

                return RedirectToAction("Perfil", "Empresa", new
                {
                    id = empresaError.Id,
                    slug = empresaError.Slug
                });
            }

            // 🔹 ESTADO
            pioneta.Activo = true;

            // 🔹 GUARDAR
            _context.Pionetas.Add(pioneta);
            _context.SaveChanges();

            // 🔹 MENSAJE DE ÉXITO
            TempData["Success"] = "Pioneta registrado correctamente.";

            var empresa = _context.Empresas.Find(pioneta.EmpresaId);

            return RedirectToAction("Perfil", "Empresa", new
            {
                id = empresa.Id,
                slug = empresa.Slug
            });
        }

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

                if (valores.Length != 4)
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

            // 🔴 ERRORES ESTRUCTURALES
            if (errores.Any())
            {
                TempData["Error"] = string.Join(" | ", errores);

                return RedirectToAction("Perfil", "Empresa", new { id = empresaId });
            }

            int agregados = 0;
            int rechazados = 0;

            foreach (var valores in registros)
            {
                var rut = valores[2].Trim().ToUpper();

                // 🔴 VALIDACIÓN DE DUPLICIDAD
                var existeActivo = _context.Pionetas
                    .Any(p => p.Rut == rut && p.Activo);

                if (existeActivo)
                {
                    rechazados++;
                    continue;
                }

                var pioneta = new Pioneta
                {
                    Nombre = valores[0].Trim(),
                    Apellido = valores[1].Trim(),
                    Rut = rut,
                    Telefono = valores[3].Trim(),
                    EmpresaId = empresaId,
                    Activo = true
                };

                _context.Pionetas.Add(pioneta);
                agregados++;
            }

            _context.SaveChanges();

            // 🔥 MENSAJES
            TempData["Success"] = $"Carga finalizada: {agregados} pionetas agregados correctamente.";

            if (rechazados > 0)
            {
                TempData["Warning"] = $"{rechazados} pionetas no fueron agregados. Si desconoce el motivo, contacte al administrador del sistema.";
            }

            var empresa = _context.Empresas.Find(empresaId);

            return RedirectToAction("Perfil", "Empresa", new
            {
                id = empresa.Id,
                slug = empresa.Slug
            });
        }
        public IActionResult Desactivar(int id)
        {
            var pioneta = _context.Pionetas.Find(id);

            if (pioneta == null)
                return RedirectToAction("Index", "Empresa");

            pioneta.Activo = false;
            _context.SaveChanges();

            var empresa = _context.Empresas.Find(pioneta.EmpresaId);

            return RedirectToAction("Perfil", "Empresa", new
            {
                id = empresa.Id,
                slug = empresa.Slug
            });
        }

        public IActionResult Activar(int id)
        {
            var pioneta = _context.Pionetas.Find(id);

            if (pioneta == null)
                return RedirectToAction("Index", "Empresa");

            var existeActivo = _context.Pionetas
                .Any(p => p.Rut == pioneta.Rut && p.Activo);

            if (existeActivo)
            {
                TempData["Error"] = "Este pioneta ya está registrado en otra empresa.";

                var empresaError = _context.Empresas.Find(pioneta.EmpresaId);

                return RedirectToAction("Perfil", "Empresa", new
                {
                    id = empresaError.Id,
                    slug = empresaError.Slug
                });
            }

            pioneta.Activo = true;
            _context.SaveChanges();

            var empresa = _context.Empresas.Find(pioneta.EmpresaId);

            return RedirectToAction("Perfil", "Empresa", new
            {
                id = empresa.Id,
                slug = empresa.Slug
            });
        }
    }
}