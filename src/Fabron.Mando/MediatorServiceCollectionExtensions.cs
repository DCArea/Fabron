// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;

using Fabron.Mando;

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MediatorServiceCollectionExtensions
    {
        public static IServiceCollection RegisterJobCommandHandlers(this IServiceCollection services, Assembly assembly)
        {
            System.Type[]? assemblyTypes = assembly.GetExportedTypes();
            System.Collections.Generic.IEnumerable<System.Type>? commandTypes = assemblyTypes.Where(t => t.IsClass && t.GetInterface(typeof(ICommand<>).Name) is not null);
            System.Collections.Generic.IEnumerable<(System.Type commandType, System.Type resultType, System.Type handlerInterfaceType)>? tuples = commandTypes.Select(commandType =>
            {
                System.Type? resultType = commandType.GetInterface(typeof(ICommand<>).Name)!.GetGenericArguments().Single();
                System.Type? handlerInterfaceType = typeof(ICommandHandler<,>).MakeGenericType(commandType, resultType);
                return (commandType, resultType, handlerInterfaceType);
            });

            foreach ((System.Type commandType, System.Type resultType, System.Type handlerInterfaceType) in tuples)
            {
                System.Type? handlerImplemention = assemblyTypes
                    .Single(t => t.IsClass && handlerInterfaceType.IsAssignableFrom(t));

                typeof(MediatorServiceCollectionExtensions)
                    .GetMethod(nameof(RegisterJobCommandHandlerHandler))!
                    .MakeGenericMethod(handlerImplemention, commandType, resultType)!
                    .Invoke(null, new[] { services });
            }
            return services;
        }

        public static IServiceCollection RegisterJobCommandHandlerHandler<THandler, TCommand, TResult>(this IServiceCollection services)
            where THandler : class, ICommandHandler<TCommand, TResult>
            where TCommand : ICommand<TResult>
        {
            System.Type? commandType = typeof(TCommand);
            System.Type? handlerType = typeof(THandler);

            services.TryAddTransient<ICommandHandler<TCommand, TResult>, THandler>();
            services.Configure<CommandRegistry>(opt =>
            {
                opt.RegisterCommand<TCommand, TResult>();
            });

            return services;
        }
    }
}
