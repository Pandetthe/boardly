namespace Boardly.Api.Models.Requests;

/// <summary>
/// Represents a request to refresh an authentication token.
/// </summary>
/// <param name="RefreshToken">The refresh token used to obtain a new access token.</param>
public record RefreshRequest(string RefreshToken);
