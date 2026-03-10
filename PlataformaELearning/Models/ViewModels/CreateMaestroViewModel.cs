using System.ComponentModel.DataAnnotations;

namespace PlataformaELearning.Models.ViewModels
{
    public class CreateMaestroViewModel
    {
        // --- DATOS DEL MAESTRO ---
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La matrícula es obligatoria")]
        public string Matricula { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(8, ErrorMessage = "Mínimo 8 caracteres")]
        public string Password { get; set; } = string.Empty;

        public string? Telephone { get; set; }

        // --- DATOS DEL CURSO A ASIGNAR ---
        [Required(ErrorMessage = "El nombre del curso es obligatorio")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string NombreCurso { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string? DescripcionCurso { get; set; }
    }
}