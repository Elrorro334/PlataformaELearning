using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient; // Requerido para evitar el crash de memoria
using PlataformaELearning.Data;
using System.Security.Claims;
using System.IO;

namespace PlataformaELearning.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Account");

            var userProfile = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
            if (userProfile == null) return NotFound("Usuario no encontrado.");

            return View(userProfile);
        }

        [HttpGet]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)] // El navegador recordará la imagen 1 hora
        public async Task<IActionResult> GetAvatar()
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return NotFound();

            // Usamos Select para traer SOLO los bytes, sin cargar nombres, contraseñas, etc.
            var userImage = await _context.Users
                .Where(u => u.Email == email)
                .Select(u => new { u.ProfilePicture, u.ContentType })
                .FirstOrDefaultAsync();

            if (userImage?.ProfilePicture == null || userImage.ProfilePicture.Length == 0)
                return NotFound(); // Retorna 404 si no tiene foto

            return File(userImage.ProfilePicture, userImage.ContentType ?? "image/jpeg");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Por favor selecciona una imagen para subir.";
                return RedirectToAction(nameof(Index));
            }

            // Límite ajustado a 5MB para no saturar la memoria ni la base de datos
            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "La imagen es demasiado pesada. El límite es 5MB.";
                return RedirectToAction(nameof(Index));
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] = "Formato no soportado. Solo se permiten imágenes JPG, PNG o WEBP.";
                return RedirectToAction(nameof(Index));
            }

            if (!file.ContentType.StartsWith("image/"))
            {
                TempData["ErrorMessage"] = "El archivo seleccionado no es una imagen válida.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Solo verificamos que exista, no cargamos todo el objeto a memoria
                var userExists = await _context.Users.AnyAsync(u => u.Email == email);
                if (!userExists) return NotFound("Usuario no encontrado.");

                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                // SOLUCIÓN AL CIERRE INESPERADO (Crash 0xffffffff)
                // Usamos ADO.NET para enviar el flujo binario directamente a SQL Server
                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("No se pudo obtener la cadena de conexión.");
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string sql = "UPDATE Users SET ProfilePicture = @Photo, ContentType = @Type WHERE Email = @Email";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        // Se define explícitamente como VarBinary de longitud máxima
                        command.Parameters.Add("@Photo", System.Data.SqlDbType.VarBinary, -1).Value = fileBytes;
                        command.Parameters.AddWithValue("@Type", file.ContentType);
                        command.Parameters.AddWithValue("@Email", email);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                TempData["SuccessMessage"] = "Foto de perfil actualizada correctamente en la base de datos.";
            }
            catch (Exception ex)
            {
                // Registramos en consola para diagnóstico futuro sin tirar la app
                Console.WriteLine($"Error crítico al guardar imagen: {ex}");
                TempData["ErrorMessage"] = $"Error interno del servidor: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}