namespace ECommerce.Application.Common.Options;

public class CacheEntryPolicy
{
    public int AbsoluteExpirationDays { get; set; } = 30;
    public int SlidingExpirationDays { get; set; } = 7;
    
    public int LocalCacheExpirationMinutes { get; set; } = 5;

    public int SlidingRefreshMinutes { get; set; } = 30;
}