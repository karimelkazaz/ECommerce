using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.DeliveryMethods.Commands;

public sealed record ChangeDeliveryMethodDescriptionCommand(Guid Id, string? Description) : IRequest<Result>;

public sealed class ChangeDeliveryMethodDescriptionCommandHandler(
    IDeliveryMethodRepository deliveryMethods,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeDeliveryMethodDescriptionCommand, Result>
{
    public async Task<Result> Handle(ChangeDeliveryMethodDescriptionCommand request, CancellationToken cancellationToken)
    {
        var deliveryMethod = await deliveryMethods.GetByIdAsync(new DeliveryMethodId(request.Id), cancellationToken);
        if (deliveryMethod is null)
            return Result.Failure(DeliveryMethodErrors.NotFound);

        var result = deliveryMethod.ChangeDescription(request.Description);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class ChangeDeliveryMethodDescriptionCommandValidator : AbstractValidator<ChangeDeliveryMethodDescriptionCommand>
{
    public ChangeDeliveryMethodDescriptionCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.Description)
            .MaximumLength(DeliveryMethod.MaxDescriptionLength);
    }
}
