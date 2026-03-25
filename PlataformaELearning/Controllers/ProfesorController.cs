using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaELearning.Data;
using PlataformaELearning.Models;
using System.Security.Claims;

[Authorize(Roles = "Maestro,Administrador")]
public class ProfesorController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProfesorController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ========== MÉTODO AUXILIAR PARA OBTENER ID DEL PROFESOR ==========
    private int? ObtenerProfesorId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return null;
        return int.Parse(userIdClaim);
    }

    // ========== LISTADO DE CURSOS DEL PROFESOR ==========
    public async Task<IActionResult> MisCursos()
    {
        var userId = ObtenerProfesorId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var cursos = await _context.Cursos
            .Where(c => c.MaestroId == userId)
            .Include(c => c.Apartados!)
                .ThenInclude(a => a.Tareas!)
            .ToListAsync();

        return View(cursos);
    }

    // ========== TAREAS PENDIENTES DE CALIFICAR ==========
    public async Task<IActionResult> TareasPendientes()
    {
        var userId = ObtenerProfesorId();
        if (userId == null) return RedirectToAction("Login", "Account");

        // Obtener los IDs de los cursos del profesor
        var cursosIds = await _context.Cursos
            .Where(c => c.MaestroId == userId)
            .Select(c => c.Id)
            .ToListAsync();

        // Obtener todas las tareas de esos cursos
        var tareas = await _context.TareasApartados
            .Include(t => t.Apartado)
                .ThenInclude(a => a!.Curso)
            .Include(t => t.Materiales)
            .Where(t => cursosIds.Contains(t.Apartado!.CursoId))
            .ToListAsync();

        // Para cada tarea, obtener las entregas sin calificar
        var resultado = new List<object>();
        foreach (var tarea in tareas)
        {
            var entregasSinCalificar = await _context.EntregasTareas
                .Include(e => e.Alumno)
                .Where(e => e.TareaId == tarea.Id && e.CalificacionId == null)
                .ToListAsync();

            if (entregasSinCalificar.Any())
            {
                resultado.Add(new
                {
                    Tarea = tarea,
                    EntregasSinCalificar = entregasSinCalificar
                });
            }
        }

        return View(resultado);
    }

    // ========== PREVISUALIZAR ARCHIVO ==========
    [HttpGet]
    public async Task<IActionResult> PrevisualizarEntrega(int id)
    {
        var userId = ObtenerProfesorId();
        if (userId == null) return Unauthorized();

        var entrega = await _context.EntregasTareas
            .Include(e => e.Tarea)
                .ThenInclude(t => t.Apartado)
                    .ThenInclude(a => a!.Curso)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entrega == null || entrega.ArchivoEntregado == null)
            return NotFound();

        // Verificar permisos: Solo el maestro del curso o un admin pueden ver
        if (entrega.Tarea?.Apartado?.Curso?.MaestroId != userId && !User.IsInRole("Administrador"))
        {
            return Forbid();
        }

        // Configurar Content-Type adecuadamente para previsualización
        string contentType = entrega.ContentType ?? "application/octet-stream";
        string fileExtension = Path.GetExtension(entrega.NombreArchivo)?.ToLower() ?? "";

        // Forzar renderizado en el navegador para tipos soportados (pdf, imagenes, texto)
        if (fileExtension == ".pdf") contentType = "application/pdf";
        else if (fileExtension == ".jpg" || fileExtension == ".jpeg") contentType = "image/jpeg";
        else if (fileExtension == ".png") contentType = "image/png";
        else if (fileExtension == ".txt") contentType = "text/plain";

        // Para archivos Word/Excel es más complejo previsualizarlos nativamente.
        // Usaremos un visor de Office si es posible, pero por ahora devolvemos el archivo.
        // En la vista manejaremos qué mostrar.

        // No agregamos header de 'attachment' para que el navegador intente mostrarlo
        return File(entrega.ArchivoEntregado, contentType);
    }

    // ========== VER ENTREGAS DE UNA TAREA ESPECÍFICA ==========
    public async Task<IActionResult> VerEntregas(int tareaId)
    {
        var userId = ObtenerProfesorId();
        if (userId == null) return RedirectToAction("Login", "Account");

        // Verificar que la tarea pertenece a un curso del profesor
        var tarea = await _context.TareasApartados
            .Include(t => t.Apartado)
                .ThenInclude(a => a!.Curso)
            .FirstOrDefaultAsync(t => t.Id == tareaId);

        if (tarea == null) return NotFound();

        // Verificar permiso
        if (tarea.Apartado?.Curso?.MaestroId != userId && !User.IsInRole("Administrador"))
        {
            return Forbid();
        }

        var entregas = await _context.EntregasTareas
            .Include(e => e.Alumno)
            .Include(e => e.Calificacion)
            .Where(e => e.TareaId == tareaId)
            .OrderByDescending(e => e.FechaEntrega)
            .ToListAsync();

        ViewBag.Tarea = tarea;
        return View(entregas);
    }

    // ========== CALIFICAR ENTREGA (GET) ==========
    [HttpGet]
    public async Task<IActionResult> CalificarEntrega(int entregaId)
    {
        var userId = ObtenerProfesorId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var entrega = await _context.EntregasTareas
            .Include(e => e.Alumno)
            .Include(e => e.Tarea)
                .ThenInclude(t => t!.Apartado)
                    .ThenInclude(a => a!.Curso)
            .Include(e => e.Calificacion)
            .FirstOrDefaultAsync(e => e.Id == entregaId);

        if (entrega == null) return NotFound();

        // Verificar permiso
        if (entrega.Tarea?.Apartado?.Curso?.MaestroId != userId && !User.IsInRole("Administrador"))
        {
            return Forbid();
        }

        return View(entrega);
    }

    // ========== CALIFICAR ENTREGA (POST) ==========
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalificarEntrega(int entregaId, decimal puntuacion, string comentarios)
    {
        var userId = ObtenerProfesorId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var entrega = await _context.EntregasTareas
            .Include(e => e.Tarea)
                .ThenInclude(t => t!.Apartado)
                    .ThenInclude(a => a!.Curso)
            .FirstOrDefaultAsync(e => e.Id == entregaId);

        if (entrega == null) return NotFound();

        // Verificar permiso
        if (entrega.Tarea?.Apartado?.Curso?.MaestroId != userId && !User.IsInRole("Administrador"))
        {
            return Forbid();
        }

        // Validar puntuación
        if (puntuacion < 0 || puntuacion > 10)
        {
            TempData["ErrorMessage"] = "La calificación debe ser entre 0 y 10";
            return RedirectToAction(nameof(CalificarEntrega), new { entregaId });
        }

        // Crear o actualizar calificación
        if (entrega.CalificacionId.HasValue)
        {
            var calificacion = await _context.Calificaciones.FindAsync(entrega.CalificacionId);
            if (calificacion != null)
            {
                calificacion.Puntuacion = puntuacion;
                calificacion.Comentarios = comentarios;
                calificacion.FechaCalificacion = DateTime.Now;
                _context.Update(calificacion);
            }
        }
        else
        {
            var nuevaCalificacion = new Calificacion
            {
                CursoId = entrega.Tarea!.Apartado!.CursoId,
                AlumnoId = entrega.AlumnoId,
                TareaId = entrega.TareaId,
                EntregaId = entregaId,
                Puntuacion = puntuacion,
                Comentarios = comentarios,
                FechaCalificacion = DateTime.Now
            };
            _context.Add(nuevaCalificacion);
            await _context.SaveChangesAsync(); // Guardar para obtener el ID

            entrega.CalificacionId = nuevaCalificacion.Id;
            _context.Update(entrega);
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Calificación guardada correctamente.";
        return RedirectToAction(nameof(VerEntregas), new { tareaId = entrega.TareaId });
    }

    // ========== DESCARGAR ARCHIVO DE ENTREGA ==========
    [HttpGet]
    public async Task<IActionResult> DescargarEntrega(int id)
    {
        var userId = ObtenerProfesorId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var entrega = await _context.EntregasTareas
            .Include(e => e.Tarea)
                .ThenInclude(t => t!.Apartado)
                    .ThenInclude(a => a!.Curso)
            .Where(e => e.Id == id)
            .Select(e => new {
                e.ArchivoEntregado,
                e.ContentType,
                e.NombreArchivo,
                CursoMaestroId = e.Tarea != null && e.Tarea.Apartado != null && e.Tarea.Apartado.Curso != null ? e.Tarea.Apartado.Curso.MaestroId : 0
            })
            .FirstOrDefaultAsync();

        if (entrega?.ArchivoEntregado == null) return NotFound();

        // Verificar permiso
        if (entrega.CursoMaestroId != userId && !User.IsInRole("Administrador"))
        {
            return Forbid();
        }

        return File(entrega.ArchivoEntregado, entrega.ContentType ?? "application/octet-stream",
                    entrega.NombreArchivo ?? $"entrega_{id}.pdf");
    }

    // ========== MÉTODOS EXISTENTES (CALIFICAR CURSO) - ACTUALIZADOS ==========
    public async Task<IActionResult> CalificarCurso(int id)
    {
        var userId = ObtenerProfesorId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var curso = await _context.Cursos
            .Include(c => c.Apartados!)
                .ThenInclude(a => a.Tareas!)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (curso == null) return NotFound();

        // Verificar permiso
        if (curso.MaestroId != userId && !User.IsInRole("Administrador"))
        {
            return Forbid();
        }

        // Obtener alumnos inscritos en el curso
        var alumnosInscritos = await _context.Inscripciones
            .Where(i => i.CursoId == id)
            .Include(i => i.Alumno)
            .Select(i => i.Alumno)
            .ToListAsync();

        // Obtener calificaciones del curso
        var calificaciones = await _context.Calificaciones
            .Where(c => c.CursoId == id)
            .ToListAsync();

        ViewBag.CursoId = id;
        ViewBag.CursoNombre = curso.Nombre;
        ViewBag.Calificaciones = calificaciones;
        ViewBag.Tareas = curso.Apartados?.SelectMany(a => a.Tareas ?? new List<TareaApartado>()).ToList() ?? new List<TareaApartado>();

        return View(alumnosInscritos);
    }

    // GET: Formulario para asignar nota (ACTUALIZADO para soportar tareas)
    public async Task<IActionResult> AsignarCalificacion(int cursoId, int alumnoId, int? tareaId = null)
    {
        var userId = ObtenerProfesorId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var alumno = await _context.Users.FindAsync(alumnoId);
        var curso = await _context.Cursos.FindAsync(cursoId);

        if (alumno == null || curso == null) return NotFound();

        // Verificar permiso
        if (curso.MaestroId != userId && !User.IsInRole("Administrador"))
        {
            return Forbid();
        }

        Calificacion calificacion;

        if (tareaId.HasValue)
        {
            // Calificación por tarea específica
            calificacion = await _context.Calificaciones
                .FirstOrDefaultAsync(c => c.CursoId == cursoId && c.AlumnoId == alumnoId && c.TareaId == tareaId)
                ?? new Calificacion { CursoId = cursoId, AlumnoId = alumnoId, TareaId = tareaId };
        }
        else
        {
            // Calificación general del curso (existente)
            calificacion = await _context.Calificaciones
                .FirstOrDefaultAsync(c => c.CursoId == cursoId && c.AlumnoId == alumnoId && c.TareaId == null)
                ?? new Calificacion { CursoId = cursoId, AlumnoId = alumnoId };
        }

        ViewBag.AlumnoNombre = $"{alumno.FirstName} {alumno.LastName}";
        ViewBag.CursoNombre = curso.Nombre;
        ViewBag.TareaId = tareaId;

        return View(calificacion);
    }

    // POST: Guardar nota (ACTUALIZADO)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsignarCalificacion(Calificacion calificacion)
    {
        if (ModelState.IsValid)
        {
            // Verificar permisos
            var userId = ObtenerProfesorId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var curso = await _context.Cursos.FindAsync(calificacion.CursoId);
            if (curso?.MaestroId != userId && !User.IsInRole("Administrador"))
            {
                return Forbid();
            }

            calificacion.FechaCalificacion = DateTime.Now;

            if (calificacion.Id == 0)
                _context.Add(calificacion);
            else
                _context.Update(calificacion);

            await _context.SaveChangesAsync();

            if (calificacion.TareaId.HasValue)
            {
                // Si es calificación de tarea, redirigir a VerEntregas
                return RedirectToAction(nameof(VerEntregas), new { tareaId = calificacion.TareaId });
            }
            else
            {
                // Si es calificación de curso, redirigir a CalificarCurso
                return RedirectToAction(nameof(CalificarCurso), new { id = calificacion.CursoId });
            }
        }
        return View(calificacion);
    }
}