using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlataformaELearning.Models
{
    public class ApartadoCurso
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CursoId { get; set; }

        [Required(ErrorMessage = "El título del apartado es obligatorio")]
        [StringLength(200)]
        [Display(Name = "Título del Apartado")]
        public string Titulo { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Display(Name = "Orden")]
        public int Orden { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [ForeignKey("CursoId")]
        public Curso? Curso { get; set; }

        // Tareas dentro de este apartado
        public ICollection<TareaApartado>? Tareas { get; set; }
    }
}