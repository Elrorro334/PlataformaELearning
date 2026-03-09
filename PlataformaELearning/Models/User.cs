using System.ComponentModel.DataAnnotations;

namespace PlataformaELearning.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        public string Password { get; set; } = string.Empty;

        public string? Telephone { get; set; }

        [Required]
        public string Matricula { get; set; } = string.Empty;

        public string Role { get; set; } = "Alumno";       
        public string? ProfilePicturePath { get; set; }
    }
}