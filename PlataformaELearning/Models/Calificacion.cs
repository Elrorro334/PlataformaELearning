using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlataformaELearning.Models
{
    public class Calificacion
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un curso")]
        public int CursoId { get; set; }

        [ForeignKey("CursoId")]
        public Curso? Curso { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un alumno")]
        public int AlumnoId { get; set; }

        [ForeignKey("AlumnoId")]
        public User? Alumno { get; set; }

        [Required(ErrorMessage = "La calificación es obligatoria")]
        [Range(0, 10, ErrorMessage = "La calificación debe ser entre 0 y 10")]
        [Column(TypeName = "decimal(4,2)")] 
        [Display(Name = "Calificación Final")]
        public decimal Puntuacion { get; set; }

        [StringLength(250, ErrorMessage = "El comentario no puede exceder los 250 caracteres")]
        [Display(Name = "Comentarios del Maestro")]
        public string? Comentarios { get; set; }
    }
}