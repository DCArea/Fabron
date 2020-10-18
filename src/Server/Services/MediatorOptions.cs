using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TGH.Server.Entities;

namespace TGH.Server.Services
{
    public delegate Task<string?> HandleDelegate(IServiceProvider serviceProvider, string commandData, CancellationToken token);

    public class JobOptions
    {
        private readonly Dictionary<Type, string> commandRegistrations = new Dictionary<Type, string>();
        public IReadOnlyDictionary<Type, string> CommandRegistrations => commandRegistrations;
        private readonly Dictionary<string, HandleDelegate> handlerRegistrations = new Dictionary<string, HandleDelegate>();
        public IReadOnlyDictionary<string, HandleDelegate> HandlerRegistrations => handlerRegistrations;

        public void RegisterCommand<TCommand, TResult>()
            where TCommand : ICommand<TResult>
        {
            var commandType = typeof(TCommand);
            var commandName = commandType.Name;
            commandRegistrations.TryAdd(commandType, commandName);
            handlerRegistrations.TryAdd(commandName, Handle);

            static async Task<string?> Handle(IServiceProvider sp, string commandData, CancellationToken token)
            {
                var handler = sp.GetRequiredService<ICommandHandler<TCommand, TResult>>();
                var typedcommand = JsonSerializer.Deserialize<TCommand>(commandData);
                var result = await handler.Handle(typedcommand!, token);
                return JsonSerializer.Serialize(result);
            }
        }
    }
}
