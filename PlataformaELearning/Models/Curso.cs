using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlataformaELearning.Models
{
    public class Curso
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del curso es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        [Display(Name = "Nombre del Curso")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción es muy larga")]
        [Display(Name = "Descripción del Curso")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El curso debe tener un maestro asignado")]
        [Display(Name = "Maestro Asignado")]
        public int MaestroId { get; set; }

        [ForeignKey("MaestroId")]
        public User? Maestro { get; set; }

        // Propiedad de navegación para los contenidos del curso (de Beto)
        public ICollection<ContenidoCurso>? Contenidos { get; set; }

        // ========== NUEVO: Apartados del curso (agregado sin modificar lo existente) ==========
        public ICollection<ApartadoCurso>? Apartados { get; set; }
    }

    public class ContenidoCurso
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CursoId { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public TipoContenido Tipo { get; set; }

        // Para anuncios
        public string? ContenidoTexto { get; set; }

        // Para PDFs (Mantenemos NombreArchivo de Beto, agregamos los binarios para SQL)
        public string? RutaArchivo { get; set; }
        public string? NombreArchivo { get; set; }
        public byte[]? ArchivoFisico { get; set; }
        public string? ContentType { get; set; }

        // Para videos
        public string? UrlVideo { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Publicación")]
        public DateTime FechaPublicacion { get; set; } = DateTime.Now;



        [ForeignKey("CursoId")]
        public Curso? Curso { get; set; }
    }

    public enum TipoContenido
    {
        Anuncio,
        PDF,
        Video
    }
}