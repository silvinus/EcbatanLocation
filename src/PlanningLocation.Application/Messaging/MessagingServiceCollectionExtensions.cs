using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace PlanningLocation.Application.Messaging;

public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the mediator and scans <paramref name="assembly"/> for request and
    /// notification handlers (closed generic interfaces), wiring each to its implementation.
    /// Pipeline behaviors are registered separately as open generics by the caller.
    /// </summary>
    public static IServiceCollection AddMediator(this IServiceCollection services, Assembly assembly)
    {
        services.AddScoped<IMediator, Mediator>();

        foreach (var type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
        {
            foreach (var contract in type.GetInterfaces().Where(i => i.IsGenericType))
            {
                var definition = contract.GetGenericTypeDefinition();
                if (definition == typeof(IRequestHandler<,>) || definition == typeof(INotificationHandler<>))
                    services.AddTransient(contract, type);
            }
        }

        return services;
    }
}
