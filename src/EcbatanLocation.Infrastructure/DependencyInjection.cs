using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EcbatanLocation.Application.Services;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Infrastructure.Identity;
using EcbatanLocation.Infrastructure.Persistence;
using EcbatanLocation.Infrastructure.Repositories;
using EcbatanLocation.Infrastructure.Services;

namespace EcbatanLocation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<DomainEventCollectorInterceptor>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        var databaseProvider = configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";

        services.AddDbContext<EcbatanLocationDbContext>((serviceProvider, options) =>
        {
            if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                options.UseNpgsql(connectionString,
                    x => x.MigrationsAssembly("EcbatanLocation.Infrastructure.Migrations.PostgreSQL"));
            else
                options.UseSqlite(connectionString,
                    x => x.MigrationsAssembly("EcbatanLocation.Infrastructure.Migrations.Sqlite"));

            options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventCollectorInterceptor>());
        });

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<EcbatanLocationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IStudioRepository, StudioRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IOwnerRepository, OwnerRepository>();
        services.AddScoped<IPricingGridRepository, PricingGridRepository>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
