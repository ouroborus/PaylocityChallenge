using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using PaylocityChallenge.Authentication;

namespace PaylocityChallenge.Models.Db
{
    public class Employee
    {
        public string Id { get; set; }

        [ForeignKey("Id"), JsonIgnore]
        public virtual ApplicationUser User { get; set; }

        [Required]
        public string NameFirst { get; set; }

        [Required]
        public string NameLast { get; set; }

        [Column(TypeName = "money")]
        public decimal? SalaryBase { get; set; }

        [Required]
        public BenefitsEmployee BenefitsEmployee { get; set; }

        public List<BenefitsDependent> BenefitsDependents { get; set; }

        public Employee()
        {
            SalaryBase = 52000;
        }

        public static ApplicationUser FromSeed(
            string id = null,
            string email = null,
            string password = null,
            string nameFirst = null,
            string nameLast = null,
            decimal? salaryBase = null,
            decimal? benefitsCost = null,
            decimal? modifier = null,
            List<BenefitsDependent> dependents = null
            )
        {
            // TODO: Validate email structure
            if (email == null) throw new ArgumentNullException(nameof(email));
            // TODO: Validate password structure
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (password.Length == 0) throw new ArgumentException("Length must be greater than 0.", nameof(password));

            if (nameFirst == null) throw new ArgumentNullException(nameof(nameFirst));
            if (nameFirst.Length == 0) throw new ArgumentException("Length must be greater than 0.", nameof(nameFirst));

            if (nameLast == null) throw new ArgumentNullException(nameof(nameLast));
            if (nameLast.Length == 0) throw new ArgumentException("Length must be greater than 0.", nameof(nameLast));

            PasswordHasher<ApplicationUser> hasher = new PasswordHasher<ApplicationUser>();

            if (id == null) id = Guid.NewGuid().ToString();

            ApplicationUser appUser = new ApplicationUser() {
                Id = id,
                Email = email,
                UserName = email,
                PasswordHash = hasher.HashPassword(null, password),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                Employee = new Employee()
                {
                    NameFirst = nameFirst,
                    NameLast = nameLast,
                    BenefitsEmployee = new BenefitsEmployee(),
                },
            };

            if (salaryBase != null) appUser.Employee.SalaryBase = (decimal)salaryBase;
            if (benefitsCost != null) appUser.Employee.BenefitsEmployee.BenefitsCost = (decimal)benefitsCost;
            if (modifier != null) appUser.Employee.BenefitsEmployee.Modifier = (decimal)modifier;
            else if (nameFirst[0] == 'A' || nameFirst[0] == 'a') appUser.Employee.BenefitsEmployee.Modifier = 0.9m;

            if (dependents != null) appUser.Employee.BenefitsDependents = dependents;

            return appUser;
        }
    }

}
