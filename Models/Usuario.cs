namespace Gbf.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        public string Nombre { get; set; }
        public string Apellido { get; set; }

        public string Email { get; set; }
        public string Username { get; set; }

        public string PasswordHash { get; set; }
        public string? Salt { get; set; }

        public string Rol { get; set; }

        public int? EmpresaId { get; set; }

        public bool Activo { get; set; }
        public bool Bloqueado { get; set; }

        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }

        public DateTime? UltimoAcceso { get; set; }

        public int IntentosFallidos { get; set; }
    }
}