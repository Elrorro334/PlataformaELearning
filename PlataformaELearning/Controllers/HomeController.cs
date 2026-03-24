using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaELearning.Data;
using PlataformaELearning.Models;
using PlataformaELearning.Models.ViewModels;
using System.Security.Claims;

namespace PlataformaELearning.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);

            var rol = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "Alumno";

            var model = new PortalDashboardViewModel
            {
                CicloEscolarActual = "Ciclo Escolar 2026",
                AvisoTitulo = "Actualización de Plataforma",
                AvisoMensaje = "El sistema de e-learning ha sido optimizado para mejorar la escalabilidad y soporte.",
                UptimeSistema = 99.9m
            };

            if (rol == "Alumno")
            {
                var cursosAlumno = await _context.Set<Inscripcion>()
                    .Where(i => i.AlumnoId == userId)
                    .Select(i => i.CursoId)
                    .ToListAsync();

                var totalTareas = await _context.Set<TareaApartado>()
                    .Include(t => t.Apartado)
                    .Where(t => t.Apartado != null && cursosAlumno.Contains(t.Apartado.CursoId))
                    .CountAsync();

                var tareasEntregadas = await _context.Set<EntregaTarea>()
                    .Where(e => e.AlumnoId == userId)
                    .CountAsync();

                if (totalTareas > 0)
                {
                    model.AvanceCuatrimestre = (int)Math.Round((double)tareasEntregadas / totalTareas * 100);
                }
                else
                {
                    model.AvanceCuatrimestre = 0;
                }
            }
            else if (rol == "Maestro")
            {
                var cursosMaestro = await _context.Set<Curso>()
                    .Where(c => c.MaestroId == userId)
                    .Select(c => c.Id)
                    .ToListAsync();

                model.TotalGruposAsignados = cursosMaestro.Count;

                model.TotalAlumnosAsignados = await _context.Set<Inscripcion>()
                    .Where(i => cursosMaestro.Contains(i.CursoId))
                    .Select(i => i.AlumnoId)
                    .Distinct()
                    .CountAsync();

                model.TareasPendientes = await _context.Set<EntregaTarea>()
                    .Include(e => e.Tarea)
                    .ThenInclude(t => t.Apartado)
                    .Where(e => e.Tarea != null &&
                                e.Tarea.Apartado != null &&
                                cursosMaestro.Contains(e.Tarea.Apartado.CursoId) &&
                                e.CalificacionId == null)
                    .CountAsync();
            }
            else if (rol == "Administrador")
            {
                model.TotalAlumnos = await _context.Set<User>().CountAsync(u => u.Role == "Alumno");
                model.TotalDocentes = await _context.Set<User>().CountAsync(u => u.Role == "Maestro");
            }

            return View(model);
        }
    }
}