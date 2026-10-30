using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods.Events;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities.DeliveryMethods;

public class DeliveryMethod : Entity<DeliveryMethodId>
{
    public const int MaxNameLength = 200;
    public const int MaxDeliveryTimeLength = 200;
    public const int MaxDescriptionLength = 1000;

    public string Name { get; private set; } = null!;
    public string DeliveryTime { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsAvailable { get; private set; }
    public Money Price { get; private set; } = null!;

    private DeliveryMethod() { }

    private DeliveryMethod(
        DeliveryMethodId id,
        string name,
        string deliveryTime,
        string? description,
        Money price) : base(id)
    {
        Name = name;
        DeliveryTime = deliveryTime;
        Description = description;
        IsAvailable = true;
        Price = price;
    }

    public static Result<DeliveryMethod> Create(
        DeliveryMethodId id,
        string name,
        string deliveryTime,
        Money price,
        string? description = null)
    {
        if (id.Value == Guid.Empty)
            return Result.Failure<DeliveryMethod>(DeliveryMethodErrors.InvalidId);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<DeliveryMethod>(DeliveryMethodErrors.NameRequired);

        if (name.Length > MaxNameLength)
            return Result.Failure<DeliveryMethod>(DeliveryMethodErrors.NameTooLong);

        if (string.IsNullOrWhiteSpace(deliveryTime))
            return Result.Failure<DeliveryMethod>(DeliveryMethodErrors.DeliveryTimeRequired);

        if (deliveryTime.Length > MaxDeliveryTimeLength)
            return Result.Failure<DeliveryMethod>(DeliveryMethodErrors.DeliveryTimeTooLong);

        if (price is null)
            return Result.Failure<DeliveryMethod>(DeliveryMethodErrors.PriceRequired);

        if (price.Amount < 0)
            return Result.Failure<DeliveryMethod>(DeliveryMethodErrors.InvalidPrice);

        if (description is { Length: > MaxDescriptionLength })
            return Result.Failure<DeliveryMethod>(DeliveryMethodErrors.DescriptionTooLong);

        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        var deliveryMethod = new DeliveryMethod(
            id, name.Trim(), deliveryTime.Trim(), trimmedDescription, price);
        deliveryMethod.RaiseDomainEvent(new DeliveryMethodCreatedDomainEvent(deliveryMethod.Id));

        return Result.Success(deliveryMethod);
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(DeliveryMethodErrors.NameRequired);

        if (name.Length > MaxNameLength)
            return Result.Failure(DeliveryMethodErrors.NameTooLong);

        Name = name.Trim();
        RaiseDomainEvent(new DeliveryMethodRenamedDomainEvent(Id));
        return Result.Success();
    }

    public Result ChangeDeliveryTime(string deliveryTime)
    {
        if (string.IsNullOrWhiteSpace(deliveryTime))
            return Result.Failure(DeliveryMethodErrors.DeliveryTimeRequired);

        if (deliveryTime.Length > MaxDeliveryTimeLength)
            return Result.Failure(DeliveryMethodErrors.DeliveryTimeTooLong);

        DeliveryTime = deliveryTime.Trim();
        RaiseDomainEvent(new DeliveryMethodDeliveryTimeChangedDomainEvent(Id));
        return Result.Success();
    }

    public Result ChangePrice(Money price)
    {
        if (price is null)
            return Result.Failure(DeliveryMethodErrors.PriceRequired);

        if (price.Amount < 0)
            return Result.Failure(DeliveryMethodErrors.InvalidPrice);

        var oldPrice = Price;
        Price = price;
        RaiseDomainEvent(new DeliveryMethodPriceChangedDomainEvent(Id, oldPrice, price));

        return Result.Success();
    }

    public Result ChangeDescription(string? description)
    {
        if (description is { Length: > MaxDescriptionLength })
            return Result.Failure(DeliveryMethodErrors.DescriptionTooLong);

        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        RaiseDomainEvent(new DeliveryMethodDescriptionChangedDomainEvent(Id));

        return Result.Success();
    }

    public Result ChangeAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
        RaiseDomainEvent(new DeliveryMethodAvailabilityChangedDomainEvent(Id, isAvailable));

        return Result.Success();
    }
}
