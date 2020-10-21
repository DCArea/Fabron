using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace TGH.Services
{
    public interface IMediator
    {
        Task<string?> Handle(string commandName, string commandData, CancellationToken token);
    }

    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly JobOptions _options;

        public Mediator(IServiceProvider serviceProvider, IOptions<JobOptions> options)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
        }

        public async Task<string?> Handle(string commandName, string commandData, CancellationToken token)
        {
            var handle = _options.HandlerRegistrations[commandName];
            var result = await handle.Invoke(_serviceProvider, commandData, token);
            return result;
        }
    }
}
