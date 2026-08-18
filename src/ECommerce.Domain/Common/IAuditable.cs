namespace ECommerce.Domain.Common;

public interface IAuditable
{
    DateTimeOffset CreatedOnUtc { get; }
    Guid? CreatedBy { get; }
    
    DateTimeOffset? UpdatedOnUtc { get; }
    Guid? UpdatedBy { get; }

    void SetCreated(DateTimeOffset createdOn, Guid? createdBy);
    void SetUpdated(DateTimeOffset updatedOn, Guid? updatedBy);
}