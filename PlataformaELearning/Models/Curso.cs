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

        // Esta llave foránea conecta directamente con tu clase User
        [ForeignKey("MaestroId")]
        public User? Maestro { get; set; }
    }
}