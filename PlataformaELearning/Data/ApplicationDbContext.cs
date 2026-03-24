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

        // ========== TABLAS EXISTENTES (NO TOCAR) ==========
        public DbSet<User> Users { get; set; }
        public DbSet<Curso> Cursos { get; set; }
        public DbSet<ContenidoCurso> ContenidosCursos { get; set; }
        public DbSet<Inscripcion> Inscripciones { get; set; }
        public DbSet<Calificacion> Calificaciones { get; set; }

        // ========== NUEVAS TABLAS ==========
        public DbSet<ApartadoCurso> ApartadosCursos { get; set; }
        public DbSet<TareaApartado> TareasApartados { get; set; }
        public DbSet<MaterialTarea> MaterialesTarea { get; set; }
        public DbSet<EntregaTarea> EntregasTareas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========== CONFIGURACIONES EXISTENTES (NO TOCAR) ==========

            // Configuración para Curso
            modelBuilder.Entity<Curso>()
                .HasOne(c => c.Maestro)
                .WithMany()
                .HasForeignKey(c => c.MaestroId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuración para ContenidoCurso (de Beto)
            modelBuilder.Entity<ContenidoCurso>()
                .HasOne(c => c.Curso)
                .WithMany(c => c.Contenidos)
                .HasForeignKey(c => c.CursoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices existentes
            modelBuilder.Entity<Curso>()
                .HasIndex(c => c.MaestroId)
                .HasDatabaseName("IX_Curso_MaestroId");

            modelBuilder.Entity<ContenidoCurso>()
                .HasIndex(c => c.CursoId)
                .HasDatabaseName("IX_ContenidoCurso_CursoId");

            modelBuilder.Entity<ContenidoCurso>()
                .HasIndex(c => c.Tipo)
                .HasDatabaseName("IX_ContenidoCurso_Tipo");

            // ========== NUEVAS CONFIGURACIONES ==========

            // Configuración para ApartadoCurso
            modelBuilder.Entity<ApartadoCurso>()
                .HasOne(a => a.Curso)
                .WithMany(c => c.Apartados)
                .HasForeignKey(a => a.CursoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuración para TareaApartado
            modelBuilder.Entity<TareaApartado>()
                .HasOne(t => t.Apartado)
                .WithMany(a => a.Tareas)
                .HasForeignKey(t => t.ApartadoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuración para MaterialTarea
            modelBuilder.Entity<MaterialTarea>()
                .HasOne(m => m.Tarea)
                .WithMany(t => t.Materiales)
                .HasForeignKey(m => m.TareaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuración para EntregaTarea
            modelBuilder.Entity<EntregaTarea>()
                .HasOne(e => e.Tarea)
                .WithMany()
                .HasForeignKey(e => e.TareaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EntregaTarea>()
                .HasOne(e => e.Alumno)
                .WithMany()
                .HasForeignKey(e => e.AlumnoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EntregaTarea>()
                .HasOne(e => e.Calificacion)
                .WithOne(c => c.Entrega)
                .HasForeignKey<EntregaTarea>(e => e.CalificacionId)
                .OnDelete(DeleteBehavior.SetNull);

            // ========== NUEVOS ÍNDICES ==========
            modelBuilder.Entity<ApartadoCurso>()
                .HasIndex(a => a.CursoId)
                .HasDatabaseName("IX_Apartado_CursoId");

            modelBuilder.Entity<TareaApartado>()
                .HasIndex(t => t.ApartadoId)
                .HasDatabaseName("IX_Tarea_ApartadoId");

            modelBuilder.Entity<TareaApartado>()
                .HasIndex(t => t.FechaLimite)
                .HasDatabaseName("IX_Tarea_FechaLimite");

            modelBuilder.Entity<EntregaTarea>()
                .HasIndex(e => e.TareaId)
                .HasDatabaseName("IX_Entrega_TareaId");

            modelBuilder.Entity<EntregaTarea>()
                .HasIndex(e => e.AlumnoId)
                .HasDatabaseName("IX_Entrega_AlumnoId");
        }
    }
}