using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaELearning.Data;
using System.Security.Claims;

namespace PlataformaELearning.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Inyectamos IWebHostEnvironment para saber dónde guardar los archivos físicamente
        public ProfileController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Account");

            var userProfile = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
            if (userProfile == null) return NotFound("Usuario no encontrado.");

            return View(userProfile);
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

            if (file.Length > 10 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "La imagen es demasiado pesada. El límite es 10MB.";
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
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null) return NotFound();

                // 1. SOLUCIÓN RAÍZ: Manejo seguro del WebRootPath
                string webRootPath = _webHostEnvironment.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRootPath))
                {
                    // Si es nulo, lo forzamos a apuntar a la carpeta base del proyecto
                    webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }

                string uploadsFolder = Path.Combine(webRootPath, "uploads", "profiles");

                // Creamos la carpeta físicamente si no existe
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = $"{Guid.NewGuid()}_{user.Matricula}{extension}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // 2. SOLUCIÓN AL BORRADO: Sintaxis correcta con .Delete()
                if (!string.IsNullOrEmpty(user.ProfilePicturePath))
                {
                    string oldPath = Path.Combine(webRootPath, user.ProfilePicturePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                user.ProfilePicturePath = $"/uploads/profiles/{uniqueFileName}";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Foto de perfil actualizada correctamente.";
            }
            catch (Exception ex)
            {
                // 3. DEBUGGING: Si falla, mandamos el mensaje real al Toastr, no explotará la vista
                TempData["ErrorMessage"] = $"Error interno del servidor: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}