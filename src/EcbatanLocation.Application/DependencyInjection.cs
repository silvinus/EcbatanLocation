using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using EcbatanLocation.Application.Behaviors;
using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Services;
using EcbatanLocation.Domain.Services;

namespace EcbatanLocation.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediator(assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<ReservationDomainService>();
        services.AddScoped<HypotheticalPromotionService>();

        return services;
    }
}
