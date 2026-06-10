using Microsoft.AspNetCore.Identity;

namespace EcbatanLocation.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = default!;
}
