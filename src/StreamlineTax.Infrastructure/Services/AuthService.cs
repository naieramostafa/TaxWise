using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StreamlineTax.Application.Common.Interfaces;
using StreamlineTax.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace StreamlineTax.Infrastructure.Services;

public class AuthService(
    UserManager<AppUser> userManager,
    IApplicationDbContext context,
    IConfiguration configuration,
    IEmailService emailService,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(string email, string password, string name)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return new AuthResult(false, null, null, null, "Email already registered");

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            Name = name
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return new AuthResult(false, null, null, null, string.Join(", ", result.Errors.Select(e => e.Description)));

        try { await SendVerificationEmailAsync(email); }
        catch (Exception ex) { logger.LogWarning(ex, "Email verification send failed for {Email}; user can resend later", email); }

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await IssueRefreshTokenAsync(user);

        return new AuthResult(true, accessToken, refreshToken, user.Id.ToString(), null, user.Email, user.Name);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
            return new AuthResult(false, null, null, null, "Invalid email or password");

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await IssueRefreshTokenAsync(user);

        return new AuthResult(true, accessToken, refreshToken, user.Id.ToString(), null, user.Email, user.Name);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        var stored = await context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return new AuthResult(false, null, null, null, "Invalid or expired refresh token");

        stored.IsRevoked = true;
        stored.UpdatedAt = DateTime.UtcNow;

        var accessToken = GenerateAccessToken(stored.User!);
        var newRefreshToken = await IssueRefreshTokenAsync(stored.User!);

        return new AuthResult(true, accessToken, newRefreshToken, stored.UserId.ToString(), null, stored.User!.Email, stored.User!.Name);
    }

    public async Task LogoutAsync(string userId)
    {
        var tokens = await context.RefreshTokens
            .Where(t => t.UserId.ToString() == userId)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    public async Task SendVerificationEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return;

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = BuildLink("confirm-email", email, token);

        var (html, plain) = EmailTemplate.Build(
            "Verify your email",
            "Confirm your email address",
            "Welcome to StreamlineTax! To activate your account and start tracking your income and taxes, please verify your email address.",
            "Verify",
            link,
            user.Name);

        await emailService.SendEmailAsync(email, "Verify your email", html, plain);
    }

    public async Task<string?> ConfirmEmailAsync(string email, string token)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return "User not found";

        var result = await userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded ? null : string.Join(", ", result.Errors.Select(e => e.Description));
    }

    public async Task SendPasswordResetEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var link = BuildLink("reset-password", email, token);

        var (html, plain) = EmailTemplate.Build(
            "Reset your password",
            "Reset your password",
            "We received a request to reset your password. Click the button below to choose a new one. If you didn't request this, you can safely ignore this email.",
            "Reset Password",
            link,
            user.Name);

        await emailService.SendEmailAsync(email, "Reset your password", html, plain);
    }

    public async Task<string?> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return "User not found";

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded ? null : string.Join(", ", result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Success, string? Error, string? Name)> UpdateProfileAsync(string userId, string? name = null)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return (false, "User not found", null);

        if (!string.IsNullOrWhiteSpace(name))
        {
            user.Name = name.Trim();
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return (false, string.Join(", ", result.Errors.Select(e => e.Description)), null);
        }

        return (true, null, user.Name);
    }

    private string BuildLink(string route, string email, string token)
    {
        var uiUrl = configuration["Frontend:Url"] ?? "http://localhost:4200";
        var base64Token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(token));
        return $"{uiUrl}/{route}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(base64Token)}";
    }

    private string GenerateAccessToken(AppUser user)
    {
        var jwtKey = configuration["Jwt:Key"]
            ?? Environment.GetEnvironmentVariable("JWT_KEY")
            ?? throw new InvalidOperationException("JWT signing key is not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Name, user.Name)
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "StreamlineTax",
            audience: configuration["Jwt:Audience"] ?? "StreamlineTax",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> IssueRefreshTokenAsync(AppUser user)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });

        await context.SaveChangesAsync();
        return token;
    }
}
