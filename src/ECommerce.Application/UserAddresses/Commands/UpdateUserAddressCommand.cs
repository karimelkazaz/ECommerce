using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.UserAddresses;
using ECommerce.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.UserAddresses.Commands;

public sealed record UpdateUserAddressCommand(
    Guid Id,
    string Label,
    bool IsDefault,
    string RecipientFirstName,
    string RecipientLastName,
    string PhoneNumber,
    string Country,
    string City,
    string Street,
    string PostalCode) : IRequest<Result<UserAddressResponse>>;

public sealed class UpdateUserAddressCommandHandler(
    IUserAddressRepository userAddresses,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateUserAddressCommand, Result<UserAddressResponse>>
{
    public async Task<Result<UserAddressResponse>> Handle(
        UpdateUserAddressCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null || userId == Guid.Empty)
            return Result.Failure<UserAddressResponse>(UserAddressErrors.InvalidUserId);

        var userAddress = await userAddresses.GetByIdForUserAsync(
            new UserAddressId(request.Id),
            userId.Value,
            cancellationToken);
        if (userAddress is null)
            return Result.Failure<UserAddressResponse>(UserAddressErrors.NotFound);

        var addressResult = Address.Create(
            request.RecipientFirstName,
            request.RecipientLastName,
            request.PhoneNumber,
            request.Country,
            request.City,
            request.Street,
            request.PostalCode);

        if (addressResult.IsFailure)
            return Result.Failure<UserAddressResponse>(addressResult.Error);

        var updateResult = userAddress.UpdateDetails(request.Label, addressResult.Value);
        if (updateResult.IsFailure)
            return Result.Failure<UserAddressResponse>(updateResult.Error);

        if (request.IsDefault)
        {
            var existingAddresses = await userAddresses.GetByUserIdAsync(userId.Value, cancellationToken);
            foreach (var existingAddress in existingAddresses.Where(address => address.IsDefault && address.Id != userAddress.Id))
                existingAddress.SetDefault(false);
        }

        userAddress.SetDefault(request.IsDefault);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UserAddressResponse.FromDomain(userAddress);
    }
}

public sealed class UpdateUserAddressCommandValidator : AbstractValidator<UpdateUserAddressCommand>
{
    public UpdateUserAddressCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Label).NotEmpty().MaximumLength(UserAddress.MaxLabelLength);
        RuleFor(command => command.RecipientFirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.RecipientLastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(command => command.Country).NotEmpty().MaximumLength(100);
        RuleFor(command => command.City).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Street).NotEmpty().MaximumLength(200);
        RuleFor(command => command.PostalCode).NotEmpty().MaximumLength(20);
    }
}