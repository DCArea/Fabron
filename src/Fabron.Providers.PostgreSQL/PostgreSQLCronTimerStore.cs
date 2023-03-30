using Fabron.Models;
using Fabron.Store;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fabron.Providers.PostgreSQL;

public class PostgreSQLCronTimerStore : ICronTimerStore
{
    private readonly PostgreSQLOptions _options;
    private readonly ILogger _logger;
    private readonly PostgreSQLStateStore _store;

    public PostgreSQLCronTimerStore(IOptions<PostgreSQLOptions> options, ILogger<PostgreSQLCronTimerStore> logger)
    {
        _options = options.Value;
        _logger = logger;

        _store = new PostgreSQLStateStore(
            _logger,
            _options.ConnectionString,
            _options.CronTimerTableName,
            _options.JsonSerializerOptions);
    }

    public Task<string> SetAsync(CronTimer state, string? expectedETag)
        => _store.SetStateAsync(state.Metadata.Key, state, expectedETag);

    public Task<StateEntry<CronTimer>?> GetAsync(string key)
        => _store.GetStateAsync<CronTimer>(key);

    public Task RemoveAsync(string key, string? expectedETag)
        => _store.RemoveStateAsync(key, expectedETag);
}
