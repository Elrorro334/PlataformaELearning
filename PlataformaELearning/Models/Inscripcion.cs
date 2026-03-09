using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlataformaELearning.Models
{
    public class Inscripcion
    {
        [Key]
        public int Id { get; set; }

        // Conecta con el Curso
        [Required(ErrorMessage = "El curso es obligatorio")]
        public int CursoId { get; set; }

        [ForeignKey("CursoId")]
        public Curso? Curso { get; set; }

        // Conecta con el Alumno (User)
        [Required(ErrorMessage = "El alumno es obligatorio")]
        public int AlumnoId { get; set; }

        [ForeignKey("AlumnoId")]
        public User? Alumno { get; set; }

        [Display(Name = "Fecha de Inscripción")]
        public DateTime FechaInscripcion { get; set; } = DateTime.Now;
    }
}