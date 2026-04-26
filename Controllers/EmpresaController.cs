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
                .OrderByDescending(e => e.Activo) // activas primero
                .ThenBy(e => e.Nombre)
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
            // 🔹 NORMALIZACIÓN
            Nombre = Nombre.Trim();
            Rut = Rut.ToUpper().Trim();
            Email = Email.Trim();
            Telefono = Telefono.Trim();
            Username = Username.Trim();

            var slug = Nombre.ToLower().Replace(" ", "-");

            // 🔴 VALIDACIÓN DUPLICIDAD
            var existeActivo = _context.Empresas
                .Any(e => e.Rut == Rut && e.Activo);

            if (existeActivo)
            {
                TempData["Error"] = "Esta empresa ya está registrada.";

                return RedirectToAction("Index");
            }

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

            TempData["Success"] = "Empresa registrada correctamente.";

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

            // 🔴 ERRORES ESTRUCTURALES
            if (errores.Any())
            {
                TempData["Error"] = string.Join(" | ", errores);
                return RedirectToAction("Index");
            }

            int agregados = 0;
            int rechazados = 0;

            var nuevasEmpresas = new List<Empresa>();
            var nuevosUsuarios = new List<Usuario>();

            foreach (var v in registros)
            {
                try
                {
                    // 🔹 NORMALIZACIÓN
                    var nombre = v[0].Trim();
                    var rut = v[1].ToUpper().Trim();
                    var email = v[2].Trim();
                    var telefono = v[3].Trim();
                    var username = v[4].Trim();
                    var password = v[5].Trim();

                    // 🔴 VALIDACIÓN DUPLICIDAD (MISMA QUE CREAR)
                    var existeActivo = _context.Empresas
                        .Any(e => e.Rut == rut && e.Activo);

                    if (existeActivo)
                    {
                        rechazados++;
                        continue;
                    }

                    // 🔴 VALIDACIÓN BÁSICA EXTRA (evita datos basura)
                    if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(rut))
                    {
                        rechazados++;
                        continue;
                    }

                    var empresa = new Empresa
                    {
                        Nombre = nombre,
                        Rut = rut,
                        Email = email,
                        Telefono = telefono,
                        LogoUrl = "/img/default-avatar.png",
                        Activo = true,
                        Slug = nombre.ToLower().Replace(" ", "-")
                    };

                    nuevasEmpresas.Add(empresa);

                    var usuario = new Usuario
                    {
                        Nombre = "Admin",
                        Apellido = nombre,
                        Email = email,
                        Username = username,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                        Rol = "CLIENTE",
                        Activo = true,
                        Bloqueado = false,
                        FechaCreacion = DateTime.Now,
                        IntentosFallidos = 0
                    };

                    // ⚠ Se asigna EmpresaId después de guardar
                    nuevosUsuarios.Add(usuario);

                    agregados++;
                }
                catch
                {
                    rechazados++;
                }
            }

            // 🔹 GUARDAR EMPRESAS
            _context.Empresas.AddRange(nuevasEmpresas);
            _context.SaveChanges();

            // 🔹 ASIGNAR EMPRESA A USUARIOS
            for (int i = 0; i < nuevasEmpresas.Count; i++)
            {
                nuevosUsuarios[i].EmpresaId = nuevasEmpresas[i].Id;
            }

            _context.Usuarios.AddRange(nuevosUsuarios);
            _context.SaveChanges();

            // 🔥 MENSAJES CONSISTENTES
            TempData["Success"] = $"Carga finalizada: {agregados} empresas registradas correctamente.";

            if (rechazados > 0)
            {
                TempData["Warning"] = $"{rechazados} empresas no fueron agregadas. Si desconoce el motivo, contacte al administrador del sistema.";
            }

            return RedirectToAction("Index");
        }
        public IActionResult Desactivar(int id)
        {
            var empresa = _context.Empresas.Find(id);

            if (empresa == null)
                return RedirectToAction("Index");

            empresa.Activo = false;
            _context.SaveChanges();

            TempData["Success"] = "Empresa desactivada correctamente.";

            return RedirectToAction("Index");
        }

        public IActionResult Activar(int id)
        {
            var empresa = _context.Empresas.Find(id);

            if (empresa == null)
                return RedirectToAction("Index");

            empresa.Activo = true;
            _context.SaveChanges();

            TempData["Success"] = "Empresa activada correctamente.";

            return RedirectToAction("Index");
        }
    }
}