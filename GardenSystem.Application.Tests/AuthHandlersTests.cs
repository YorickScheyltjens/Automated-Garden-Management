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

        var handler = new RegisterUserCommandHandler(userRepositoryMock.Object, passwordHasherMock.Object);
        var command = new RegisterUserCommand("New", "User", "existing@example.com", "supersecret1");

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));

        userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterUserCommandHandler_CreatesUserWithHashedPasswordAndUnverifiedEmail()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();

        userRepositoryMock
            .Setup(x => x.GetByEmailAsync("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        passwordHasherMock
            .Setup(x => x.Hash("supersecret1"))
            .Returns("hashed-supersecret1");

        User? capturedUser = null;
        userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user)
            .Returns(Task.CompletedTask);

        var handler = new RegisterUserCommandHandler(userRepositoryMock.Object, passwordHasherMock.Object);
        var command = new RegisterUserCommand("New", "User", "new@example.com", "supersecret1");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(capturedUser);
        Assert.Equal("hashed-supersecret1", capturedUser!.PasswordHash);
        Assert.NotEqual("supersecret1", capturedUser.PasswordHash);
        Assert.False(capturedUser.EmailVerified);
        Assert.Equal("new@example.com", capturedUser.Email);

        Assert.Equal(capturedUser.UserId, result.UserId);
        Assert.False(result.EmailVerified);

        userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
