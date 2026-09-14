using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamlineTax.Application.Common.Interfaces;
using System.Security.Claims;

namespace StreamlineTax.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator,
    IValidator<EmailRequest> emailValidator,
    IValidator<ConfirmEmailRequest> confirmValidator,
    IValidator<ResetPasswordRequest> resetValidator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var validation = await registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { error = validation.Errors.FirstOrDefault()?.ErrorMessage });

        var result = await authService.RegisterAsync(request.Email, request.Password, request.Name);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new { token = result.Token, refreshToken = result.RefreshToken, userId = result.UserId, email = request.Email, name = result.Name });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validation = await loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { error = validation.Errors.FirstOrDefault()?.ErrorMessage });

        var result = await authService.LoginAsync(request.Email, request.Password);
        if (!result.Success)
            return Unauthorized(new { error = result.Error });

        return Ok(new { token = result.Token, refreshToken = result.RefreshToken, userId = result.UserId, email = request.Email, name = result.Name });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var validation = await refreshValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { error = validation.Errors.FirstOrDefault()?.ErrorMessage });

        var result = await authService.RefreshAsync(request.RefreshToken);
        if (!result.Success)
            return Unauthorized(new { error = result.Error });

        return Ok(new { token = result.Token, refreshToken = result.RefreshToken, userId = result.UserId, email = result.Email, name = result.Name });
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required" });

        var (success, error, name) = await authService.UpdateProfileAsync(UserId.ToString(), request.Name);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { name });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await authService.LogoutAsync(UserId.ToString());
        return NoContent();
    }

    [HttpPost("verify-email-request")]
    public async Task<IActionResult> SendVerification([FromBody] EmailRequest request)
    {
        var validation = await emailValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { error = validation.Errors.FirstOrDefault()?.ErrorMessage });

        await authService.SendVerificationEmailAsync(request.Email);
        return Ok(new { message = "If the email exists, a verification link was sent." });
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        var validation = await confirmValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { error = validation.Errors.FirstOrDefault()?.ErrorMessage });

        var error = await authService.ConfirmEmailAsync(request.Email, DecodeToken(request.Token));
        return error is null ? Ok(new { message = "Email confirmed." }) : BadRequest(new { error });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] EmailRequest request)
    {
        var validation = await emailValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { error = validation.Errors.FirstOrDefault()?.ErrorMessage });

        await authService.SendPasswordResetEmailAsync(request.Email);
        return Ok(new { message = "If the email exists, a reset link was sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var validation = await resetValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { error = validation.Errors.FirstOrDefault()?.ErrorMessage });

        var error = await authService.ResetPasswordAsync(request.Email, DecodeToken(request.Token), request.NewPassword);
        return error is null
            ? Ok(new { message = "Password reset successful." })
            : BadRequest(new { error });
    }

    private static string DecodeToken(string base64) =>
        System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(base64));
}

public record RegisterRequest(string Email, string Password, string Name);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record EmailRequest(string Email);
public record ConfirmEmailRequest(string Email, string Token);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record UpdateProfileRequest(string Name);