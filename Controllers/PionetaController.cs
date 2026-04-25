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

            pioneta.Nombre = pioneta.Nombre.Trim();
            pioneta.Apellido = pioneta.Apellido.Trim();
            pioneta.Rut = pioneta.Rut.ToUpper().Trim();
            pioneta.Activo = true;

            _context.Pionetas.Add(pioneta);
            _context.SaveChanges();

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

            using var reader = new StreamReader(archivo.OpenReadStream());

            bool primeraLinea = true;

            while (!reader.EndOfStream)
            {
                var linea = await reader.ReadLineAsync();

                if (primeraLinea)
                {
                    primeraLinea = false;
                    continue;
                }

                var valores = linea.Split(',');

                if (valores.Length < 4)
                    continue;

                var pioneta = new Pioneta
                {
                    Nombre = valores[0].Trim(),
                    Apellido = valores[1].Trim(),
                    Rut = valores[2].Trim().ToUpper(),
                    Telefono = valores[3].Trim(),
                    EmpresaId = empresaId,
                    Activo = true
                };

                _context.Pionetas.Add(pioneta);
            }

            _context.SaveChanges();

            var empresa = _context.Empresas.Find(empresaId);

            return RedirectToAction("Perfil", "Empresa", new
            {
                id = empresa.Id,
                slug = empresa.Slug
            });
        }
    }
}