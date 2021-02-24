using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using PaylocityChallenge.Authentication;
using PaylocityChallenge.Models.Db;

namespace PaylocityChallenge
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);


            //builder.Entity<BenefitsEmployee>().Property(b => b.BenefitsCost).HasDefaultValue(1000);
            //builder.Entity<BenefitsEmployee>().Property(b => b.Modifier).HasDefaultValue(1);

            //builder.Entity<BenefitsDependent>().Property(b => b.BenefitsCost).HasDefaultValue(500);
            //builder.Entity<BenefitsDependent>().Property(b => b.Modifier).HasDefaultValue(1);


        }

        public virtual DbSet<Employee> Employees { get; set; }
        public virtual DbSet<BenefitsEmployee> BenefitsEmployees { get; set; }
        public virtual DbSet<BenefitsDependent> BenefitsDependents { get; set; }
    }
}
