using FluentAssertions;
using FluentValidation;
using StreamlineTax.Api.Models;
using StreamlineTax.Api.Validators;

namespace StreamlineTax.Tests.Unit;

public class AuthValidatorTests
{
    private readonly RegisterRequestValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();
    private readonly ResetPasswordRequestValidator _resetValidator = new();

    [Fact]
    public void RegisterRequest_ValidInput_ShouldPass()
    {
        var request = new RegisterRequest("test@example.com", "P@ssw0rd", "John");
        var result = _registerValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    public void RegisterRequest_InvalidEmail_ShouldFail(string email)
    {
        var request = new RegisterRequest(email, "P@ssw0rd", "John");
        var result = _registerValidator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    public void RegisterRequest_ShortPassword_ShouldFail(string password)
    {
        var request = new RegisterRequest("test@example.com", password, "John");
        var result = _registerValidator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LoginRequest_ValidInput_ShouldPass()
    {
        var request = new LoginRequest("test@example.com", "password");
        var result = _loginValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoginRequest_EmptyEmail_ShouldFail()
    {
        var request = new LoginRequest("", "password");
        var result = _loginValidator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ResetPasswordRequest_ValidInput_ShouldPass()
    {
        var request = new ResetPasswordRequest("test@example.com", "token123", "NewP@ssw0rd");
        var result = _resetValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ResetPasswordRequest_ShortPassword_ShouldFail()
    {
        var request = new ResetPasswordRequest("test@example.com", "token123", "12345");
        var result = _resetValidator.Validate(request);
        result.IsValid.Should().BeFalse();
    }
}
