namespace PaymentApi.Services;

public class MockCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key)
    {
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(false);
    }

    public Task IncrementAsync(string key, TimeSpan? expiration = null)
    {
        return Task.CompletedTask;
    }

    public Task<long> GetCounterAsync(string key)
    {
        return Task.FromResult(0L);
    }
}
