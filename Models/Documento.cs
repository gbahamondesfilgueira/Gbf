namespace Gbf.Models
{
    public class Documento
    {
        public int Id { get; set; }

        public string Nombre { get; set; }
        public string Ruta { get; set; }

        public int EmpresaId { get; set; }
        public Empresa Empresa { get; set; }
    }
}
