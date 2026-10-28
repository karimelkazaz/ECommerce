using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.DeliveryMethods.Commands;

public sealed record RenameDeliveryMethodCommand(Guid Id, string Name) : IRequest<Result>;

public sealed class RenameDeliveryMethodCommandHandler(
    IDeliveryMethodRepository deliveryMethods,
    IUnitOfWork unitOfWork) : IRequestHandler<RenameDeliveryMethodCommand, Result>
{
    public async Task<Result> Handle(RenameDeliveryMethodCommand request, CancellationToken cancellationToken)
    {
        var deliveryMethod = await deliveryMethods.GetByIdAsync(new DeliveryMethodId(request.Id), cancellationToken);
        if (deliveryMethod is null)
            return Result.Failure(DeliveryMethodErrors.NotFound);

        var result = deliveryMethod.Rename(request.Name);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class RenameDeliveryMethodCommandValidator : AbstractValidator<RenameDeliveryMethodCommand>
{
    public RenameDeliveryMethodCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(DeliveryMethod.MaxNameLength);
    }
}
