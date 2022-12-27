using Fabron.Models;
using Fabron.Store;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fabron.Providers.PostgreSQL;

public class PostgreSQLTimedEventStore : ITimedEventStore
{
    private readonly PostgreSQLOptions _options;
    private readonly ILogger _logger;
    private readonly PostgreSQLStateStore _store;

    public PostgreSQLTimedEventStore(IOptions<PostgreSQLOptions> options, ILogger<PostgreSQLTimedEventStore> logger)
    {
        _options = options.Value;
        _logger = logger;

        _store = new PostgreSQLStateStore(
            _logger,
            _options.ConnectionString,
            _options.TimedEventTableName,
            _options.JsonSerializerOptions);
    }

    public Task<string> SetAsync(TimedEvent state, string? expectedETag)
        => _store.SetStateAsync(state.Metadata.Key, state, expectedETag);

    public Task<StateEntry<TimedEvent>?> GetAsync(string key)
        => _store.GetStateAsync<TimedEvent>(key);

    public Task RemoveAsync(string key, string? expectedETag)
        => _store.RemoveStateAsync(key, expectedETag);
}
