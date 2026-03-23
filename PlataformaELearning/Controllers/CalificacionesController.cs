using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaELearning.Data;
using PlataformaELearning.Models;
using System.Security.Claims;

namespace PlataformaELearning.Controllers
{
    [Authorize(Roles = "Alumno")]
    public class CalificacionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CalificacionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Calificaciones/Index
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int alumnoId = int.Parse(userIdClaim);

            // Obtener todos los cursos donde el alumno está inscrito
            var cursosInscritos = await _context.Inscripciones
                .Where(i => i.AlumnoId == alumnoId)
                .Select(i => i.CursoId)
                .ToListAsync();

            // ========== Obtener TODAS las tareas de esos cursos ==========
            var todasLasTareas = await _context.TareasApartados
                .Include(t => t.Apartado)
                    .ThenInclude(a => a!.Curso)
                .Where(t => cursosInscritos.Contains(t.Apartado!.CursoId))
                .ToListAsync();

            // Obtener calificaciones del alumno (notas finales de curso)
            var calificaciones = await _context.Calificaciones
                .Include(c => c.Curso)
                .Include(c => c.Tarea)
                .Where(c => c.AlumnoId == alumnoId && cursosInscritos.Contains(c.CursoId))
                .OrderByDescending(c => c.FechaCalificacion)
                .ToListAsync();

            // Obtener entregas calificadas (tareas con calificación)
            var entregasCalificadas = await _context.EntregasTareas
                .Include(e => e.Tarea)
                    .ThenInclude(t => t!.Apartado)
                        .ThenInclude(a => a!.Curso)
                .Include(e => e.Calificacion)
                .Where(e => e.AlumnoId == alumnoId && e.CalificacionId != null)
                .ToListAsync();

            // ========== Pasar las tareas totales al ViewBag ==========
            ViewBag.TareasTotales = todasLasTareas;
            ViewBag.EntregasCalificadas = entregasCalificadas;

            return View(calificaciones);
        }

        // GET: /Calificaciones/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int alumnoId = int.Parse(userIdClaim);

            var calificacion = await _context.Calificaciones
                .Include(c => c.Curso)
                .Include(c => c.Tarea)
                .Include(c => c.Entrega)
                .FirstOrDefaultAsync(c => c.Id == id && c.AlumnoId == alumnoId);

            if (calificacion == null) return NotFound();

            return View(calificacion);
        }
    }
}