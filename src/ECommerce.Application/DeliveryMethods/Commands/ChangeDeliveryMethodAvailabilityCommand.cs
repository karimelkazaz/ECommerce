using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.DeliveryMethods.Commands;

public sealed record ChangeDeliveryMethodAvailabilityCommand(Guid Id, bool IsAvailable) : IRequest<Result>;

public sealed class ChangeDeliveryMethodAvailabilityCommandHandler(
    IDeliveryMethodRepository deliveryMethods,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeDeliveryMethodAvailabilityCommand, Result>
{
    public async Task<Result> Handle(ChangeDeliveryMethodAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var deliveryMethod = await deliveryMethods.GetByIdAsync(new DeliveryMethodId(request.Id), cancellationToken);
        if (deliveryMethod is null)
            return Result.Failure(DeliveryMethodErrors.NotFound);

        var result = deliveryMethod.ChangeAvailability(request.IsAvailable);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class ChangeDeliveryMethodAvailabilityCommandValidator : AbstractValidator<ChangeDeliveryMethodAvailabilityCommand>
{
    public ChangeDeliveryMethodAvailabilityCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEqual(Guid.Empty);
    }
}
