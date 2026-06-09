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
        var proprietaires = await SeedProprietairesAsync(context, userManager);
        await SeedStudiosAsync(context);
        await SeedGrilleTarifaireAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = ["Proprietaire", "Admin"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task<List<Proprietaire>> SeedProprietairesAsync(
        PlanningLocationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        if (await context.Proprietaires.AnyAsync())
            return await context.Proprietaires.ToListAsync();

        var proprietairesData = new[]
        {
            ("Léa", "lea@planninglocation.fr"),
            ("Sarah", "sarah@planninglocation.fr"),
            ("Jean", "jean@planninglocation.fr"),
            ("Christophe", "christophe@planninglocation.fr")
        };

        var proprietaires = new List<Proprietaire>();

        foreach (var (nom, email) in proprietairesData)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Nom = nom,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Proprietaire");
                var proprietaire = Proprietaire.Creer(nom, user.Id);
                await context.Proprietaires.AddAsync(proprietaire);
                proprietaires.Add(proprietaire);
            }
        }

        await context.SaveChangesAsync();
        return proprietaires;
    }

    private static async Task SeedStudiosAsync(PlanningLocationDbContext context)
    {
        if (await context.Studios.AnyAsync())
            return;

        var studios = new[]
        {
            Studio.Creer("Villa", 6, true, true, 1),
            Studio.Creer("Studio Est", 2, true, true, 2),
            Studio.Creer("Studio Ouest", 2, true, true, 3),
            Studio.Creer("Studio Centre", 2, false, false, 4),
            Studio.Creer("Mobil-home", 6, false, false, 5),
            Studio.Creer("Emplacement tente 1", 4, false, true, 6),
            Studio.Creer("Emplacement tente 2", 4, false, true, 7),
        };

        await context.Studios.AddRangeAsync(studios);
        await context.SaveChangesAsync();
    }

    private static async Task SeedGrilleTarifaireAsync(PlanningLocationDbContext context)
    {
        if (await context.GrillesTarifaires.AnyAsync())
            return;

        var lignes = new[]
        {
            LigneTarif.Creer(TypeClient.Proprietaire, 3.50m),
            LigneTarif.Creer(TypeClient.InviteAvecPresence, 7.00m),
            LigneTarif.Creer(TypeClient.Connaissance, 15.00m),
            LigneTarif.Creer(TypeClient.MobilHome, 12.00m),
            LigneTarif.Creer(TypeClient.Tente, 7.00m),
        };

        var grille = GrilleTarifaire.Creer(2026, lignes);
        await context.GrillesTarifaires.AddAsync(grille);
        await context.SaveChangesAsync();
    }
}
