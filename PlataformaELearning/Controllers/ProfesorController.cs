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

    // Listado de cursos que imparte el profesor logueado
    public async Task<IActionResult> MisCursos()
    {
        // Buscamos el claim que acabamos de agregar
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            return RedirectToAction("Login", "Account");
        }

        int userId = int.Parse(userIdClaim);

        // Filtrar los cursos donde el MaestroId coincida con el usuario logueado
        var cursos = await _context.Cursos
            .Where(c => c.MaestroId == userId)
            .ToListAsync();

        return View(cursos);
    }

    // Vista que ya tienes (Calificar Alumnos)
    public async Task<IActionResult> CalificarCurso(int id)
    {
        var curso = await _context.Cursos.FindAsync(id);
        if (curso == null) return NotFound();

        // En un sistema real, aquí filtrarías por alumnos inscritos a este curso
        var alumnos = await _context.Users.Where(u => u.Role == "Alumno").ToListAsync();

        var calificaciones = await _context.Calificaciones
            .Where(c => c.CursoId == id)
            .ToListAsync();

        ViewBag.CursoId = id;
        ViewBag.CursoNombre = curso.Nombre;
        ViewBag.Calificaciones = calificaciones;

        return View(alumnos);
    }

    // GET: Formulario para asignar nota
    public async Task<IActionResult> AsignarCalificacion(int cursoId, int alumnoId)
    {
        var alumno = await _context.Users.FindAsync(alumnoId);
        var curso = await _context.Cursos.FindAsync(cursoId);

        if (alumno == null || curso == null) return NotFound();

        var calificacion = await _context.Calificaciones
            .FirstOrDefaultAsync(c => c.CursoId == cursoId && c.AlumnoId == alumnoId)
            ?? new Calificacion { CursoId = cursoId, AlumnoId = alumnoId };

        ViewBag.AlumnoNombre = $"{alumno.FirstName} {alumno.LastName}";
        ViewBag.CursoNombre = curso.Nombre;

        return View(calificacion);
    }

    // POST: Guardar nota
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsignarCalificacion(Calificacion calificacion)
    {
        if (ModelState.IsValid)
        {
            if (calificacion.Id == 0)
                _context.Add(calificacion);
            else
                _context.Update(calificacion);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(CalificarCurso), new { id = calificacion.CursoId });
        }
        return View(calificacion);
    }
}