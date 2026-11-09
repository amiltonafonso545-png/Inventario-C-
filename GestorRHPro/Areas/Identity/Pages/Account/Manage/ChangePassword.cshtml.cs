using System.ComponentModel.DataAnnotations;
using GestorRHPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestorRHPro.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class ChangePasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<ChangePasswordModel> _logger;

    public ChangePasswordModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<ChangePasswordModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!await _userManager.HasPasswordAsync(user)) return RedirectToPage("./SetPassword");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!await _userManager.HasPasswordAsync(user)) return RedirectToPage("./SetPassword");
        if (!ModelState.IsValid) return Page();

        if (string.Equals(Input.CurrentPassword, Input.NewPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError("Input.NewPassword", "A nova palavra-passe tem de ser diferente da atual.");
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, TranslateIdentityError(error.Code));
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        _logger.LogInformation("User changed their password.");
        StatusMessage = "A palavra-passe foi alterada com sucesso.";
        return RedirectToPage();
    }

    private static string TranslateIdentityError(string code) => code switch
    {
        "PasswordMismatch" => "A palavra-passe atual está incorreta.",
        "PasswordTooShort" => "A nova palavra-passe tem de ter pelo menos 12 caracteres.",
        "PasswordRequiresDigit" => "Inclua pelo menos um número.",
        "PasswordRequiresLower" => "Inclua pelo menos uma letra minúscula.",
        "PasswordRequiresUpper" => "Inclua pelo menos uma letra maiúscula.",
        "PasswordRequiresNonAlphanumeric" => "Inclua pelo menos um símbolo.",
        _ => "Não foi possível alterar a palavra-passe. Reveja os requisitos e tente novamente."
    };

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Indique a palavra-passe atual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Palavra-passe atual")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Indique a nova palavra-passe.")]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "Use entre 12 e 100 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova palavra-passe")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a nova palavra-passe.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "As palavras-passe não coincidem.")]
        [Display(Name = "Confirmar nova palavra-passe")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
