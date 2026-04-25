using System.ComponentModel.DataAnnotations;

namespace Gbf.Models
{
    public class Conductor
    {
        public int Id { get; set; }

        // 🔹 DATOS PERSONALES
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El RUT es obligatorio")]
        [StringLength(20)]
        public string Rut { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }

        [Required(ErrorMessage = "La clase de licencia es obligatoria")]
        [StringLength(2)] // A1, B, C, etc.
        public string ClaseLicencia { get; set; } = string.Empty;

        // 🔹 ARCHIVOS (TODOS OPCIONALES)
        public string? CarnetFrontalUrl { get; set; }
        public string? CarnetTraseroUrl { get; set; }
        public string? LicenciaFrontalUrl { get; set; }
        public string? LicenciaTraseraUrl { get; set; }
        public string? CertificadoAntecedentesUrl { get; set; }
        public string? FotoPerfilUrl { get; set; }

        // 🔹 ESTADO
        public bool Activo { get; set; } = true;

        // 🔹 RELACIÓN
        [Required]
        public int EmpresaId { get; set; }

        public Empresa? Empresa { get; set; }
    }
}