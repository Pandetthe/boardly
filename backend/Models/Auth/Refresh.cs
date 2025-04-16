namespace Boardly.Backend.Models.Auth;

public record Refresh(string RefreshToken);


public record RefreshResponse(string AccessToken, int ExpiresIn, string RefreshToken);