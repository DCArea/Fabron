using Fabron.Models;
using Fabron.Store;
using Fabron.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fabron.Providers.PostgreSQL;

public class PostgreSQLGenericTimerStore : IGenericTimerStore
{
    private readonly PostgreSQLOptions _options;
    private readonly ILogger _logger;
    private readonly PostgreSQLStateStore _store;

    public PostgreSQLGenericTimerStore(IOptions<PostgreSQLOptions> options, ILogger<PostgreSQLGenericTimerStore> logger)
    {
        _options = options.Value;
        _logger = logger;

        _store = new PostgreSQLStateStore(
            _logger,
            _options.ConnectionString,
            _options.GenericTimerTableName,
            _options.JsonSerializerOptions);
    }

    public Task<string> SetAsync(GenericTimer state, string? expectedETag)
        => _store.SetStateAsync(state.Metadata.Key, state, expectedETag);

    public Task<StateEntry<GenericTimer>?> GetAsync(string key)
        => _store.GetStateAsync<GenericTimer>(key);

    public Task RemoveAsync(string key, string? expectedETag)
        => _store.RemoveStateAsync(key, expectedETag);
}
