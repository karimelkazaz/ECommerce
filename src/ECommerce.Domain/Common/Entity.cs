namespace ECommerce.Domain.Common;

public abstract class Entity<TId> : ISoftDeletable, IAuditable where TId : notnull
{
    public TId Id { get; init; } = default!;
    public bool IsDeleted { get; protected set; }

    public DateTimeOffset CreatedOnUtc { get; protected set; }

    public Guid? CreatedBy { get; protected set; }

    public DateTimeOffset? UpdatedOnUtc { get; protected set; }

    public Guid? UpdatedBy { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(TId id)
    {
        Id = id;
    }

    protected Entity() { }


    public void MarkAsDeleted()
    {
        IsDeleted = true;
        UpdatedOnUtc = DateTimeOffset.UtcNow;
    }
    public void ClearDomainEvents() => _domainEvents.Clear();
    public IReadOnlyList<IDomainEvent> GetDomainEvents() => _domainEvents.ToList();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void SetCreated(DateTimeOffset createdOn, Guid? createdBy)
    {
        CreatedOnUtc = createdOn;
        CreatedBy = createdBy;
    }

    public void SetUpdated(DateTimeOffset updatedOn, Guid? updatedBy)
    {
        UpdatedOnUtc = updatedOn;
        UpdatedBy = updatedBy;
    }
}