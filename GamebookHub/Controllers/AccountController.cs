using System.Collections.Generic;
using System.Security.Claims;
using GamebookHub.Models;
using GamebookHub.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamebookHub.Controllers;

public class AccountController : Controller
{
    private const string DemoEmail = "admin@demo.local";
    private const string DemoPassword = "123";
    private const string AuthScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    private readonly DemoUserStore _userStore;

    public AccountController(DemoUserStore userStore)
    {
        _userStore = userStore;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var claims = await BuildClaimsAsync(model.Email, model.Password);
        if (claims == null)
        {
            ModelState.AddModelError(string.Empty, "Credenciais inválidas. Verifique o e-mail e a senha.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var identity = new ClaimsIdentity(claims, AuthScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(AuthScheme, principal, new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            AllowRefresh = true
        });

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult CreateUser()
    {
        ViewBag.Message = TempData["UserCreated"] as string;
        return View(new RegisterUserViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(RegisterUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _userStore.ExistsAsync(model.Email))
        {
            ModelState.AddModelError(nameof(RegisterUserViewModel.Email), "Já existe um usuário com esse e-mail.");
            return View(model);
        }

        try
        {
            await _userStore.AddAsync(model.Email, model.Password);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }

        TempData["UserCreated"] = $"Usuário {model.Email} criado com sucesso.";
        return RedirectToAction(nameof(CreateUser));
    }

    private async Task<List<Claim>?> BuildClaimsAsync(string email, string password)
    {
        if (string.Equals(email?.Trim(), DemoEmail, StringComparison.OrdinalIgnoreCase)
            && string.Equals(password, DemoPassword, StringComparison.Ordinal))
        {
            return new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, DemoEmail),
                new(ClaimTypes.Name, DemoEmail),
                new(ClaimTypes.Email, DemoEmail),
                new(ClaimTypes.Role, "Admin")
            };
        }

        var stored = await _userStore.ValidateAsync(email, password);
        if (stored == null)
        {
            return null;
        }

        return new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, stored.Email),
            new(ClaimTypes.Name, stored.Email),
            new(ClaimTypes.Email, stored.Email),
            new(ClaimTypes.Role, stored.IsAdmin ? "Admin" : "User")
        };
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
