using Fabron.Models;
using Fabron.Store;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fabron.Providers.PostgreSQL;

public class PostgreSQLPeriodicTimerStore : IPeriodicTimerStore
{
    private readonly PostgreSQLOptions _options;
    private readonly ILogger _logger;
    private readonly PostgreSQLStateStore _store;

    public PostgreSQLPeriodicTimerStore(IOptions<PostgreSQLOptions> options, ILogger<PostgreSQLPeriodicTimerStore> logger)
    {
        _options = options.Value;
        _logger = logger;

        _store = new PostgreSQLStateStore(
            _logger,
            _options.ConnectionString,
            _options.PeriodicTimerTableName,
            _options.JsonSerializerOptions);
    }

    public Task<string> SetAsync(PeriodicTimer state, string? expectedETag)
        => _store.SetStateAsync(state.Metadata.Key, state, expectedETag);

    public Task<StateEntry<PeriodicTimer>?> GetAsync(string key)
        => _store.GetStateAsync<PeriodicTimer>(key);

    public Task RemoveAsync(string key, string? expectedETag)
        => _store.RemoveStateAsync(key, expectedETag);
}
