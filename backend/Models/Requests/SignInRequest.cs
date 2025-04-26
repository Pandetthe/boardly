using System.ComponentModel.DataAnnotations;

namespace Boardly.Backend.Models.Requests;

/// <summary>
/// Request model for user sign-in.
/// </summary>
public class SignInRequest
{
    /// <summary>
    /// Nickname of the user.
    /// This field is required and must be between 3 and 50 characters.
    /// </summary>
    [Required(ErrorMessage = "Nickname is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Nickname must be between 3 and 50 characters.")]
    public string Nickname { get; init; } = null!;

    /// <summary>
    /// Password of the user.
    /// This field is required and must be at least 8 characters long.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; init; } = null!;
}
