using System.ComponentModel.DataAnnotations;

namespace Boardly.Backend.Models.Requests;

/// <summary>
/// Represents a request to sign up a new user.
/// </summary>
public class SignUpRequest
{
    /// <summary>
    /// Nickname of the user.
    /// The nickname must be between 3 and 50 characters.
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Nickname must be between 3 and 50 characters.")]
    public string Nickname { get; init; } = null!;

    /// <summary>
    /// Password of the user.
    /// The password must be at least 8 characters long.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; init; } = null!;
}
