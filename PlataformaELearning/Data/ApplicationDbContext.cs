using Microsoft.EntityFrameworkCore;
using PlataformaELearning.Models;

namespace PlataformaELearning.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Curso> Cursos { get; set; }
        
        // Tablas agregadas por Beto
        public DbSet<ContenidoCurso> ContenidosCursos { get; set; }
        
        // Tablas agregadas en secondbranch
        public DbSet<Inscripcion> Inscripciones { get; set; }
        public DbSet<Calificacion> Calificaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración para Curso
            modelBuilder.Entity<Curso>()
                .HasOne(c => c.Maestro)
                .WithMany() // Asumiendo que un User puede tener muchos cursos
                .HasForeignKey(c => c.MaestroId)
                .OnDelete(DeleteBehavior.Restrict); // Evita que se elimine un maestro si tiene cursos

            // Configuración para ContenidoCurso
            modelBuilder.Entity<ContenidoCurso>()
                .HasOne(c => c.Curso)
                .WithMany(c => c.Contenidos)
                .HasForeignKey(c => c.CursoId)
                .OnDelete(DeleteBehavior.Cascade); // Si se elimina un curso, se elimina su contenido

            // Índices para mejorar búsquedas
            modelBuilder.Entity<Curso>()
                .HasIndex(c => c.MaestroId)
                .HasDatabaseName("IX_Curso_MaestroId");

            modelBuilder.Entity<ContenidoCurso>()
                .HasIndex(c => c.CursoId)
                .HasDatabaseName("IX_ContenidoCurso_CursoId");

            modelBuilder.Entity<ContenidoCurso>()
                .HasIndex(c => c.Tipo)
                .HasDatabaseName("IX_ContenidoCurso_Tipo");
        }
    }
}