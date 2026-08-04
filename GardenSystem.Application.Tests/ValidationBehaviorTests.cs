using FluentValidation;
using GardenSystem.Application.Behaviors;
using MediatR;
using DomainValidationException = GardenSystem.Domain.Exceptions.ValidationException;

namespace GardenSystem.Application.Tests;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsBeforeHandlerExecution()
    {
        var validators = new List<IValidator<AlwaysFailRequest>>
        {
            new AlwaysFailValidator()
        };

        var behavior = new ValidationBehavior<AlwaysFailRequest, Unit>(validators);
        var handlerWasReached = false;

        RequestHandlerDelegate<Unit> next = _ =>
        {
            handlerWasReached = true;
            return Task.FromResult(Unit.Value);
        };

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            behavior.Handle(new AlwaysFailRequest("payload"), next, CancellationToken.None));

        Assert.False(handlerWasReached);
    }

    private sealed record AlwaysFailRequest(string Value) : IRequest<Unit>;

    private sealed class AlwaysFailValidator : AbstractValidator<AlwaysFailRequest>
    {
        public AlwaysFailValidator()
        {
            RuleFor(x => x.Value)
                .Must(_ => false)
                .WithMessage("Validation always fails for this test.");
        }
    }
}