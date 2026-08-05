using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Auth.Commands;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using GardenSystem.Domain.Exceptions;
using Moq;

namespace GardenSystem.Application.Tests;

public sealed class AuthHandlersTests
{
    [Fact]
    public async Task RegisterUserCommandHandler_WhenEmailAlreadyExists_ThrowsConflict()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var emailSenderMock = new Mock<IEmailSender>();

        userRepositoryMock
            .Setup(x => x.GetByEmailAsync("existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                UserId = Guid.NewGuid(),
                FirstName = "Existing",
                LastName = "User",
                Email = "existing@example.com",
                PasswordHash = "hash",
                EmailVerified = false,
                CreatedAtUtc = DateTime.UtcNow
            });

        var handler = new RegisterUserCommandHandler(userRepositoryMock.Object, passwordHasherMock.Object, emailSenderMock.Object);
        var command = new RegisterUserCommand("New", "User", "existing@example.com", "supersecret1");

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));

        userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        emailSenderMock.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterUserCommandHandler_CreatesUserWithHashedPasswordAndUnverifiedEmail_AndSendsVerificationEmail()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var emailSenderMock = new Mock<IEmailSender>();

        userRepositoryMock
            .Setup(x => x.GetByEmailAsync("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        passwordHasherMock
            .Setup(x => x.Hash("supersecret1"))
            .Returns("hashed-supersecret1");

        passwordHasherMock
            .Setup(x => x.Hash(It.Is<string>(code => code != "supersecret1")))
            .Returns<string>(code => $"hashed-{code}");

        User? capturedUser = null;
        userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user)
            .Returns(Task.CompletedTask);

        string? emailedToAddress = null;
        string? emailedBody = null;
        emailSenderMock
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((to, _, body, _) =>
            {
                emailedToAddress = to;
                emailedBody = body;
            })
            .Returns(Task.CompletedTask);

        var handler = new RegisterUserCommandHandler(userRepositoryMock.Object, passwordHasherMock.Object, emailSenderMock.Object);
        var command = new RegisterUserCommand("New", "User", "new@example.com", "supersecret1");

        var before = DateTime.UtcNow;
        var result = await handler.Handle(command, CancellationToken.None);
        var after = DateTime.UtcNow;

        Assert.NotNull(capturedUser);
        Assert.Equal("hashed-supersecret1", capturedUser!.PasswordHash);
        Assert.NotEqual("supersecret1", capturedUser.PasswordHash);
        Assert.False(capturedUser.EmailVerified);
        Assert.Equal("new@example.com", capturedUser.Email);

        Assert.NotNull(capturedUser.EmailVerificationCodeHash);
        Assert.StartsWith("hashed-", capturedUser.EmailVerificationCodeHash);
        Assert.NotNull(capturedUser.EmailVerificationCodeExpiresAtUtc);
        Assert.InRange(capturedUser.EmailVerificationCodeExpiresAtUtc!.Value, before.AddMinutes(15).AddSeconds(-2), after.AddMinutes(15).AddSeconds(2));

        Assert.Equal(capturedUser.UserId, result.UserId);
        Assert.False(result.EmailVerified);

        Assert.Equal("new@example.com", emailedToAddress);

        var emailedVerificationCode = capturedUser.EmailVerificationCodeHash!["hashed-".Length..];
        Assert.Contains(emailedVerificationCode, emailedBody);
        Assert.Matches(@"^\d{6}$", emailedVerificationCode);

        userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        emailSenderMock.Verify(
            x => x.SendEmailAsync("new@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyEmailCommandHandler_WhenUserDoesNotExist_ThrowsValidation()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        userRepositoryMock
            .Setup(x => x.GetByEmailAsync("unknown@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new VerifyEmailCommandHandler(userRepositoryMock.Object, passwordHasherMock.Object);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new VerifyEmailCommand("unknown@example.com", "123456"), CancellationToken.None));
    }

    [Fact]
    public async Task VerifyEmailCommandHandler_WhenCodeIsExpired_ThrowsValidation()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        var user = BuildUserWithVerificationCode("123456", DateTime.UtcNow.AddMinutes(-1));

        userRepositoryMock
            .Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        passwordHasherMock
            .Setup(x => x.Verify("123456", user.EmailVerificationCodeHash!))
            .Returns(true);

        var handler = new VerifyEmailCommandHandler(userRepositoryMock.Object, passwordHasherMock.Object);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new VerifyEmailCommand(user.Email, "123456"), CancellationToken.None));
    }

    [Fact]
    public async Task VerifyEmailCommandHandler_WhenCodeDoesNotMatch_ThrowsValidation()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        var user = BuildUserWithVerificationCode("123456", DateTime.UtcNow.AddMinutes(15));

        userRepositoryMock
            .Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        passwordHasherMock
            .Setup(x => x.Verify("000000", user.EmailVerificationCodeHash!))
            .Returns(false);

        var handler = new VerifyEmailCommandHandler(userRepositoryMock.Object, passwordHasherMock.Object);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new VerifyEmailCommand(user.Email, "000000"), CancellationToken.None));

        userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VerifyEmailCommandHandler_WithValidCode_MarksEmailVerifiedAndClearsCode()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        var user = BuildUserWithVerificationCode("123456", DateTime.UtcNow.AddMinutes(15));

        userRepositoryMock
            .Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        passwordHasherMock
            .Setup(x => x.Verify("123456", user.EmailVerificationCodeHash!))
            .Returns(true);

        User? updatedUser = null;
        userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((updated, _) => updatedUser = updated)
            .Returns(Task.CompletedTask);

        var handler = new VerifyEmailCommandHandler(userRepositoryMock.Object, passwordHasherMock.Object);

        await handler.Handle(new VerifyEmailCommand(user.Email, "123456"), CancellationToken.None);

        Assert.NotNull(updatedUser);
        Assert.True(updatedUser!.EmailVerified);
        Assert.Null(updatedUser.EmailVerificationCodeHash);
        Assert.Null(updatedUser.EmailVerificationCodeExpiresAtUtc);
    }

    [Fact]
    public async Task LoginCommandHandler_WhenUserDoesNotExist_ThrowsAuthentication()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        userRepositoryMock
            .Setup(x => x.GetByEmailAsync("unknown@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new LoginCommandHandler(userRepositoryMock.Object, passwordHasherMock.Object, jwtTokenGeneratorMock.Object);

        await Assert.ThrowsAsync<AuthenticationException>(
            () => handler.Handle(new LoginCommand("unknown@example.com", "whatever"), CancellationToken.None));
    }

    [Fact]
    public async Task LoginCommandHandler_WhenPasswordIsWrong_ThrowsAuthentication()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        var user = BuildVerifiedUser();
        userRepositoryMock.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        passwordHasherMock.Setup(x => x.Verify("wrong", user.PasswordHash)).Returns(false);

        var handler = new LoginCommandHandler(userRepositoryMock.Object, passwordHasherMock.Object, jwtTokenGeneratorMock.Object);

        await Assert.ThrowsAsync<AuthenticationException>(
            () => handler.Handle(new LoginCommand(user.Email, "wrong"), CancellationToken.None));
    }

    [Fact]
    public async Task LoginCommandHandler_WhenEmailNotVerified_ThrowsAuthentication()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        var user = BuildVerifiedUser();
        user.EmailVerified = false;
        userRepositoryMock.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        passwordHasherMock.Setup(x => x.Verify("correct", user.PasswordHash)).Returns(true);

        var handler = new LoginCommandHandler(userRepositoryMock.Object, passwordHasherMock.Object, jwtTokenGeneratorMock.Object);

        await Assert.ThrowsAsync<AuthenticationException>(
            () => handler.Handle(new LoginCommand(user.Email, "correct"), CancellationToken.None));
    }

    [Fact]
    public async Task LoginCommandHandler_WithValidCredentials_ReturnsTokensAndStoresHashedRefreshToken()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        var user = BuildVerifiedUser();
        userRepositoryMock.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        passwordHasherMock.Setup(x => x.Verify("correct", user.PasswordHash)).Returns(true);
        jwtTokenGeneratorMock.Setup(x => x.GenerateAccessToken(user.UserId, user.Email)).Returns("access-token-123");

        User? updatedUser = null;
        userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((updated, _) => updatedUser = updated)
            .Returns(Task.CompletedTask);

        var handler = new LoginCommandHandler(userRepositoryMock.Object, passwordHasherMock.Object, jwtTokenGeneratorMock.Object);

        var before = DateTime.UtcNow;
        var result = await handler.Handle(new LoginCommand(user.Email, "correct"), CancellationToken.None);
        var after = DateTime.UtcNow;

        Assert.Equal("access-token-123", result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);

        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser!.RefreshTokenHash);
        Assert.NotEqual(result.RefreshToken, updatedUser.RefreshTokenHash);
        Assert.NotNull(updatedUser.RefreshTokenExpiresAtUtc);
        Assert.InRange(updatedUser.RefreshTokenExpiresAtUtc!.Value, before.AddDays(7).AddSeconds(-2), after.AddDays(7).AddSeconds(2));
    }

    [Fact]
    public async Task RefreshCommandHandler_WhenTokenNotFound_ThrowsAuthentication()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        userRepositoryMock
            .Setup(x => x.GetByRefreshTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = new RefreshCommandHandler(userRepositoryMock.Object, jwtTokenGeneratorMock.Object);

        await Assert.ThrowsAsync<AuthenticationException>(
            () => handler.Handle(new RefreshCommand("some-token"), CancellationToken.None));
    }

    [Fact]
    public async Task RefreshCommandHandler_WhenTokenExpired_ThrowsAuthentication()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        var user = BuildVerifiedUser();
        user.RefreshTokenHash = "some-hash";
        user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);

        userRepositoryMock
            .Setup(x => x.GetByRefreshTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new RefreshCommandHandler(userRepositoryMock.Object, jwtTokenGeneratorMock.Object);

        await Assert.ThrowsAsync<AuthenticationException>(
            () => handler.Handle(new RefreshCommand("some-token"), CancellationToken.None));
    }

    [Fact]
    public async Task RefreshCommandHandler_WithValidToken_RotatesRefreshTokenAndReturnsNewAccessToken()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        var user = BuildVerifiedUser();
        user.RefreshTokenHash = "old-hash";
        user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(1);

        userRepositoryMock
            .Setup(x => x.GetByRefreshTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        jwtTokenGeneratorMock.Setup(x => x.GenerateAccessToken(user.UserId, user.Email)).Returns("new-access-token");

        User? updatedUser = null;
        userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((updated, _) => updatedUser = updated)
            .Returns(Task.CompletedTask);

        var handler = new RefreshCommandHandler(userRepositoryMock.Object, jwtTokenGeneratorMock.Object);

        var result = await handler.Handle(new RefreshCommand("old-plaintext-token"), CancellationToken.None);

        Assert.Equal("new-access-token", result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);

        Assert.NotNull(updatedUser);
        Assert.NotEqual("old-hash", updatedUser!.RefreshTokenHash);
    }

    private static User BuildUserWithVerificationCode(string code, DateTime expiresAtUtc)
    {
        return new User
        {
            UserId = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane.doe@example.com",
            PasswordHash = "hash",
            EmailVerified = false,
            EmailVerificationCodeHash = $"hash-of-{code}",
            EmailVerificationCodeExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static User BuildVerifiedUser()
    {
        return new User
        {
            UserId = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane.doe@example.com",
            PasswordHash = "hashed-correct",
            EmailVerified = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
