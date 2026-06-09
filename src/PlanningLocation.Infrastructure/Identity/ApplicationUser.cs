using Microsoft.AspNetCore.Identity;

namespace PlanningLocation.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = default!;
}
