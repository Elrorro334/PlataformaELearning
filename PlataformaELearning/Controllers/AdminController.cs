using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaELearning.Data;
using PlataformaELearning.Models;
using System.Security.Claims;

namespace PlataformaELearning.Controllers
{
    // Bloqueamos el acceso a nivel de clase: Solo Administradores
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 1. LISTAR TODOS LOS USUARIOS
        [HttpGet]
        public async Task<IActionResult> Index(string? rol)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            // Filtro opcional por rol (ej. ?rol=Maestro)
            if (!string.IsNullOrEmpty(rol))
            {
                query = query.Where(u => u.Role == rol);
            }

            var users = await query.ToListAsync();
            ViewData["FiltroActual"] = rol ?? "Todos";

            return View(users);
        }

        // 2. FORMULARIO DE CREACIÓN (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View(new User());
        }

        // 3. GUARDAR NUEVO USUARIO (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            if (!ModelState.IsValid) return View(user);

            try
            {
                string emailNormalizado = user.Email?.Trim().ToLower() ?? string.Empty;
                string matriculaNormalizada = user.Matricula?.Trim() ?? string.Empty;

                bool existeUsuario = await _context.Users.AsNoTracking()
                    .AnyAsync(u => u.Email == emailNormalizado || u.Matricula == matriculaNormalizada);

                if (existeUsuario)
                {
                    ModelState.AddModelError(string.Empty, "El correo electrónico o la matrícula ya están registrados.");
                    return View(user);
                }

                user.Email = emailNormalizado;
                user.Matricula = matriculaNormalizada;

                // Hasheamos la contraseña antes de guardar usando el mismo estándar del AccountController
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

                // Forzamos que la primera letra del rol sea mayúscula por convención
                user.Role = char.ToUpper(user.Role[0]) + user.Role.Substring(1).ToLower();

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Usuario {user.FirstName} registrado correctamente como {user.Role}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario.");
                ModelState.AddModelError(string.Empty, "Error interno al guardar el usuario.");
                return View(user);
            }
        }

        // 4. FORMULARIO DE EDICIÓN (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // 5. GUARDAR EDICIÓN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User userForm)
        {
            if (id != userForm.Id) return BadRequest();

            // Removemos la validación de la contraseña porque en edición podría venir vacía si no se quiere cambiar
            ModelState.Remove("Password");

            if (!ModelState.IsValid) return View(userForm);

            try
            {
                var userDb = await _context.Users.FindAsync(id);
                if (userDb == null) return NotFound();

                // Verificar que no intente usar un correo o matrícula que ya es de otro
                string emailNormalizado = userForm.Email?.Trim().ToLower() ?? string.Empty;
                string matriculaNormalizada = userForm.Matricula?.Trim() ?? string.Empty;

                bool duplicado = await _context.Users.AsNoTracking()
                    .AnyAsync(u => u.Id != id && (u.Email == emailNormalizado || u.Matricula == matriculaNormalizada));

                if (duplicado)
                {
                    ModelState.AddModelError(string.Empty, "El correo o matrícula pertenece a otro usuario.");
                    return View(userForm);
                }

                userDb.FirstName = userForm.FirstName;
                userDb.LastName = userForm.LastName;
                userDb.Email = emailNormalizado;
                userDb.Telephone = userForm.Telephone;
                userDb.Matricula = matriculaNormalizada;
                userDb.Role = char.ToUpper(userForm.Role[0]) + userForm.Role.Substring(1).ToLower();

                // Si el administrador escribió una nueva contraseña, la hasheamos y la actualizamos
                if (!string.IsNullOrWhiteSpace(userForm.Password))
                {
                    userDb.Password = BCrypt.Net.BCrypt.HashPassword(userForm.Password);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Datos de usuario actualizados correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar usuario {Id}", id);
                ModelState.AddModelError(string.Empty, "Error al actualizar los datos.");
                return View(userForm);
            }
        }

        // 6. ELIMINAR USUARIO (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "El usuario no existe.";
                    return RedirectToAction(nameof(Index));
                }

                // Prevención: Evitar que el administrador actual se elimine a sí mismo
                string? currentEmail = User.FindFirstValue(ClaimTypes.Email);
                if (user.Email == currentEmail)
                {
                    TempData["ErrorMessage"] = "No puedes eliminar tu propia cuenta mientras estás en sesión.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Usuario {user.FirstName} eliminado correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {Id}", id);
                TempData["ErrorMessage"] = "Error de base de datos al intentar eliminar el usuario.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}