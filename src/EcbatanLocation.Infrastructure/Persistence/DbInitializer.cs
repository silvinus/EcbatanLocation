using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Infrastructure.Identity;

namespace EcbatanLocation.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EcbatanLocationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedOwnersAsync(context, userManager);
        await EnsureAdminsAsync(userManager);
        await SeedStudiosAsync(context);
        await SeedPricingGridAsync(context);
        await BackfillPerBedBedCountsAsync(context);
    }

    /// <summary>
    /// Idempotent: per-bed studios may carry legacy reservations created before the switch to
    /// per-bed mode, with a bed count of 0. Give each its adult count (clamped to the studio's
    /// beds, at least one) so the occupation KPI counts them. Runs on every startup; once fixed,
    /// no reservation on a per-bed studio has a 0 bed count, so subsequent runs are no-ops.
    /// </summary>
    private static async Task BackfillPerBedBedCountsAsync(EcbatanLocationDbContext context)
    {
        var perBedStudios = await context.Studios
            .Where(s => s.RentalMode == RentalMode.PerBed)
            .Select(s => new { s.Id, s.NumberOfBeds })
            .ToListAsync();

        var changed = false;
        foreach (var studio in perBedStudios)
        {
            var reservations = await context.Reservations
                .Where(r => r.StudioId == studio.Id && r.BedCount == 0)
                .ToListAsync();

            foreach (var reservation in reservations)
            {
                reservation.BackfillBedCount(Math.Min(Math.Max(1, reservation.TotalAdultCount), studio.NumberOfBeds));
                changed = true;
            }
        }

        if (changed)
            await context.SaveChangesAsync();
    }

    /// <summary>
    /// Idempotent: grants the Admin role to Christophe (owner + admin) and ensures the
    /// dedicated technical admin account "Sylvain" exists with the Admin role.
    /// Runs on every startup so it also upgrades already-seeded databases.
    /// </summary>
    private static async Task EnsureAdminsAsync(UserManager<ApplicationUser> userManager)
    {
        var christophe = await userManager.FindByEmailAsync("christophe@ecbatanelocation.fr");
        if (christophe is not null && !await userManager.IsInRoleAsync(christophe, "Admin"))
            await userManager.AddToRoleAsync(christophe, "Admin");

        const string sylvainEmail = "sylvain@ecbatanelocation.fr";
        var sylvain = await userManager.FindByEmailAsync(sylvainEmail);
        if (sylvain is null)
        {
            sylvain = new ApplicationUser
            {
                UserName = sylvainEmail,
                Email = sylvainEmail,
                DisplayName = "Sylvain",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(sylvain, "Password123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(sylvain, "Admin");
        }
        else if (!await userManager.IsInRoleAsync(sylvain, "Admin"))
        {
            await userManager.AddToRoleAsync(sylvain, "Admin");
        }
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
        EcbatanLocationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        if (await context.Owners.AnyAsync())
            return;

        var ownersData = new[]
        {
            ("Léa", "lea@ecbatanelocation.fr"),
            ("Sarah", "sarah@ecbatanelocation.fr"),
            ("Jean", "jean@ecbatanelocation.fr"),
            ("Christophe", "christophe@ecbatanelocation.fr")
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

    private static async Task SeedStudiosAsync(EcbatanLocationDbContext context)
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

    private static async Task SeedPricingGridAsync(EcbatanLocationDbContext context)
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
