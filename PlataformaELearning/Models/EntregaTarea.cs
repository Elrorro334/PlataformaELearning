using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlataformaELearning.Models
{
    public class EntregaTarea
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TareaId { get; set; }

        [ForeignKey("TareaId")]
        public TareaApartado? Tarea { get; set; }

        [Required]
        public int AlumnoId { get; set; }

        [ForeignKey("AlumnoId")]
        public User? Alumno { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime FechaEntrega { get; set; } = DateTime.Now;

        public string? ComentarioAlumno { get; set; }

        // Archivos de la entrega
        public byte[]? ArchivoEntregado { get; set; }
        public string? NombreArchivo { get; set; }
        public string? ContentType { get; set; }

        // Relación con calificación (opcional)
        public int? CalificacionId { get; set; }

        [ForeignKey("CalificacionId")]
        public Calificacion? Calificacion { get; set; }

        // Propiedad calculada (no se guarda en BD)
        [NotMapped]
        public bool EsEntregaTardia => Tarea != null && FechaEntrega > Tarea.FechaLimite;
    }
}