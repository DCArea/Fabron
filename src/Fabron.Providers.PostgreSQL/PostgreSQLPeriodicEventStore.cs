using System.Threading.Tasks;
using Fabron.Models;
using Fabron.Providers.PostgreSQL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fabron.Store;

public class PostgreSQLPeriodicEventStore : IPeriodicEventStore
{
    private readonly PostgreSQLOptions _options;
    private readonly ILogger _logger;
    private readonly PostgreSQLStateStore _store;

    public PostgreSQLPeriodicEventStore(IOptions<PostgreSQLOptions> options, ILogger<PostgreSQLPeriodicEventStore> logger)
    {
        _options = options.Value;
        _logger = logger;

        _store = new PostgreSQLStateStore(
            _logger,
            _options.ConnectionString,
            _options.PeriodicEventTableName,
            _options.JsonSerializerOptions);
    }

    public Task<string> SetAsync(PeriodicEvent state, string? expectedETag)
        => _store.SetStateAsync(state.Metadata.Key, state, expectedETag);

    public Task<StateEntry<PeriodicEvent>?> GetAsync(string key)
        => _store.GetStateAsync<PeriodicEvent>(key);

    public Task RemoveAsync(string key, string? expectedETag)
        => _store.RemoveStateAsync(key, expectedETag);
}
