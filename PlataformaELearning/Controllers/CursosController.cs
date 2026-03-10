using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaELearning.Data;
using PlataformaELearning.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PlataformaELearning.Controllers
{
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CursosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cursos
        public async Task<IActionResult> Index()
        {
            var cursos = await _context.Cursos
                .Include(c => c.Maestro)
                .Include(c => c.Contenidos)
                .ToListAsync();
            return View(cursos);
        }

        // GET: Cursos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var curso = await _context.Cursos
                .Include(c => c.Maestro)
                .Include(c => c.Contenidos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (curso == null)
            {
                return NotFound();
            }

            return View(curso);
        }

        // GET: Cursos/Create
        public IActionResult Create()
        {
            ViewData["MaestroId"] = new SelectList(_context.Users, "Id", "Email"); // Ajusta según tu modelo User
            return View();
        }

        // POST: Cursos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Descripcion,MaestroId")] Curso curso)
        {
            if (ModelState.IsValid)
            {
                _context.Add(curso);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaestroId"] = new SelectList(_context.Users, "Id", "Email", curso.MaestroId);
            return View(curso);
        }

        // GET: Cursos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null)
            {
                return NotFound();
            }
            ViewData["MaestroId"] = new SelectList(_context.Users, "Id", "Email", curso.MaestroId);
            return View(curso);
        }

        // POST: Cursos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,MaestroId")] Curso curso)
        {
            if (id != curso.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(curso);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CursoExists(curso.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaestroId"] = new SelectList(_context.Users, "Id", "Email", curso.MaestroId);
            return View(curso);
        }

        // GET: Cursos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var curso = await _context.Cursos
                .Include(c => c.Maestro)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (curso == null)
            {
                return NotFound();
            }

            return View(curso);
        }

        // POST: Cursos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso != null)
            {
                _context.Cursos.Remove(curso);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CursoExists(int id)
        {
            return _context.Cursos.Any(e => e.Id == id);
        }

        // ============ MÉTODOS PARA CONTENIDO DEL CURSO ============

        // GET: Cursos/AgregarContenido/5
        public async Task<IActionResult> AgregarContenido(int? cursoId)
        {
            if (cursoId == null)
            {
                return NotFound();
            }

            var curso = await _context.Cursos.FindAsync(cursoId);
            if (curso == null)
            {
                return NotFound();
            }

            ViewBag.CursoId = cursoId;
            ViewBag.CursoNombre = curso.Nombre;
            ViewBag.Tipos = new SelectList(Enum.GetValues(typeof(TipoContenido)).Cast<TipoContenido>().Select(v => new SelectListItem
            {
                Text = v.ToString(),
                Value = ((int)v).ToString()
            }), "Value", "Text");

            return View();
        }

        // POST: Cursos/AgregarContenido
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarContenido([Bind("CursoId,Titulo,Tipo,ContenidoTexto,UrlVideo")] ContenidoCurso contenido, IFormFile? archivoPDF)
        {
            if (ModelState.IsValid)
            {
                contenido.FechaPublicacion = DateTime.Now;

                // Si es un PDF y se subió un archivo
                if (contenido.Tipo == TipoContenido.PDF && archivoPDF != null)
                {
                    // Crear carpeta si no existe
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/pdfs");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Generar nombre único para el archivo
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + archivoPDF.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Guardar el archivo
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await archivoPDF.CopyToAsync(fileStream);
                    }

                    contenido.RutaArchivo = "/uploads/pdfs/" + uniqueFileName;
                    contenido.NombreArchivo = archivoPDF.FileName;
                }

                _context.Add(contenido);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = contenido.CursoId });
            }

            ViewBag.CursoId = contenido.CursoId;
            ViewBag.Tipos = new SelectList(Enum.GetValues(typeof(TipoContenido)).Cast<TipoContenido>().Select(v => new SelectListItem
            {
                Text = v.ToString(),
                Value = ((int)v).ToString()
            }), "Value", "Text");

            return View(contenido);
        }

        // GET: Cursos/EliminarContenido/5
        public async Task<IActionResult> EliminarContenido(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contenido = await _context.ContenidosCursos
                .Include(c => c.Curso)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contenido == null)
            {
                return NotFound();
            }

            return View(contenido);
        }

        // POST: Cursos/EliminarContenido/5
        [HttpPost, ActionName("EliminarContenido")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarContenidoConfirmed(int id)
        {
            var contenido = await _context.ContenidosCursos.FindAsync(id);
            if (contenido != null)
            {
                // Si es un PDF, eliminar el archivo físico
                if (contenido.Tipo == TipoContenido.PDF && !string.IsNullOrEmpty(contenido.RutaArchivo))
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + contenido.RutaArchivo);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.ContenidosCursos.Remove(contenido);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = contenido?.CursoId });
        }
    }
}