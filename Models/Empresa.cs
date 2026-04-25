namespace Gbf.Models
{
    public class Empresa
    {
        public int Id { get; set; }

        public string Nombre { get; set; }
        public string Rut { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }

        public string LogoUrl { get; set; }

        public bool Activo { get; set; }
        public string Slug { get; set; }

        // 🔥 RELACIONES MULTIEMPRESA
        public List<Vehiculo> Vehiculos { get; set; }
        public List<Conductor> Conductores { get; set; }
        public List<Pioneta> Pionetas { get; set; }
        public List<Incidente> Incidentes { get; set; }

        public List<Documento> Documentos { get; set; }
    }
}