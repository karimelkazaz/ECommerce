using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.DeliveryMethods.Commands;

public sealed record ChangeDeliveryMethodDeliveryTimeCommand(Guid Id, string DeliveryTime) : IRequest<Result>;

public sealed class ChangeDeliveryMethodDeliveryTimeCommandHandler(
    IDeliveryMethodRepository deliveryMethods,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeDeliveryMethodDeliveryTimeCommand, Result>
{
    public async Task<Result> Handle(ChangeDeliveryMethodDeliveryTimeCommand request, CancellationToken cancellationToken)
    {
        var deliveryMethod = await deliveryMethods.GetByIdAsync(new DeliveryMethodId(request.Id), cancellationToken);
        if (deliveryMethod is null)
            return Result.Failure(DeliveryMethodErrors.NotFound);

        var result = deliveryMethod.ChangeDeliveryTime(request.DeliveryTime);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class ChangeDeliveryMethodDeliveryTimeCommandValidator : AbstractValidator<ChangeDeliveryMethodDeliveryTimeCommand>
{
    public ChangeDeliveryMethodDeliveryTimeCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.DeliveryTime)
            .NotEmpty()
            .MaximumLength(DeliveryMethod.MaxDeliveryTimeLength);
    }
}
