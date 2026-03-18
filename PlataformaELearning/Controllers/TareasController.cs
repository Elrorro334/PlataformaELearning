using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaELearning.Data;
using PlataformaELearning.Models;
using System.Security.Claims;

namespace PlataformaELearning.Controllers
{
    [Authorize(Roles = "Alumno")]
    public class TareasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TareasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Tareas/MisTareas
        public async Task<IActionResult> MisTareas()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int alumnoId = int.Parse(userIdClaim);

            // Obtener cursos en los que está inscrito
            var cursosInscritos = await _context.Inscripciones
                .Where(i => i.AlumnoId == alumnoId)
                .Select(i => i.CursoId)
                .ToListAsync();

            // Obtener tareas de esos cursos
            var tareas = await _context.TareasApartados
                .Include(t => t.Apartado)
                .ThenInclude(a => a!.Curso)
                .Include(t => t.Materiales)
                .Where(t => cursosInscritos.Contains(t.Apartado!.CursoId))
                .OrderBy(t => t.FechaLimite)
                .ToListAsync();

            // Verificar entregas existentes
            var entregas = await _context.EntregasTareas
                .Where(e => e.AlumnoId == alumnoId)
                .ToDictionaryAsync(e => e.TareaId);

            ViewBag.Entregas = entregas;
            ViewBag.TieneInscripciones = cursosInscritos.Any();

            return View(tareas);
        }

        // GET: /Tareas/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int alumnoId = int.Parse(userIdClaim);

            var tarea = await _context.TareasApartados
                .Include(t => t.Apartado)
                .ThenInclude(a => a!.Curso)
                .ThenInclude(c => c!.Maestro)
                .Include(t => t.Materiales)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tarea == null) return NotFound();

            // Verificar que el alumno está inscrito en el curso
            bool inscrito = await _context.Inscripciones
                .AnyAsync(i => i.AlumnoId == alumnoId && i.CursoId == tarea.Apartado!.CursoId);

            if (!inscrito) return Forbid();

            // Verificar si ya entregó
            var entrega = await _context.EntregasTareas
                .Include(e => e.Calificacion)
                .FirstOrDefaultAsync(e => e.TareaId == id && e.AlumnoId == alumnoId);

            ViewBag.Entrega = entrega;
            ViewBag.DiasRestantes = (tarea.FechaLimite - DateTime.Now).Days;
            ViewBag.HorasRestantes = (tarea.FechaLimite - DateTime.Now).Hours;

            return View(tarea);
        }

        // POST: /Tareas/Entregar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(104857600)] // 100MB
        public async Task<IActionResult> Entregar(int tareaId, string comentario, IFormFile? archivo)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int alumnoId = int.Parse(userIdClaim);

            var tarea = await _context.TareasApartados
                .Include(t => t.Apartado)
                .FirstOrDefaultAsync(t => t.Id == tareaId);

            if (tarea == null) return NotFound();

            // Verificar fecha límite
            if (tarea.FechaLimite < DateTime.Now)
            {
                TempData["ErrorMessage"] = "La tarea ya ha vencido. No puedes entregarla.";
                return RedirectToAction(nameof(Detalle), new { id = tareaId });
            }

            // Verificar si ya entregó
            var entregaExistente = await _context.EntregasTareas
                .FirstOrDefaultAsync(e => e.TareaId == tareaId && e.AlumnoId == alumnoId);

            if (entregaExistente != null)
            {
                // Actualizar entrega existente
                entregaExistente.FechaEntrega = DateTime.Now;
                entregaExistente.ComentarioAlumno = comentario;

                if (archivo != null && archivo.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await archivo.CopyToAsync(memoryStream);
                    entregaExistente.ArchivoEntregado = memoryStream.ToArray();
                    entregaExistente.NombreArchivo = archivo.FileName;
                    entregaExistente.ContentType = archivo.ContentType;
                }

                _context.Update(entregaExistente);
                TempData["SuccessMessage"] = "Entrega actualizada correctamente.";
            }
            else
            {
                // Crear nueva entrega
                var nuevaEntrega = new EntregaTarea
                {
                    TareaId = tareaId,
                    AlumnoId = alumnoId,
                    FechaEntrega = DateTime.Now,
                    ComentarioAlumno = comentario
                };

                if (archivo != null && archivo.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await archivo.CopyToAsync(memoryStream);
                    nuevaEntrega.ArchivoEntregado = memoryStream.ToArray();
                    nuevaEntrega.NombreArchivo = archivo.FileName;
                    nuevaEntrega.ContentType = archivo.ContentType;
                }

                _context.Add(nuevaEntrega);
                TempData["SuccessMessage"] = "Tarea entregada correctamente.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Detalle), new { id = tareaId });
        }

        // GET: /Tareas/DescargarArchivo/5
        public async Task<IActionResult> DescargarArchivo(int id)
        {
            var entrega = await _context.EntregasTareas
                .Where(e => e.Id == id)
                .Select(e => new { e.ArchivoEntregado, e.ContentType, e.NombreArchivo })
                .FirstOrDefaultAsync();

            if (entrega?.ArchivoEntregado == null) return NotFound();

            return File(entrega.ArchivoEntregado, entrega.ContentType ?? "application/octet-stream",
                        entrega.NombreArchivo ?? $"archivo_{id}.bin");
        }
    }
}