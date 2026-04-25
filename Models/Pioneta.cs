using System.ComponentModel.DataAnnotations;

namespace Gbf.Models
{
    public class Pioneta
    {
        public int Id { get; set; }

        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public string Apellido { get; set; } = string.Empty;

        [Required]
        public string Rut { get; set; } = string.Empty;

        public string? Telefono { get; set; }

        public bool Activo { get; set; } = true;

        public int EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
    }
}