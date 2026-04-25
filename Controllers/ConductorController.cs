using Gbf.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gbf.Controllers
{
    public class ConductorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConductorController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Crear(
            Conductor conductor,
            IFormFile? carnetFrontal,
            IFormFile? carnetTrasero,
            IFormFile? licenciaFrontal,
            IFormFile? licenciaTrasera,
            IFormFile? certificado,
            IFormFile? foto)
        {
            // 🔴 VALIDACIÓN BACKEND
            if (!ModelState.IsValid)
            {
                var empresaError = _context.Empresas.Find(conductor.EmpresaId);

                if (empresaError == null)
                    return RedirectToAction("Index", "Empresa");

                return RedirectToAction("Perfil", "Empresa", new
                {
                    id = empresaError.Id,
                    slug = empresaError.Slug
                });
            }

            // 🔹 NORMALIZACIÓN DE DATOS
            conductor.Rut = conductor.Rut?.ToUpper().Trim();
            conductor.Nombre = conductor.Nombre?.Trim();
            conductor.Apellido = conductor.Apellido?.Trim();
            conductor.ClaseLicencia = conductor.ClaseLicencia?.ToUpper().Trim();

            // 🔴 VALIDACIÓN DE CLASE DE LICENCIA (CONTROLADA)
            var licenciasValidas = new[] { "A1", "A2", "A3", "A4", "A5", "B", "C", "D", "E" };

            if (!licenciasValidas.Contains(conductor.ClaseLicencia))
            {
                var empresaError = _context.Empresas.Find(conductor.EmpresaId);

                if (empresaError == null)
                    return RedirectToAction("Index", "Empresa");

                return RedirectToAction("Perfil", "Empresa", new
                {
                    id = empresaError.Id,
                    slug = empresaError.Slug
                });
            }

            // 🔹 CARPETA DE ARCHIVOS
            var rutaBase = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/conductores");

            if (!Directory.Exists(rutaBase))
                Directory.CreateDirectory(rutaBase);

            // 🔹 MÉTODO LOCAL PARA GUARDAR ARCHIVOS
            async Task<string?> GuardarArchivo(IFormFile? archivo)
            {
                if (archivo == null) return null;

                var nombre = Guid.NewGuid() + Path.GetExtension(archivo.FileName);
                var ruta = Path.Combine(rutaBase, nombre);

                using var stream = new FileStream(ruta, FileMode.Create);
                await archivo.CopyToAsync(stream);

                return "/uploads/conductores/" + nombre;
            }

            // 🔹 GUARDAR ARCHIVOS (TODOS OPCIONALES)
            conductor.CarnetFrontalUrl = await GuardarArchivo(carnetFrontal);
            conductor.CarnetTraseroUrl = await GuardarArchivo(carnetTrasero);
            conductor.LicenciaFrontalUrl = await GuardarArchivo(licenciaFrontal);
            conductor.LicenciaTraseraUrl = await GuardarArchivo(licenciaTrasera);
            conductor.CertificadoAntecedentesUrl = await GuardarArchivo(certificado);
            conductor.FotoPerfilUrl = await GuardarArchivo(foto);

            // 🔹 ESTADO
            conductor.Activo = true;

            // 🔹 GUARDAR EN BD
            _context.Conductores.Add(conductor);
            _context.SaveChanges();

            // 🔹 REDIRECCIÓN SEGURA
            var empresa = _context.Empresas.Find(conductor.EmpresaId);

            if (empresa == null)
                return RedirectToAction("Index", "Empresa");

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

                if (valores.Length != 5)
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

            // 🔴 errores estructurales
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

                // 🔴 EVITAR DUPLICADOS ACTIVOS
                var existeActivo = _context.Conductores
                    .Any(c => c.Rut == rut && c.Activo);

                if (existeActivo)
                {
                    rechazados++;
                    continue;
                }

                var conductor = new Conductor
                {
                    Nombre = valores[0].Trim(),
                    Apellido = valores[1].Trim(),
                    Rut = rut,
                    FechaNacimiento = DateTime.Parse(valores[3]),
                    ClaseLicencia = valores[4].Trim().ToUpper(),
                    EmpresaId = empresaId,
                    Activo = true
                };

                _context.Conductores.Add(conductor);
                agregados++;
            }

            _context.SaveChanges();

            // 🔥 MENSAJES
            TempData["Success"] = $"Carga finalizada: {agregados} conductores agregados correctamente.";

            if (rechazados > 0)
            {
                TempData["Warning"] = $"{rechazados} conductores no fueron agregados. Si desconoce el motivo, contacte al administrador del sistema.";
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
            var conductor = _context.Conductores.Find(id);

            if (conductor == null)
                return RedirectToAction("Index", "Empresa");

            conductor.Activo = false;
            _context.SaveChanges();

            var empresa = _context.Empresas.Find(conductor.EmpresaId);

            return RedirectToAction("Perfil", "Empresa", new
            {
                id = empresa.Id,
                slug = empresa.Slug
            });
        }

        public IActionResult Activar(int id)
        {
            var conductor = _context.Conductores.Find(id);

            if (conductor == null)
                return RedirectToAction("Index", "Empresa");

            // 🔴 VALIDACIÓN (igual lógica que vehículos)
            var existeActivo = _context.Conductores
                .Any(c => c.Rut == conductor.Rut && c.Activo);

            if (existeActivo)
            {
                TempData["Error"] = "Este conductor ya está activo en otra empresa.";

                return RedirectToAction("Perfil", "Empresa", new
                {
                    id = conductor.EmpresaId
                });
            }

            conductor.Activo = true;
            _context.SaveChanges();

            var empresa = _context.Empresas.Find(conductor.EmpresaId);

            return RedirectToAction("Perfil", "Empresa", new
            {
                id = empresa.Id,
                slug = empresa.Slug
            });
        }
    }
}