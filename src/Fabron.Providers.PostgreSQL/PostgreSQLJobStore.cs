using System.Threading.Tasks;
using Fabron.Models;
using Fabron.Providers.PostgreSQL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fabron.Store;

public class PostgreSQLJobStore : IJobStore
{
    private readonly PostgreSQLOptions _options;
    private readonly ILogger _logger;
    private readonly PostgreSQLStateStore _store;

    public PostgreSQLJobStore(IOptions<PostgreSQLOptions> options, ILogger<PostgreSQLJobStore> logger)
    {
        _options = options.Value;
        _logger = logger;

        _store = new PostgreSQLStateStore(_logger, _options.ConnectionString, _options.JobTableName, _options.JsonSerializerOptions);
    }

    public async Task<string> SetAsync(Job job, string? expectedETag)
    {
        string key = KeyUtils.BuildJobKey(job.Metadata.Name, job.Metadata.Namespace);
        return await _store.SetStateAsync(key, job, expectedETag);
    }

    public async Task<(Job? state, string? eTag)> GetAsync(string name, string @namespace)
    {
        string key = KeyUtils.BuildJobKey(name, @namespace);
        var (data, etag) = await _store.GetStateAsync<Job>(key);
        return (data, etag);
    }

    public async Task RemoveAsync(string name, string @namespace, string? expectedETag)
    {
        string key = KeyUtils.BuildJobKey(name, @namespace);
        await _store.RemoveStateAsync(key, expectedETag);
    }
}
