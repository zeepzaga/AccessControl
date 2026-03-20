using System.ComponentModel.DataAnnotations;

namespace AccessControl.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Укажи email.")]
    [EmailAddress(ErrorMessage = "Нужен корректный email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажи пароль.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}
