using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlataformaELearning.Models
{
    public class Calificacion
    {
        [Key]
        public int Id { get; set; }

        // RELACIONES EXISTENTES (se mantienen)
        [Required(ErrorMessage = "Debe seleccionar un curso")]
        public int CursoId { get; set; }

        [ForeignKey("CursoId")]
        public Curso? Curso { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un alumno")]
        public int AlumnoId { get; set; }

        [ForeignKey("AlumnoId")]
        public User? Alumno { get; set; }

        // ========== NUEVAS RELACIONES ==========
        public int? TareaId { get; set; }

        [ForeignKey("TareaId")]
        public TareaApartado? Tarea { get; set; }

        public int? EntregaId { get; set; }

        [ForeignKey("EntregaId")]
        public EntregaTarea? Entrega { get; set; }

        // CAMPOS EXISTENTES
        [Required(ErrorMessage = "La calificación es obligatoria")]
        [Range(0, 10, ErrorMessage = "La calificación debe ser entre 0 y 10")]
        [Column(TypeName = "decimal(4,2)")]
        [Display(Name = "Calificación")]
        public decimal Puntuacion { get; set; }

        [StringLength(250, ErrorMessage = "El comentario no puede exceder los 250 caracteres")]
        [Display(Name = "Comentarios")]
        public string? Comentarios { get; set; }

        // ========== NUEVOS CAMPOS ==========
        [DataType(DataType.Date)]
        public DateTime FechaCalificacion { get; set; } = DateTime.Now;
    }
}