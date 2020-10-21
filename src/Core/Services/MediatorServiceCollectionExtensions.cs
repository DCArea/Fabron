using System.Linq;
using System.Reflection;
using TGH.Contracts;
using TGH.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MediatorServiceCollectionExtensions
    {
        public static IServiceCollection RegisterJobCommandHandlers(this IServiceCollection services, Assembly assembly)
        {
            var assemblyTypes = assembly.GetExportedTypes();
            var commandTypes = assemblyTypes.Where(t => t.IsClass && t.GetInterface(typeof(ICommand<>).Name) is not null);
            var tuples = commandTypes.Select(commandType =>
            {
                var resultType = commandType.GetInterface(typeof(ICommand<>).Name)!.GetGenericArguments().Single();
                var handlerInterfaceType = typeof(ICommandHandler<,>).MakeGenericType(commandType, resultType);
                return (commandType, resultType, handlerInterfaceType);
            });

            foreach (var (commandType, resultType, handlerInterfaceType) in tuples)
            {
                var handlerImplemention = assemblyTypes
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
            var commandType = typeof(TCommand);
            var handlerType = typeof(THandler);

            services.AddTransient<ICommandHandler<TCommand, TResult>, THandler>();
            services.Configure<JobOptions>(opt =>
            {
                opt.RegisterCommand<TCommand, TResult>();
            });

            return services;
        }
    }
}
