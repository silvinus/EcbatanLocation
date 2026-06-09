using Microsoft.AspNetCore.Identity;

namespace PlanningLocation.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string Nom { get; set; } = default!;
}
