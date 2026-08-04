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
}
