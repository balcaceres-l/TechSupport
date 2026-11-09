using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechSupport.MVC.Models;
using TechSupport.MVC.Services;

namespace TechSupport.MVC.Controllers;

public class AccountController : Controller
{
    private readonly ITechSupportApiClient _apiClient;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ITechSupportApiClient apiClient, ILogger<AccountController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Incidentes");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, data, error) = await _apiClient.LoginAsync(model.NombreUsuario, model.Password);

        if (!success || data == null)
        {
            ModelState.AddModelError(string.Empty, error ?? "Usuario o contraseña inválidos.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, data.Id.ToString()),
            new Claim(ClaimTypes.Name, data.NombreUsuario),
            new Claim(ClaimTypes.Role, data.Rol),
            new Claim("access_token", data.Token)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(120)
        };

        authProperties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = data.Token }
        });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("Usuario {Usuario} ({Rol}) ha iniciado sesión correctamente.", data.NombreUsuario, data.Rol);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Incidentes");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Usuario ha cerrado sesión.");
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
