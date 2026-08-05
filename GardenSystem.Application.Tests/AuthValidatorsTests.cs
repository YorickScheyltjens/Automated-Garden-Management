using GardenSystem.Application.Auth.Commands;
using GardenSystem.Application.Auth.Validators;

namespace GardenSystem.Application.Tests;

public sealed class AuthValidatorsTests
{
    [Fact]
    public void RegisterUserCommandValidator_WithValidCommand_IsValid()
    {
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand("Jane", "Doe", "jane.doe@example.com", "supersecret1");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Doe", "jane.doe@example.com", "supersecret1")]
    [InlineData("Jane", "", "jane.doe@example.com", "supersecret1")]
    [InlineData("Jane", "Doe", "", "supersecret1")]
    [InlineData("Jane", "Doe", "not-an-email", "supersecret1")]
    [InlineData("Jane", "Doe", "jane.doe@example.com", "")]
    [InlineData("Jane", "Doe", "jane.doe@example.com", "short1")]
    public void RegisterUserCommandValidator_WithInvalidInput_IsInvalid(
        string firstName, string lastName, string email, string password)
    {
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand(firstName, lastName, email, password);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void VerifyEmailCommandValidator_WithValidCommand_IsValid()
    {
        var validator = new VerifyEmailCommandValidator();
        var command = new VerifyEmailCommand("jane.doe@example.com", "123456");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "123456")]
    [InlineData("not-an-email", "123456")]
    [InlineData("jane.doe@example.com", "")]
    [InlineData("jane.doe@example.com", "12345")]
    [InlineData("jane.doe@example.com", "1234567")]
    [InlineData("jane.doe@example.com", "12345a")]
    public void VerifyEmailCommandValidator_WithInvalidInput_IsInvalid(string email, string code)
    {
        var validator = new VerifyEmailCommandValidator();
        var command = new VerifyEmailCommand(email, code);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void LoginCommandValidator_WithValidCommand_IsValid()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("jane.doe@example.com", "supersecret1");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "supersecret1")]
    [InlineData("not-an-email", "supersecret1")]
    [InlineData("jane.doe@example.com", "")]
    public void LoginCommandValidator_WithInvalidInput_IsInvalid(string email, string password)
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand(email, password);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void RefreshCommandValidator_WithValidCommand_IsValid()
    {
        var validator = new RefreshCommandValidator();
        var command = new RefreshCommand("some-refresh-token");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RefreshCommandValidator_WithEmptyToken_IsInvalid()
    {
        var validator = new RefreshCommandValidator();
        var command = new RefreshCommand("");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
