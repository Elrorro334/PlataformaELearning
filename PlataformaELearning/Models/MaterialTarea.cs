using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlataformaELearning.Models
{
    public class MaterialTarea
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TareaId { get; set; }

        [Required]
        public string Titulo { get; set; } = string.Empty;

        public TipoMaterial Tipo { get; set; }

        // Para texto
        public string? ContenidoTexto { get; set; }

        // Para PDFs (igual que en ContenidoCurso)
        public byte[]? ArchivoFisico { get; set; }
        public string? NombreArchivo { get; set; }
        public string? ContentType { get; set; }

        // Para videos
        public string? UrlVideo { get; set; }

        [ForeignKey("TareaId")]
        public TareaApartado? Tarea { get; set; }
    }

    public enum TipoMaterial
    {
        Texto,
        PDF,
        Video
    }
}