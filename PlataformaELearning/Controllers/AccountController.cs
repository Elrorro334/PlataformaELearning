using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaELearning.Data;
using PlataformaELearning.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace PlataformaELearning.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        // Inyección de dependencias incluyendo ILogger para auditoría
        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult Register()
        {
            // Evitar que usuarios ya autenticados accedan a esta vista
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            try
            {
                // Normalización de datos críticos
                string emailNormalizado = user.Email?.Trim().ToLower() ?? string.Empty;
                string matriculaNormalizada = user.Matricula?.Trim() ?? string.Empty;

                // Uso de AsNoTracking para optimizar consultas de solo lectura
                bool existeUsuario = await _context.Users.AsNoTracking()
                    .AnyAsync(u => u.Email == emailNormalizado || u.Matricula == matriculaNormalizada);

                if (existeUsuario)
                {
                    _logger.LogWarning("Intento de registro duplicado. Correo: {Email}, Matrícula: {Matricula}", emailNormalizado, matriculaNormalizada);
                    ModelState.AddModelError(string.Empty, "El correo electrónico o la matrícula ya están registrados en la plataforma.");
                    return View(user);
                }

                user.Email = emailNormalizado;
                user.Matricula = matriculaNormalizada;
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Nuevo usuario registrado exitosamente. Matrícula: {Matricula}", user.Matricula);

                // Mensaje temporal para confirmar la acción en la vista de Login
                TempData["SuccessMessage"] = "Registro completado exitosamente. Por favor, inicia sesión.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico durante el registro del usuario {Email}", user.Email);
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al procesar tu registro. Intenta más tarde.");
                return View(user);
            }
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "El correo y la contraseña son obligatorios.");
                return View();
            }

            try
            {
                string emailNormalizado = email.Trim().ToLower();

                // AsNoTracking evita cargar la entidad en el ChangeTracker de EF Core
                var user = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == emailNormalizado);

                if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.FirstName ?? "Usuario"),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role ?? "Alumno"),
                        new Claim("Matricula", user.Matricula ?? string.Empty)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var userPrincipal = new ClaimsPrincipal(claimsIdentity);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                        AllowRefresh = true // Renueva la sesión automáticamente si el usuario sigue activo
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, userPrincipal, authProperties);

                    _logger.LogInformation("Inicio de sesión exitoso para {Email}", emailNormalizado);

                    // Prevención de ataques Open Redirect
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return LocalRedirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }

                _logger.LogWarning("Credenciales inválidas ingresadas para {Email}", emailNormalizado);
                ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico durante el inicio de sesión para {Email}", email);
                ModelState.AddModelError(string.Empty, "Error de conexión. Por favor, intenta de nuevo.");
                return View();
            }
        }

        // El Logout debe ser POST para prevenir ataques CSRF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                string? userEmail = User.FindFirstValue(ClaimTypes.Email);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation("Sesión cerrada exitosamente para {Email}", userEmail ?? "Usuario desconocido");

                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al intentar cerrar sesión.");
                return RedirectToAction("Index", "Home");
            }
        }
    }
}