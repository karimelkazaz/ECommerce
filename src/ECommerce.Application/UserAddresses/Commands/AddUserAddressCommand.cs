using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.UserAddresses;
using ECommerce.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.UserAddresses.Commands;

public sealed record AddUserAddressCommand(
    string Label,
    bool IsDefault,
    string RecipientFirstName,
    string RecipientLastName,
    string PhoneNumber,
    string Country,
    string City,
    string Street,
    string PostalCode) : IRequest<Result<UserAddressResponse>>;

public sealed class AddUserAddressCommandHandler(
    IUserAddressRepository userAddresses,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<AddUserAddressCommand, Result<UserAddressResponse>>
{
    public async Task<Result<UserAddressResponse>> Handle(
        AddUserAddressCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null || userId == Guid.Empty)
            return Result.Failure<UserAddressResponse>(UserAddressErrors.InvalidUserId);

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

        if (request.IsDefault)
        {
            var existingAddresses = await userAddresses.GetByUserIdAsync(userId.Value, cancellationToken);
            foreach (var existingAddress in existingAddresses.Where(address => address.IsDefault))
                existingAddress.SetDefault(false);
        }

        var createResult = UserAddress.Create(
            UserAddressId.New(),
            userId.Value,
            request.Label,
            addressResult.Value,
            request.IsDefault);

        if (createResult.IsFailure)
            return Result.Failure<UserAddressResponse>(createResult.Error);

        userAddresses.Add(createResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UserAddressResponse.FromDomain(createResult.Value);
    }
}

public sealed class AddUserAddressCommandValidator : AbstractValidator<AddUserAddressCommand>
{
    public AddUserAddressCommandValidator()
    {
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