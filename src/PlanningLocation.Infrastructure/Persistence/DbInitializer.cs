using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Enums;
using PlanningLocation.Infrastructure.Identity;

namespace PlanningLocation.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PlanningLocationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedOwnersAsync(context, userManager);
        await SeedStudiosAsync(context);
        await SeedPricingGridAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = ["Owner", "Admin"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedOwnersAsync(
        PlanningLocationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        if (await context.Owners.AnyAsync())
            return;

        var ownersData = new[]
        {
            ("Léa", "lea@planninglocation.fr"),
            ("Sarah", "sarah@planninglocation.fr"),
            ("Jean", "jean@planninglocation.fr"),
            ("Christophe", "christophe@planninglocation.fr")
        };

        foreach (var (name, email) in ownersData)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = name,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Owner");
                var owner = Owner.Create(name, user.Id);
                await context.Owners.AddAsync(owner);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedStudiosAsync(PlanningLocationDbContext context)
    {
        if (await context.Studios.AnyAsync())
            return;

        var studios = new[]
        {
            Studio.Create("Villa", 6, true, true, 1),
            Studio.Create("Studio Est", 2, true, true, 2),
            Studio.Create("Studio Ouest", 2, true, true, 3),
            Studio.Create("Studio Centre", 2, false, false, 4),
            Studio.Create("Mobil-home", 6, false, false, 5),
            Studio.Create("Emplacement tente 1", 4, false, true, 6),
            Studio.Create("Emplacement tente 2", 4, false, true, 7),
        };

        await context.Studios.AddRangeAsync(studios);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPricingGridAsync(PlanningLocationDbContext context)
    {
        if (await context.PricingGrids.AnyAsync())
            return;

        var lines = new[]
        {
            PricingLine.Create(ClientType.Owner, 3.50m),
            PricingLine.Create(ClientType.GuestWithPresence, 7.00m),
            PricingLine.Create(ClientType.Acquaintance, 15.00m),
            PricingLine.Create(ClientType.MobileHome, 12.00m),
            PricingLine.Create(ClientType.Tent, 7.00m),
        };

        var grid = PricingGrid.Create(2026, lines);
        await context.PricingGrids.AddAsync(grid);
        await context.SaveChangesAsync();
    }
}
