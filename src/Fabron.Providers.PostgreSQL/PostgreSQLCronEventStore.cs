using Fabron.Models;
using Fabron.Store;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fabron.Providers.PostgreSQL;

public class PostgreSQLCronEventStore : ICronEventStore
{
    private readonly PostgreSQLOptions _options;
    private readonly ILogger _logger;
    private readonly PostgreSQLStateStore _store;

    public PostgreSQLCronEventStore(IOptions<PostgreSQLOptions> options, ILogger<PostgreSQLCronEventStore> logger)
    {
        _options = options.Value;
        _logger = logger;

        _store = new PostgreSQLStateStore(
            _logger,
            _options.ConnectionString,
            _options.CronEventTableName,
            _options.JsonSerializerOptions);
    }

    public Task<string> SetAsync(CronEvent state, string? expectedETag)
        => _store.SetStateAsync(state.Metadata.Key, state, expectedETag);

    public Task<StateEntry<CronEvent>?> GetAsync(string key)
        => _store.GetStateAsync<CronEvent>(key);

    public Task RemoveAsync(string key, string? expectedETag)
        => _store.RemoveStateAsync(key, expectedETag);
}
