using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanningLocation.Domain.Repositories;
using PlanningLocation.Infrastructure.Identity;
using PlanningLocation.Infrastructure.Persistence;
using PlanningLocation.Infrastructure.Repositories;

namespace PlanningLocation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PlanningLocationDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<PlanningLocationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IStudioRepository, StudioRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IProprietaireRepository, ProprietaireRepository>();
        services.AddScoped<IGrilleTarifaireRepository, GrilleTarifaireRepository>();

        return services;
    }
}
