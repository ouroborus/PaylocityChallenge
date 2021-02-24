using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PaylocityChallenge.Authentication;
using PaylocityChallenge.Models.Db;

namespace PaylocityChallenge
{
    public class SeedData
    {
        private readonly ApplicationDbContext context;

        /// <summary>
        /// Push seed data to database
        /// </summary>
        /// <param name="context"></param>
        public SeedData(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task PopulateSeedData()
        {
            List<string> roles = new List<string>
            {
                "ViewSelf",
                "ViewOther",
                "AddEdit",
                "Remove",
                "PermissionsView",
                "PermissionsEdit",
            };
            List<string> noRoles = new List<string>();

            RoleManager<IdentityRole> roleManager = context.GetService<RoleManager<IdentityRole>>();

            // Add missing roles by Id
            foreach (var role in roles.Where(x => !context.Roles.Any(y => y.Name == x)))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }

            // TODO: Move seed data somewhere more appropriate
            var users = new[]
            {
                new {
                    AppUser = Employee.FromSeed(
                        id: ApplicationUser.SuperAdminId,
                        email: "superadmin@example.com",
                        password: "password",
                        nameFirst: "Super",
                        nameLast: "Admin",
                        salaryBase: 0,
                        benefitsCost: 0
                    ),
                    Roles = roles,
                },
                new {
                    AppUser = Employee.FromSeed(
                        email: "seeduser01@example.com",
                        password: "password",
                        nameFirst: "Seed",
                        nameLast: "User01"
                    ),
                    Roles = noRoles
                },
                new {
                    AppUser = Employee.FromSeed(
                        email: "seeduser02@example.com",
                        password: "password",
                        nameFirst: "Seed",
                        nameLast: "User02"
                    ),
                    Roles = new List<string>() { 
                        "ViewSelf",
                    },
                },
                new {
                    AppUser = Employee.FromSeed(
                        email: "seeduser03@example.com",
                        password: "password",
                        nameFirst: "Seed",
                        nameLast: "User03"
                    ),
                    Roles = new List<string>() {
                        "ViewSelf",
                        "ViewOther",
                    },
                },
                new {
                    AppUser = Employee.FromSeed(
                        email: "seeduser04@example.com",
                        password: "password",
                        nameFirst: "Seed",
                        nameLast: "User04"
                    ),
                    Roles = new List<string>() {
                        "ViewSelf",
                        "ViewOther",
                        "AddEdit",
                    },
                },
                new {
                    AppUser = Employee.FromSeed(
                        email: "seeduser05@example.com",
                        password: "password",
                        nameFirst: "Seed",
                        nameLast: "User05",
                        salaryBase: 100000,
                        dependents: new List<BenefitsDependent>()
                        {
                            BenefitsDependent.FromSeed(
                                nameFirst: "Anthony",
                                nameLast: "User05"
                            ),
                        }
                    ),
                    Roles = new List<string>() {
                        "ViewSelf",
                    },
                },
            };

            UserManager<ApplicationUser> userManager = context.GetService<UserManager<ApplicationUser>>();
            PasswordHasher<ApplicationUser> hasher = new PasswordHasher<ApplicationUser>();

            // TODO: Split seed data into "always" and "dev only"
            // Add missing users by Id
            foreach (var user in users.Where(x => !context.Users.Any(y => x.AppUser.Email == y.UserName || x.AppUser.Id == y.Id)))
            {
                // With password validation
                //var results = await userManager.CreateAsync(user.AppUser, user.AppUser.Password);

                // Without password validation
                //user.AppUser.PasswordHash = hasher.HashPassword(null, user.AppUser.password);

                // TODO: Do something with `results`
                var results = await userManager.CreateAsync(user.AppUser);

                await userManager.AddToRolesAsync(user.AppUser, user.Roles);
            }

            await context.SaveChangesAsync();
        }
    }
}
