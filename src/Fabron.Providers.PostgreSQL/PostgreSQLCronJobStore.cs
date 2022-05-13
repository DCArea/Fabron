using System.Threading.Tasks;
using Fabron.Models;
using Fabron.Providers.PostgreSQL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fabron.Store;

public class PostgreSQLCronJobStore : ICronJobStore
{
    private readonly PostgreSQLOptions _options;
    private readonly ILogger _logger;
    private readonly PostgreSQLStateStore _store;

    public PostgreSQLCronJobStore(IOptions<PostgreSQLOptions> options, ILogger<PostgreSQLCronJobStore> logger)
    {
        _options = options.Value;
        _logger = logger;

        _store = new PostgreSQLStateStore(_logger, _options.ConnectionString, _options.CronJobTableName, _options.JsonSerializerOptions);
    }

    public async Task<string> SetAsync(CronJob state, string? expectedETag)
    {
        string key = KeyUtils.BuildJobKey(state.Metadata.Name, state.Metadata.Namespace);
        return await _store.SetStateAsync(key, state, expectedETag);
    }

    public async Task<(CronJob? state, string? eTag)> GetAsync(string name, string @namespace)
    {
        string key = KeyUtils.BuildJobKey(name, @namespace);
        var (data, etag) = await _store.GetStateAsync<CronJob>(key);
        return (data, etag);
    }

    public async Task RemoveAsync(string name, string @namespace, string? expectedETag)
    {
        string key = KeyUtils.BuildJobKey(name, @namespace);
        await _store.RemoveStateAsync(key, expectedETag);
    }
}
