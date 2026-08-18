namespace ECommerce.Domain.Common;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
