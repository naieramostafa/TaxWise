using StreamlineTax.Application.Common.Interfaces;

namespace StreamlineTax.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string name);
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RefreshAsync(string refreshToken);
    Task LogoutAsync(string userId);
    Task SendVerificationEmailAsync(string email);
    Task<string?> ConfirmEmailAsync(string email, string token);
    Task SendPasswordResetEmailAsync(string email);
    Task<string?> ResetPasswordAsync(string email, string token, string newPassword);
    Task<(bool Success, string? Error, string? Name)> UpdateProfileAsync(string userId, string? name = null);
}

public record AuthResult(bool Success, string? Token, string? RefreshToken, string? UserId, string? Error, string? Email = null, string? Name = null);