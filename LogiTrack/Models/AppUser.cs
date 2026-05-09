using Microsoft.AspNetCore.Identity;

namespace LogiTrack.Models
{
    public class AppUser            : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}
