﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace Fabron.Mando
{
    public delegate Task<string?> HandleDelegate(IServiceProvider serviceProvider, string commandData, CancellationToken token);

    public class CommandRegistry
    {
        private readonly Dictionary<Type, string> _commandNameRegistrations = new();
        public IReadOnlyDictionary<Type, string> CommandNameRegistrations => _commandNameRegistrations;
        private readonly Dictionary<string, Type> _commandTypeRegistrations = new();
        public IReadOnlyDictionary<string, Type> CommandTypeRegistrations => _commandTypeRegistrations;
        private readonly Dictionary<string, Type> _resultTypeRegistrations = new();
        public IReadOnlyDictionary<string, Type> ResultTypeRegistrations => _resultTypeRegistrations;
        private readonly Dictionary<string, HandleDelegate> _handlerRegistrations = new();
        public IReadOnlyDictionary<string, HandleDelegate> HandlerRegistrations => _handlerRegistrations;

        public void RegisterCommand<TCommand, TResult>()
            where TCommand : ICommand<TResult>
        {
            Type commandType = typeof(TCommand);
            string commandName = commandType.Name;
            Type resultType = typeof(TResult);
            _commandNameRegistrations.TryAdd(commandType, commandName);
            _commandTypeRegistrations.TryAdd(commandName, commandType);
            _resultTypeRegistrations.TryAdd(commandName, resultType);
            _handlerRegistrations.TryAdd(commandName, Handle);

            static async Task<string?> Handle(IServiceProvider sp, string commandData, CancellationToken token)
            {
                ICommandHandler<TCommand, TResult> handler = sp.GetRequiredService<ICommandHandler<TCommand, TResult>>();
                TCommand? typedcommand = JsonSerializer.Deserialize<TCommand>(commandData);
                if (typedcommand is null)
                {
                    throw new Exception("Command should not be null");
                }

                TResult result = await handler.Handle(typedcommand!, token);
                return JsonSerializer.Serialize(result);
            }
        }
    }
}
