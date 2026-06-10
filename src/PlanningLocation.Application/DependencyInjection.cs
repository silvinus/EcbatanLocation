using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PlanningLocation.Application.Behaviors;
using PlanningLocation.Application.Messaging;
using PlanningLocation.Domain.Services;

namespace PlanningLocation.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediator(assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<ReservationDomainService>();

        return services;
    }
}
