using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PaylocityChallenge.Models.Db;

namespace PaylocityChallenge.Authentication
{
    public class ApplicationUser : IdentityUser
    {
        public static readonly string SuperAdminId = "ffffffff-ffff-4fff-bfff-ffffffffffff";

        [Required]
        public Employee Employee { get; set; }

    }
}
