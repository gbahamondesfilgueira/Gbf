using Gbf.Models;
using Microsoft.AspNetCore.Mvc;
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

        // 🔹 LISTADO
        public IActionResult Index()
        {
            var empresas = _context.Empresas
                .Where(e => e.Activo)
                .ToList();

            return View(empresas);
        }

        // 🔹 PERFIL
        [Route("Empresa/{id:int}/{slug}")]
        public IActionResult Perfil(int id, string slug)
        {
            var empresa = _context.Empresas
                .FirstOrDefault(e => e.Id == id && e.Activo);

            if (empresa == null)
                return NotFound();

            // 🔹 VALIDAR SLUG
            if (empresa.Slug != slug)
            {
                return RedirectToAction("Perfil", new
                {
                    id = empresa.Id,
                    slug = empresa.Slug
                });
            }

            // 🔹 VEHÍCULOS
            ViewBag.Vehiculos = _context.Vehiculos
                .Where(v => v.EmpresaId == id)
                .ToList();

            // 🔹 CONDUCTORES
            ViewBag.Conductores = _context.Conductores
                .Where(c => c.EmpresaId == id)
                .ToList();

            // 🔹 PIONETAS
            ViewBag.Pionetas = _context.Pionetas
                .Where(p => p.EmpresaId == id)
                .ToList();

            // 🔹 DOCUMENTOS
            ViewBag.Documentos = _context.Documentos
                .Where(d => d.EmpresaId == id)
                .ToList();

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

            var nombre = Guid.NewGuid() + Path.GetExtension(archivo.FileName);
            var ruta = Path.Combine(carpeta, nombre);

            using var stream = new FileStream(ruta, FileMode.Create);
            await archivo.CopyToAsync(stream);

            var doc = new Documento
            {
                Nombre = archivo.FileName,
                Ruta = "/uploads/empresas/" + nombre,
                EmpresaId = empresaId
            };

            _context.Documentos.Add(doc);
            _context.SaveChanges();

            var empresa = _context.Empresas.Find(empresaId);

            return RedirectToAction("Perfil", new
            {
                id = empresa.Id,
                slug = empresa.Slug
            });
        }

        // 🔹 CREAR EMPRESA + USUARIO
        [HttpPost]
        public IActionResult Crear(string Nombre, string Rut, string Email, string Username, string Password, string Telefono)
        {
            var slug = Nombre.ToLower().Replace(" ", "-");

            var empresa = new Empresa
            {
                Nombre = Nombre.Trim(),
                Rut = Rut.Trim(),
                Email = Email.Trim(),
                Telefono = Telefono.Trim(),
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

            bool primera = true;
            int fila = 1;

            while (!reader.EndOfStream)
            {
                var linea = await reader.ReadLineAsync();
                fila++;

                if (primera)
                {
                    primera = false;
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

            foreach (var v in registros)
            {
                var empresa = new Empresa
                {
                    Nombre = v[0].Trim(),
                    Rut = v[1].Trim(),
                    Email = v[2].Trim(),
                    Telefono = v[3].Trim(),
                    LogoUrl = "/img/default-avatar.png",
                    Activo = true,
                    Slug = v[0].ToLower().Replace(" ", "-")
                };

                _context.Empresas.Add(empresa);
                _context.SaveChanges();

                var usuario = new Usuario
                {
                    Nombre = "Admin",
                    Apellido = v[0],
                    Email = v[2],
                    Username = v[4],
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(v[5]),
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