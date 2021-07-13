// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace Fabron.Mando
{
    public interface IMediator
    {
        Task<string?> Handle(string commandName, string commandData, CancellationToken token);
    }

    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandRegistry _options;

        public Mediator(IServiceProvider serviceProvider, IOptions<CommandRegistry> options)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
        }

        public async Task<string?> Handle(string commandName, string commandData, CancellationToken token)
        {
            HandleDelegate handle = _options.HandlerRegistrations[commandName];
            string? result = await handle.Invoke(_serviceProvider, commandData, token);
            return result;
        }
    }
}
