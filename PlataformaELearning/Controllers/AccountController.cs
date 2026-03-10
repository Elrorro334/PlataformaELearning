using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaELearning.Data;
using PlataformaELearning.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.Net;
using System.Net.Mail;

namespace PlataformaELearning.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IDataProtector _protector;
        private readonly IConfiguration _config;

        public AccountController(
            ApplicationDbContext context,
            ILogger<AccountController> logger,
            IDataProtectionProvider dataProtectionProvider,
            IConfiguration config)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _protector = dataProtectionProvider.CreateProtector("EduNix_PasswordReset");
        }

        [HttpGet]
        public IActionResult Register()
        {
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
                string emailNormalizado = user.Email?.Trim().ToLower() ?? string.Empty;
                string matriculaNormalizada = user.Matricula?.Trim() ?? string.Empty;

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
                        AllowRefresh = true
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, userPrincipal, authProperties);

                    _logger.LogInformation("Inicio de sesión exitoso para {Email}", emailNormalizado);

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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Ingresa tu correo institucional.");
                return View();
            }

            try
            {
                string emailNormalizado = email.Trim().ToLower();
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == emailNormalizado);

                if (user != null)
                {
                    string expiracion = DateTime.UtcNow.AddHours(1).Ticks.ToString();
                    string tokenPayload = $"{user.Email}|{expiracion}";

                    string token = Uri.EscapeDataString(_protector.Protect(tokenPayload));
                    string? resetLink = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);

                    await EnviarCorreoRecuperacionAsync(user.Email, user.FirstName ?? "Universitario", resetLink);

                    _logger.LogInformation("Correo de recuperación enviado a {Email}", user.Email);
                }

                TempData["SuccessMessage"] = "Si el correo existe en nuestro sistema, recibirás un enlace para restablecer tu contraseña.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al procesar solicitud de recuperación para {Email}", email);
                ModelState.AddModelError(string.Empty, "Hubo un problema al procesar tu solicitud. Intenta más tarde.");
                return View();
            }
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "El enlace de recuperación es inválido o ha expirado.";
                return RedirectToAction(nameof(Login));
            }

            ViewData["Token"] = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                ModelState.AddModelError(string.Empty, "La contraseña debe tener al menos 8 caracteres.");
                ViewData["Token"] = token;
                return View();
            }

            try
            {
                string unescapedToken = Uri.UnescapeDataString(token);
                string tokenPayload = _protector.Unprotect(unescapedToken);
                var partes = tokenPayload.Split('|');

                string email = partes[0];
                long expiracionTicks = long.Parse(partes[1]);

                if (DateTime.UtcNow.Ticks > expiracionTicks)
                {
                    TempData["ErrorMessage"] = "El enlace ha expirado. Solicita uno nuevo.";
                    return RedirectToAction(nameof(ForgotPassword));
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Contraseña restablecida exitosamente para {Email}", email);
                    TempData["SuccessMessage"] = "Tu contraseña ha sido actualizada. Ya puedes iniciar sesión.";
                }

                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Intento de restablecimiento fallido o token manipulado.");
                TempData["ErrorMessage"] = "El token es inválido o ha expirado.";
                return RedirectToAction(nameof(ForgotPassword));
            }
        }

        private async Task EnviarCorreoRecuperacionAsync(string destinatario, string nombre, string? enlace)
        {
            string host = _config["SmtpSettings:Server"] ?? throw new InvalidOperationException("SMTP Server no configurado.");
            int puerto = _config.GetValue<int>("SmtpSettings:Port");
            string remitente = _config["SmtpSettings:SenderEmail"] ?? throw new InvalidOperationException("SMTP SenderEmail no configurado.");
            string remitenteNombre = _config["SmtpSettings:SenderName"] ?? "Plataforma e-Learning";
            string passwordApp = _config["SmtpSettings:Password"] ?? throw new InvalidOperationException("SMTP Password no configurado.");

            var mail = new MailMessage
            {
                From = new MailAddress(remitente, remitenteNombre),
                Subject = "Recuperación de Contraseña - Plataforma Académica",
                IsBodyHtml = true,
                Body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e5e7eb; border-radius: 10px;'>
                        <h2 style='color: #16a34a;'>Recuperación de Acceso</h2>
                        <p>Hola <strong>{nombre}</strong>,</p>
                        <p>Hemos recibido una solicitud para restablecer tu contraseña en el portal universitario. Este enlace será válido por 1 hora.</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{enlace}' style='background-color: #16a34a; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Restablecer mi Contraseña</a>
                        </div>
                        <p style='color: #6b7280; font-size: 12px;'>Si no solicitaste este cambio, puedes ignorar este correo de forma segura.</p>
                    </div>"
            };
            mail.To.Add(destinatario);

            using var smtp = new SmtpClient(host, puerto)
            {
                Credentials = new NetworkCredential(remitente, passwordApp),
                EnableSsl = true
            };

            await smtp.SendMailAsync(mail);
        }
    }
}