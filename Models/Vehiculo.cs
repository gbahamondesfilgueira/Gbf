using System.ComponentModel.DataAnnotations;


namespace Gbf.Models
{

    public class Vehiculo
    {
        public int Id { get; set; }

        [Required]
        public string Patente { get; set; }

        [Required]
        public string Marca { get; set; }

        [Required]
        public string Modelo { get; set; }

        public string Color { get; set; }
        public string Tipo { get; set; }

        public int Año { get; set; }

        public bool Activo { get; set; } = true;

        public int EmpresaId { get; set; }
        public Empresa Empresa { get; set; }
    }
}
