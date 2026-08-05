using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Exceptions;
using MediatR;

namespace GardenSystem.Application.Auth.Commands;

public sealed class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher) : IRequestHandler<VerifyEmailCommand, Unit>
{
    public async Task<Unit> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        var codeIsValid = user is not null
            && user.EmailVerificationCodeHash is not null
            && user.EmailVerificationCodeExpiresAtUtc is not null
            && user.EmailVerificationCodeExpiresAtUtc >= DateTime.UtcNow
            && passwordHasher.Verify(request.Code, user.EmailVerificationCodeHash);

        if (!codeIsValid)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["code"] = ["The verification code is invalid or has expired."]
            });
        }

        user!.EmailVerified = true;
        user.EmailVerificationCodeHash = null;
        user.EmailVerificationCodeExpiresAtUtc = null;

        await userRepository.UpdateAsync(user, cancellationToken);

        return Unit.Value;
    }
}
