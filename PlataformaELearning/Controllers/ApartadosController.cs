using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using PlataformaELearning.Data;
using PlataformaELearning.Models;
using System.Security.Claims;

namespace PlataformaELearning.Controllers
{
    [Authorize(Roles = "Administrador,Maestro")]
    public class ApartadosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApartadosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== APARTADOS ====================

        // Ver todos los apartados de un curso
        public async Task<IActionResult> Index(int cursoId)
        {
            var curso = await _context.Cursos
                .Include(c => c.Apartados!.OrderBy(a => a.Orden))
                .FirstOrDefaultAsync(c => c.Id == cursoId);

            if (curso == null) return NotFound();

            ViewBag.Curso = curso;
            return View(curso.Apartados?.ToList() ?? new List<ApartadoCurso>());
        }

        // Crear nuevo apartado (GET)
        public IActionResult Create(int cursoId)
        {
            var curso = _context.Cursos.Find(cursoId);
            if (curso == null) return NotFound();

            ViewBag.Curso = curso;
            return View(new ApartadoCurso { CursoId = cursoId });
        }

        // Crear nuevo apartado (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApartadoCurso apartado)
        {
            // Asignar orden automático
            var ultimoOrden = await _context.ApartadosCursos
                .Where(a => a.CursoId == apartado.CursoId)
                .MaxAsync(a => (int?)a.Orden) ?? 0;

            apartado.Orden = ultimoOrden + 1;
            apartado.FechaCreacion = DateTime.Now;

            if (ModelState.IsValid)
            {
                _context.Add(apartado);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Apartado creado correctamente.";
                return RedirectToAction(nameof(Index), new { cursoId = apartado.CursoId });
            }

            var curso = await _context.Cursos.FindAsync(apartado.CursoId);
            ViewBag.Curso = curso;
            return View(apartado);
        }

        // Editar apartado (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var apartado = await _context.ApartadosCursos
                .Include(a => a.Curso)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (apartado == null) return NotFound();

            return View(apartado);
        }

        // Editar apartado (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ApartadoCurso apartado)
        {
            if (id != apartado.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(apartado);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Apartado actualizado correctamente.";
                    return RedirectToAction(nameof(Index), new { cursoId = apartado.CursoId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ApartadosCursos.Any(a => a.Id == id))
                        return NotFound();
                    else
                        throw;
                }
            }
            return View(apartado);
        }

        // Eliminar apartado (GET)
        public async Task<IActionResult> Delete(int id)
        {
            var apartado = await _context.ApartadosCursos
                .Include(a => a.Curso)
                .Include(a => a.Tareas)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (apartado == null) return NotFound();

            return View(apartado);
        }

        // Eliminar apartado (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var apartado = await _context.ApartadosCursos.FindAsync(id);
            if (apartado != null)
            {
                _context.ApartadosCursos.Remove(apartado);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Apartado eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index), new { cursoId = apartado?.CursoId });
        }

        // ==================== TAREAS ====================

        // Ver tareas de un apartado
        public async Task<IActionResult> Tareas(int apartadoId)
        {
            var apartado = await _context.ApartadosCursos
                .Include(a => a.Curso)
                .Include(a => a.Tareas!)
                .ThenInclude(t => t.Materiales)
                .FirstOrDefaultAsync(a => a.Id == apartadoId);

            if (apartado == null) return NotFound();

            return View(apartado);
        }

        // Crear nueva tarea (GET)
        public async Task<IActionResult> CreateTarea(int apartadoId)
        {
            var apartado = await _context.ApartadosCursos
                .Include(a => a.Curso)
                .FirstOrDefaultAsync(a => a.Id == apartadoId);

            if (apartado == null) return NotFound();

            ViewBag.Apartado = apartado;
            return View(new TareaApartado { ApartadoId = apartadoId });
        }

        // Crear nueva tarea (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTarea(TareaApartado tarea)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tarea);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tarea creada correctamente.";
                return RedirectToAction(nameof(Tareas), new { apartadoId = tarea.ApartadoId });
            }

            var apartado = await _context.ApartadosCursos.FindAsync(tarea.ApartadoId);
            ViewBag.Apartado = apartado;
            return View(tarea);
        }

        // Ver detalles de una tarea
        public async Task<IActionResult> DetallesTarea(int id)
        {
            var tarea = await _context.TareasApartados
                .Include(t => t.Apartado)
                .ThenInclude(a => a!.Curso)
                .Include(t => t.Materiales)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tarea == null) return NotFound();

            // Verificar si está próxima a vencer
            ViewBag.ProximaAVencer = (tarea.FechaLimite - DateTime.Now).TotalHours < 24;

            return View(tarea);
        }

        // Editar tarea (GET)
        public async Task<IActionResult> EditTarea(int id)
        {
            var tarea = await _context.TareasApartados
                .Include(t => t.Apartado)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tarea == null) return NotFound();

            return View(tarea);
        }

        // Editar tarea (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTarea(int id, TareaApartado tarea)
        {
            if (id != tarea.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tarea);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Tarea actualizada correctamente.";
                    return RedirectToAction(nameof(DetallesTarea), new { id = tarea.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TareasApartados.Any(t => t.Id == id))
                        return NotFound();
                    else
                        throw;
                }
            }
            return View(tarea);
        }

        // Eliminar tarea (GET)
        public async Task<IActionResult> DeleteTarea(int id)
        {
            var tarea = await _context.TareasApartados
                .Include(t => t.Apartado)
                .ThenInclude(a => a!.Curso)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tarea == null) return NotFound();

            return View(tarea);
        }

        // Eliminar tarea (POST)
        [HttpPost, ActionName("DeleteTarea")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTareaConfirmed(int id)
        {
            var tarea = await _context.TareasApartados.FindAsync(id);
            if (tarea != null)
            {
                _context.TareasApartados.Remove(tarea);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tarea eliminada correctamente.";
            }
            return RedirectToAction(nameof(Tareas), new { apartadoId = tarea?.ApartadoId });
        }

        // ==================== MATERIALES ====================

        // Agregar material a tarea (GET)
        public async Task<IActionResult> AgregarMaterial(int tareaId)
        {
            var tarea = await _context.TareasApartados
                .Include(t => t.Apartado)
                .FirstOrDefaultAsync(t => t.Id == tareaId);

            if (tarea == null) return NotFound();

            ViewBag.Tarea = tarea;
            ViewBag.Tipos = new SelectList(Enum.GetValues(typeof(TipoMaterial)).Cast<TipoMaterial>());
            return View(new MaterialTarea { TareaId = tareaId });
        }

        // Agregar material a tarea (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(104857600)] // 100MB para PDFs
        public async Task<IActionResult> AgregarMaterial(MaterialTarea material, IFormFile? archivoPDF)
        {
            if (ModelState.IsValid)
            {
                // Guardar primero para obtener ID
                _context.Add(material);
                await _context.SaveChangesAsync();

                // Si es PDF y hay archivo, guardar con ADO.NET (igual que en CursosController)
                if (material.Tipo == TipoMaterial.PDF && archivoPDF != null && archivoPDF.Length > 0)
                {
                    byte[] fileBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        await archivoPDF.CopyToAsync(memoryStream);
                        fileBytes = memoryStream.ToArray();
                    }

                    var connectionString = _context.Database.GetConnectionString();
                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        string sql = "UPDATE MaterialesTarea SET ArchivoFisico = @Pdf, ContentType = @Type, NombreArchivo = @Name WHERE Id = @Id";
                        using (var command = new SqlCommand(sql, connection))
                        {
                            command.Parameters.Add("@Pdf", System.Data.SqlDbType.VarBinary, -1).Value = fileBytes;
                            command.Parameters.AddWithValue("@Type", archivoPDF.ContentType);
                            command.Parameters.AddWithValue("@Name", archivoPDF.FileName);
                            command.Parameters.AddWithValue("@Id", material.Id);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                TempData["SuccessMessage"] = "Material agregado correctamente.";
                return RedirectToAction(nameof(DetallesTarea), new { id = material.TareaId });
            }

            var tarea = await _context.TareasApartados.FindAsync(material.TareaId);
            ViewBag.Tarea = tarea;
            ViewBag.Tipos = new SelectList(Enum.GetValues(typeof(TipoMaterial)).Cast<TipoMaterial>());
            return View(material);
        }

        // Descargar material PDF
        [HttpGet]
        public async Task<IActionResult> DescargarMaterial(int id)
        {
            var material = await _context.MaterialesTarea
                .Where(m => m.Id == id && m.Tipo == TipoMaterial.PDF)
                .Select(m => new { m.ArchivoFisico, m.ContentType, m.NombreArchivo, m.Titulo })
                .FirstOrDefaultAsync();

            if (material?.ArchivoFisico == null) return NotFound();

            return File(material.ArchivoFisico, material.ContentType ?? "application/pdf",
                material.NombreArchivo ?? $"material_{id}.pdf");
        }

        // Eliminar material
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarMaterial(int id)
        {
            var material = await _context.MaterialesTarea.FindAsync(id);
            if (material != null)
            {
                _context.MaterialesTarea.Remove(material);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Material eliminado correctamente.";
            }
            return RedirectToAction(nameof(DetallesTarea), new { id = material?.TareaId });
        }
    }
}