namespace Boardly.Backend.Models.Responses;

/// <summary>
/// Response model for user auth.
/// </summary>
/// <param name="AccessToken">The access token issued to the user.</param>
/// <param name="AccessTokenExpiresIn">The duration in seconds until the access token expires.</param>
/// <param name="RefreshToken">The refresh token issued to the user for obtaining new access tokens.</param>
/// <param name="RefreshTokenExpiresIn">The duration in seconds until the refresh token expires.</param>
public record AuthResponse(string AccessToken, uint AccessTokenExpiresIn, string RefreshToken, uint RefreshTokenExpiresIn);
