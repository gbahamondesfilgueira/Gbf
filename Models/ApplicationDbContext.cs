using Microsoft.EntityFrameworkCore;
using Gbf.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Empresa> Empresas { get; set; }
    public DbSet<Documento> Documentos { get; set; }

    public DbSet<Vehiculo> Vehiculos { get; set; }
    public DbSet<Conductor> Conductores { get; set; }
    public DbSet<Pioneta> Pionetas { get; set; }

    public DbSet<Incidente> Incidentes { get; set; }
    public DbSet<IncidentePioneta> IncidentePionetas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 🔹 IncidentePioneta
        modelBuilder.Entity<IncidentePioneta>()
            .HasOne(ip => ip.Incidente)
            .WithMany(i => i.IncidentePionetas)
            .HasForeignKey(ip => ip.IncidenteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IncidentePioneta>()
            .HasOne(ip => ip.Pioneta)
            .WithMany()
            .HasForeignKey(ip => ip.PionetaId)
            .OnDelete(DeleteBehavior.Restrict);

        // 🔹 Incidente relaciones
        modelBuilder.Entity<Incidente>()
            .HasOne(i => i.Empresa)
            .WithMany()
            .HasForeignKey(i => i.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Incidente>()
            .HasOne(i => i.Vehiculo)
            .WithMany()
            .HasForeignKey(i => i.VehiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Incidente>()
            .HasOne(i => i.Conductor)
            .WithMany()
            .HasForeignKey(i => i.ConductorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}