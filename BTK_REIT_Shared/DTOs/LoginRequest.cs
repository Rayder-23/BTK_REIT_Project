using System.ComponentModel.DataAnnotations;

namespace BTK_REIT_Shared.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Username is required.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}
