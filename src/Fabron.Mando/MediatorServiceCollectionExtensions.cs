// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Fabron.Mando;

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MediatorServiceCollectionExtensions
    {
        public static IServiceCollection RegisterCommands(this IServiceCollection services, IEnumerable<Assembly>? assemblies = null)
        {
            if (assemblies is null || !assemblies.Any())
            {
                assemblies = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a => !a.IsDynamic);
            }

            IEnumerable<Type> assemblyTypes = assemblies.SelectMany(assembly => assembly.GetExportedTypes());
            IEnumerable<Type> commandTypes = assemblyTypes.Where(t => t.IsClass && !t.IsAbstract && t.GetInterface(typeof(ICommand<>).Name) is not null);
            IEnumerable<(Type commandType, Type resultType)> tuples = commandTypes.Select(commandType =>
            {
                Type resultType = commandType.GetInterface(typeof(ICommand<>).Name)!.GetGenericArguments().Single();
                return (commandType, resultType);
            });

            services.Configure<CommandRegistry>(opt =>
            {
                foreach ((Type commandType, Type resultType) in tuples){
                    opt.RegisterCommand(commandType, resultType);
                }
            });

            return services;
        }

        public static IServiceCollection RegisterJobCommandHandlers(this IServiceCollection services, IEnumerable<Assembly>? assemblies = null)
        {
            if (assemblies is null || !assemblies.Any())
            {
                assemblies = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a => !a.IsDynamic);
            }

            IEnumerable<Type> assemblyTypes = assemblies.SelectMany(assembly => assembly.GetExportedTypes());
            IEnumerable<Type> commandTypes = assemblyTypes.Where(t => t.IsClass && !t.IsAbstract && t.GetInterface(typeof(ICommand<>).Name) is not null);
            IEnumerable<(Type commandType, Type resultType, Type handlerInterfaceType)> tuples = commandTypes.Select(commandType =>
            {
                Type resultType = commandType.GetInterface(typeof(ICommand<>).Name)!.GetGenericArguments().Single();
                Type handlerInterfaceType = typeof(ICommandHandler<,>).MakeGenericType(commandType, resultType);
                return (commandType, resultType, handlerInterfaceType);
            });

            foreach ((Type commandType, Type resultType, Type handlerInterfaceType) in tuples)
            {
                Type handlerImplemention = assemblyTypes
                    .Single(t => t.IsClass && handlerInterfaceType.IsAssignableFrom(t));

                typeof(MediatorServiceCollectionExtensions)
                    .GetMethod(nameof(RegisterJobCommandHandler))!
                    .MakeGenericMethod(handlerImplemention, commandType, resultType)!
                    .Invoke(null, new[] { services });
            }
            return services;
        }

        public static IServiceCollection RegisterJobCommandHandler<THandler, TCommand, TResult>(this IServiceCollection services)
            where THandler : class, ICommandHandler<TCommand, TResult>
            where TCommand : ICommand<TResult>
        {
            Type commandType = typeof(TCommand);
            Type handlerType = typeof(THandler);

            services.TryAddTransient<ICommandHandler<TCommand, TResult>, THandler>();
            services.Configure<CommandRegistry>(opt =>
            {
                opt.RegisterCommandHandler<TCommand, TResult>();
            });

            return services;
        }
    }
}
