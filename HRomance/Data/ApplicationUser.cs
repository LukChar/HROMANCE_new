using Microsoft.AspNetCore.Identity;
using HRomance.Models;

namespace HRomance.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public int? MitarbeiterId { get; set; }

        public Mitarbeiter? Mitarbeiter { get; set; }
    }

}
