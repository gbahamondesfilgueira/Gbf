using Gbf.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Gbf.Controllers
{
    public class EmpresaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmpresaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 LISTADO DE EMPRESAS (solo activas)
        public IActionResult Index()
        {
            var empresas = _context.Empresas
                .Where(e => e.Activo)
                .ToList();

            return View(empresas);
        }

        // 🔹 PERFIL EMPRESA
        [Route("Empresa/{id:int}/{slug}")]
        public IActionResult Perfil(int id, string slug)
        {
            var empresa = _context.Empresas
                .FirstOrDefault(e => e.Id == id && e.Activo);

            if (empresa == null)
                return NotFound();

            // 🔹 Validación de slug
            if (empresa.Slug != slug)
            {
                return RedirectToAction("Perfil", new { id = empresa.Id, slug = empresa.Slug });
            }

            // 🔹 VEHÍCULOS ACTIVOS
            var vehiculos = _context.Vehiculos
                .Where(v => v.EmpresaId == id && v.Activo)
                .ToList();

            ViewBag.Vehiculos = vehiculos;

            // 🔹 CONDUCTORES ACTIVOS
            var conductores = _context.Conductores
                .Where(c => c.EmpresaId == id && c.Activo)
                .ToList();

            ViewBag.Conductores = conductores;

            // 🔹 PIONETAS ACTIVOS
            var pionetas = _context.Pionetas
                .Where(p => p.EmpresaId == id && p.Activo)
                .ToList();

            ViewBag.Pionetas = pionetas;

            // 🔹 DOCUMENTOS
            var documentos = _context.Documentos
                .Where(d => d.EmpresaId == id)
                .ToList();

            ViewBag.Documentos = documentos;

            return View(empresa);
        }

        // 🔹 SUBIR DOCUMENTO
        [HttpPost]
        public async Task<IActionResult> SubirArchivo(int empresaId, IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return RedirectToAction("Perfil", new { id = empresaId });

            var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/empresas");

            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
            var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            var doc = new Documento
            {
                Nombre = archivo.FileName,
                Ruta = "/uploads/empresas/" + nombreArchivo,
                EmpresaId = empresaId
            };

            _context.Documentos.Add(doc);
            _context.SaveChanges();

            return RedirectToAction("Perfil", new { id = empresaId });
        }

        // 🔹 CREAR EMPRESA + USUARIO
        [HttpPost]
        public IActionResult Crear(string Nombre, string Rut, string Email, string Username, string Password, string Telefono)
        {
            var slug = Nombre.ToLower().Replace(" ", "-");

            var empresa = new Empresa
            {
                Nombre = Nombre,
                Rut = Rut,
                Email = Email,
                Telefono = Telefono,
                LogoUrl = "/img/default-avatar.png",
                Activo = true,
                Slug = slug
            };

            _context.Empresas.Add(empresa);
            _context.SaveChanges();

            var usuario = new Usuario
            {
                Nombre = "Admin",
                Apellido = Nombre,
                Email = Email,
                Username = Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                Rol = "CLIENTE",
                EmpresaId = empresa.Id,
                Activo = true,
                Bloqueado = false,
                FechaCreacion = DateTime.Now,
                IntentosFallidos = 0
            };

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // 🔹 IMPORTACIÓN CSV
        [HttpPost]
        public async Task<IActionResult> ImportarCsv(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return RedirectToAction("Index");

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
                return RedirectToAction("Index");
            }

            foreach (var valores in registros)
            {
                string nombre = valores[0].Trim();
                string rut = valores[1].Trim();
                string email = valores[2].Trim();
                string telefono = valores[3].Trim();
                string username = valores[4].Trim();
                string password = valores[5].Trim();

                string slug = nombre.ToLower().Replace(" ", "-");

                var empresa = new Empresa
                {
                    Nombre = nombre,
                    Rut = rut,
                    Email = email,
                    Telefono = telefono,
                    LogoUrl = "/img/default-avatar.png",
                    Activo = true,
                    Slug = slug
                };

                _context.Empresas.Add(empresa);
                _context.SaveChanges();

                var usuario = new Usuario
                {
                    Nombre = "Admin",
                    Apellido = nombre,
                    Email = email,
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Rol = "CLIENTE",
                    EmpresaId = empresa.Id,
                    Activo = true,
                    Bloqueado = false,
                    FechaCreacion = DateTime.Now,
                    IntentosFallidos = 0
                };

                _context.Usuarios.Add(usuario);
                _context.SaveChanges();
            }

            TempData["Success"] = "Importación exitosa";
            return RedirectToAction("Index");
        }
    }
}