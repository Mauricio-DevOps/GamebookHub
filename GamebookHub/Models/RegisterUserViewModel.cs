using System.ComponentModel.DataAnnotations;

namespace GamebookHub.Models;

public class RegisterUserViewModel
{
    [Required(ErrorMessage = "Informe o e-mail")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha")]
    [DataType(DataType.Password)]
    [MinLength(3, ErrorMessage = "Use pelo menos 3 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "As senhas não conferem")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
