namespace StreamlineTax.Api.Models;

public record RegisterRequest(string Email, string Password, string Name);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record EmailRequest(string Email);
public record ConfirmEmailRequest(string Email, string Token);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record UpdateProfileRequest(string Name);
