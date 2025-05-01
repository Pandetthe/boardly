using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

/// <summary>
/// Request model for user password update.
/// </summary>
public class UpdatePasswordRequest
{
    /// <summary>
    /// Password of the user.
    /// This field is required and must be at least 8 characters long.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; init; } = null!;
}
