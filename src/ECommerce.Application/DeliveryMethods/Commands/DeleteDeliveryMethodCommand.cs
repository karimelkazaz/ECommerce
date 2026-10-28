using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.DeliveryMethods.Commands;

public sealed record DeleteDeliveryMethodCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteDeliveryMethodCommandHandler(
    IDeliveryMethodRepository deliveryMethods,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteDeliveryMethodCommand, Result>
{
    public async Task<Result> Handle(DeleteDeliveryMethodCommand request, CancellationToken cancellationToken)
    {
        var deliveryMethod = await deliveryMethods.GetByIdAsync(new DeliveryMethodId(request.Id), cancellationToken);
        if (deliveryMethod is null)
            return Result.Failure(DeliveryMethodErrors.NotFound);

        deliveryMethods.Remove(deliveryMethod);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class DeleteDeliveryMethodCommandValidator : AbstractValidator<DeleteDeliveryMethodCommand>
{
    public DeleteDeliveryMethodCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEqual(Guid.Empty);
    }
}
