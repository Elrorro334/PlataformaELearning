using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaELearning.Data;
using PlataformaELearning.Models;
using System.Security.Claims;

namespace PlataformaELearning.Controllers
{
    [Authorize(Roles = "Alumno")]
    public class InscripcionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InscripcionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Mostrar cursos disponibles para inscribirse
        public async Task<IActionResult> CursosDisponibles()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            // Obtener IDs de cursos donde ya está inscrito
            var cursosInscritos = await _context.Inscripciones
                .Where(i => i.AlumnoId == userId)
                .Select(i => i.CursoId)
                .ToListAsync();

            // Mostrar cursos NO inscritos
            var cursosDisponibles = await _context.Cursos
                .Include(c => c.Maestro)
                .Where(c => !cursosInscritos.Contains(c.Id))
                .ToListAsync();

            return View(cursosDisponibles);
        }

        // GET: Mis cursos (ya inscritos)
        public async Task<IActionResult> MisCursos()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            var misCursos = await _context.Inscripciones
                .Include(i => i.Curso)
                .ThenInclude(c => c!.Maestro)
                .Where(i => i.AlumnoId == userId)
                .Select(i => i.Curso)
                .ToListAsync();

            return View(misCursos);
        }

        // POST: Inscribirse a un curso
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inscribirse(int cursoId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim);

            // Verificar si ya está inscrito
            bool yaInscrito = await _context.Inscripciones
                .AnyAsync(i => i.CursoId == cursoId && i.AlumnoId == userId);

            if (yaInscrito)
            {
                TempData["ErrorMessage"] = "Ya estás inscrito en este curso";
                return RedirectToAction(nameof(CursosDisponibles));
            }

            // Verificar que el curso existe
            var curso = await _context.Cursos.FindAsync(cursoId);
            if (curso == null) return NotFound();

            // Crear inscripción
            var inscripcion = new Inscripcion
            {
                CursoId = cursoId,
                AlumnoId = userId,
                FechaInscripcion = DateTime.Now
            };

            _context.Add(inscripcion);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "¡Te has inscrito correctamente al curso!";
            return RedirectToAction(nameof(MisCursos));
        }

        // POST: Desinscribirse de un curso
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desinscribirse(int cursoId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var inscripcion = await _context.Inscripciones
                .FirstOrDefaultAsync(i => i.CursoId == cursoId && i.AlumnoId == userId);

            if (inscripcion != null)
            {
                _context.Inscripciones.Remove(inscripcion);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Te has desinscrito del curso";
            }

            return RedirectToAction(nameof(MisCursos));
        }
    }
}