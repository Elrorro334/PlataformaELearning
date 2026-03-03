using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaELearning.Data;
using System.Security.Claims;

namespace PlataformaELearning.Controllers
{
    [Authorize] // Asegura que solo usuarios autenticados entren aquí
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Extraer el email del usuario logueado desde sus Claims
            string? email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Account");
            }

            // Buscar la información completa en la BD (AsNoTracking para lectura rápida)
            var userProfile = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (userProfile == null)
            {
                return NotFound("No se encontró el perfil del usuario.");
            }

            return View(userProfile);
        }
    }
}