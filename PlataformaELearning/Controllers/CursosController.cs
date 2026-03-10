using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using PlataformaELearning.Data;
using PlataformaELearning.Models;

namespace PlataformaELearning.Controllers
{
    [Authorize(Roles = "Administrador,Maestro,Alumno")]
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CursosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Método auxiliar para cargar SOLO Maestros reales en los selectores
        private void CargarMaestrosViewBag(int? maestroSeleccionado = null)
        {
            var maestros = _context.Users
                .Where(u => u.Role == "Maestro")
                .Select(u => new { u.Id, NombreCompleto = $"{u.FirstName} {u.LastName} ({u.Matricula})" })
                .ToList();

            ViewData["MaestroId"] = new SelectList(maestros, "Id", "NombreCompleto", maestroSeleccionado);
        }

        public async Task<IActionResult> Index()
        {
            var cursos = await _context.Cursos
                .Include(c => c.Maestro)
                .Include(c => c.Contenidos)
                .ToListAsync();
            return View(cursos);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var curso = await _context.Cursos
                .Include(c => c.Maestro)
                .Include(c => c.Contenidos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (curso == null) return NotFound();

            return View(curso);
        }

        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            CargarMaestrosViewBag();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Descripcion,MaestroId")] Curso curso)
        {
            if (ModelState.IsValid)
            {
                _context.Add(curso);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            CargarMaestrosViewBag(curso.MaestroId);
            return View(curso);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null) return NotFound();

            CargarMaestrosViewBag(curso.MaestroId);
            return View(curso);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,MaestroId")] Curso curso)
        {
            if (id != curso.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(curso);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Cursos.Any(e => e.Id == curso.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            CargarMaestrosViewBag(curso.MaestroId);
            return View(curso);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var curso = await _context.Cursos
                .Include(c => c.Maestro)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (curso == null) return NotFound();

            return View(curso);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso != null) _context.Cursos.Remove(curso);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ============ MÉTODOS PARA CONTENIDO DEL CURSO ============

        public async Task<IActionResult> AgregarContenido(int? cursoId)
        {
            if (cursoId == null) return NotFound();

            var curso = await _context.Cursos.FindAsync(cursoId);
            if (curso == null) return NotFound();

            ViewBag.CursoId = cursoId;
            ViewBag.CursoNombre = curso.Nombre;
            ViewBag.Tipos = new SelectList(Enum.GetValues(typeof(TipoContenido)).Cast<TipoContenido>().Select(v => new SelectListItem
            {
                Text = v.ToString(),
                Value = ((int)v).ToString()
            }), "Value", "Text");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(104857600)] // Permite subir PDFs de hasta 100MB
        public async Task<IActionResult> AgregarContenido([Bind("CursoId,Titulo,Tipo,ContenidoTexto,UrlVideo")] ContenidoCurso contenido, IFormFile? archivoPDF)
        {
            if (ModelState.IsValid)
            {
                contenido.FechaPublicacion = DateTime.Now;

                // Primero guardamos los datos básicos para que Entity Framework nos genere el ID
                _context.Add(contenido);
                await _context.SaveChangesAsync();

                // Si es un PDF, inyectamos el binario a la BD usando ADO.NET para proteger la memoria RAM
                if (contenido.Tipo == TipoContenido.PDF && archivoPDF != null && archivoPDF.Length > 0)
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
                        string sql = "UPDATE ContenidosCursos SET ArchivoFisico = @Pdf, ContentType = @Type, NombreArchivo = @Name WHERE Id = @Id";
                        using (var command = new SqlCommand(sql, connection))
                        {
                            command.Parameters.Add("@Pdf", System.Data.SqlDbType.VarBinary, -1).Value = fileBytes;
                            command.Parameters.AddWithValue("@Type", archivoPDF.ContentType);
                            command.Parameters.AddWithValue("@Name", archivoPDF.FileName);
                            command.Parameters.AddWithValue("@Id", contenido.Id);

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                return RedirectToAction(nameof(Details), new { id = contenido.CursoId });
            }

            ViewBag.CursoId = contenido.CursoId;
            ViewBag.Tipos = new SelectList(Enum.GetValues(typeof(TipoContenido)).Cast<TipoContenido>().Select(v => new SelectListItem { Text = v.ToString(), Value = ((int)v).ToString() }), "Value", "Text");
            return View(contenido);
        }

        // NUEVO: Endpoint optimizado para descargar el PDF de la Base de Datos
        [HttpGet]
        public async Task<IActionResult> DescargarPDF(int id)
        {
            var contenido = await _context.ContenidosCursos
                .Where(c => c.Id == id && c.Tipo == TipoContenido.PDF)
                .Select(c => new { c.ArchivoFisico, c.ContentType, c.NombreArchivo })
                .FirstOrDefaultAsync();

            if (contenido?.ArchivoFisico == null) return NotFound();

            return File(contenido.ArchivoFisico, contenido.ContentType ?? "application/pdf", contenido.NombreArchivo);
        }

        public async Task<IActionResult> EliminarContenido(int? id)
        {
            if (id == null) return NotFound();

            var contenido = await _context.ContenidosCursos
                .Include(c => c.Curso)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contenido == null) return NotFound();

            return View(contenido);
        }

        [HttpPost, ActionName("EliminarContenido")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarContenidoConfirmed(int id)
        {
            var contenido = await _context.ContenidosCursos.FindAsync(id);
            if (contenido != null)
            {
                _context.ContenidosCursos.Remove(contenido);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = contenido?.CursoId });
        }
    }
}