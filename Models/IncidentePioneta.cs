using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gbf.Models
{
    public class IncidentePioneta
    {
        [Key]
        public int Id { get; set; }

        // 🔹 Relación con Incidente
        public int IncidenteId { get; set; }
        public Incidente Incidente { get; set; }

        // 🔹 Relación con Pioneta
        public int PionetaId { get; set; }
        public Pioneta Pioneta { get; set; }
    }
}