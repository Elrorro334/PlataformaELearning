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

        public async Task<IActionResult> MisTareas(string filtro = "pendientes")
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Login", "Account");

            int alumnoId = int.Parse(userIdClaim);

            // 1. Obtener IDs de cursos inscritos
            var cursosInscritos = await _context.Inscripciones
                .Where(i => i.AlumnoId == alumnoId)
                .Select(i => i.CursoId)
                .ToListAsync();

            // 2. Obtener tareas asociadas a esos cursos
            var tareas = await _context.TareasApartados
                .Include(t => t.Apartado)
                    .ThenInclude(a => a!.Curso)
                .Include(t => t.Materiales)
                .Where(t => cursosInscritos.Contains(t.Apartado!.CursoId))
                .OrderBy(t => t.FechaLimite)
                .ToListAsync();

            // 3. Obtener entregas del alumno para estas tareas
            var entregas = await _context.EntregasTareas
                .Include(e => e.Calificacion)
                .Where(e => e.AlumnoId == alumnoId)
                .ToDictionaryAsync(e => e.TareaId);

            // 4. Pasar datos necesarios a la vista
            ViewBag.Entregas = entregas;
            ViewBag.TieneInscripciones = cursosInscritos.Any();
            ViewData["FiltroActual"] = filtro; // Usado para resaltar el botón activo en la vista

            return View(tareas);
        }
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

            bool inscrito = await _context.Inscripciones
                .AnyAsync(i => i.AlumnoId == alumnoId && i.CursoId == tarea.Apartado!.CursoId);

            if (!inscrito) return Forbid();

            var entrega = await _context.EntregasTareas
                .Include(e => e.Calificacion)
                .FirstOrDefaultAsync(e => e.TareaId == id && e.AlumnoId == alumnoId);

            // Lógica de tiempo restante detallada
            var tiempoRestante = tarea.FechaLimite - DateTime.Now;
            ViewBag.Entrega = entrega;
            ViewBag.DiasRestantes = tiempoRestante.Days;
            ViewBag.HorasRestantes = tiempoRestante.Hours;
            ViewBag.MinutosRestantes = tiempoRestante.Minutes;

            return View(tarea);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(104857600)]
        public async Task<IActionResult> Entregar(int tareaId, string comentario, IFormFile? archivo)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int alumnoId = int.Parse(userIdClaim);

            var tarea = await _context.TareasApartados.FindAsync(tareaId);
            if (tarea == null) return NotFound();

            // 1. Verificar si ya existe una entrega y si está calificada
            var entregaExistente = await _context.EntregasTareas
                .FirstOrDefaultAsync(e => e.TareaId == tareaId && e.AlumnoId == alumnoId);

            if (entregaExistente?.CalificacionId != null)
            {
                TempData["ErrorMessage"] = "No puedes modificar una tarea que ya ha sido calificada.";
                return RedirectToAction(nameof(Detalle), new { id = tareaId });
            }

            // 2. Verificar fecha límite (Classroom permite entregas tardías, pero aquí bloqueamos o marcamos)
            if (tarea.FechaLimite < DateTime.Now && entregaExistente == null)
            {
                TempData["ErrorMessage"] = "El plazo de entrega ha expirado.";
                return RedirectToAction(nameof(Detalle), new { id = tareaId });
            }

            if (entregaExistente != null)
            {
                // ACTUALIZACIÓN
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
                TempData["SuccessMessage"] = "Entrega actualizada.";
            }
            else
            {
                // NUEVA ENTREGA
                if (archivo == null || archivo.Length == 0)
                {
                    TempData["ErrorMessage"] = "Debes adjuntar un archivo para realizar la entrega.";
                    return RedirectToAction(nameof(Detalle), new { id = tareaId });
                }

                var nuevaEntrega = new EntregaTarea
                {
                    TareaId = tareaId,
                    AlumnoId = alumnoId,
                    FechaEntrega = DateTime.Now,
                    ComentarioAlumno = comentario
                };

                using var memoryStream = new MemoryStream();
                await archivo.CopyToAsync(memoryStream);
                nuevaEntrega.ArchivoEntregado = memoryStream.ToArray();
                nuevaEntrega.NombreArchivo = archivo.FileName;
                nuevaEntrega.ContentType = archivo.ContentType;

                _context.Add(nuevaEntrega);
                TempData["SuccessMessage"] = "Tarea entregada con éxito.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Detalle), new { id = tareaId });
        }

        [HttpGet]
        public async Task<IActionResult> DescargarArchivo(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();
            int currentUserId = int.Parse(userIdClaim);

            // Seguridad: Solo el dueño de la entrega (o un admin/maestro si cambiaras el Authorize) puede descargar
            var entrega = await _context.EntregasTareas
                .FirstOrDefaultAsync(e => e.Id == id);

            if (entrega == null) return NotFound();

            // Validar que el archivo pertenece al usuario actual
            if (entrega.AlumnoId != currentUserId) return Forbid();

            if (entrega.ArchivoEntregado == null) return NotFound();

            return File(entrega.ArchivoEntregado,
                        entrega.ContentType ?? "application/octet-stream",
                        entrega.NombreArchivo ?? "entrega.bin");
        }
    }
}