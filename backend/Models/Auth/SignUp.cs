using System.ComponentModel.DataAnnotations;

namespace Boardly.Backend.Models.Auth
{
    public record SignUpRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Nickname must be between 3 and 50 characters.")]
        public string Nickname { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = null!;
    }

    public record SignUpResponse(string AccessToken, int ExpiresIn, string RefreshToken);
}
