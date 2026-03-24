using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlataformaELearning.Models
{
    public class TareaApartado
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ApartadoId { get; set; }

        [Required(ErrorMessage = "El título de la tarea es obligatorio")]
        [StringLength(200)]
        [Display(Name = "Título de la Tarea")]
        public string Titulo { get; set; } = string.Empty;

        [Display(Name = "Descripción de la Tarea")]
        public string? Descripcion { get; set; }

        // Tipo de tarea (ej: "Laplace inversa", "Laplace normal")
        [Display(Name = "Tipo de Tarea")]
        public string? TipoTarea { get; set; }

        // Fecha límite de entrega
        [Required(ErrorMessage = "La fecha límite es obligatoria")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Límite de Entrega")]
        public DateTime FechaLimite { get; set; }

        // Puntos posibles
        [Display(Name = "Puntos Totales")]
        public int PuntosTotales { get; set; } = 100;

        // Materiales de apoyo (PDFs, videos, etc.)
        public ICollection<MaterialTarea>? Materiales { get; set; }

        [ForeignKey("ApartadoId")]
        public ApartadoCurso? Apartado { get; set; }
    }
}