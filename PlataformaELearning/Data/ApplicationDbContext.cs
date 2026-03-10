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
        public DbSet<Inscripcion> Inscripciones { get; set; }
        public DbSet<Calificacion> Calificaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Aquí podrías configurar relaciones más complejas si fuera necesario,
            // pero por ahora, con las anotaciones [ForeignKey] en los modelos es suficiente.
        }
    }
}