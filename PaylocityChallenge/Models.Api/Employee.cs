using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PaylocityChallenge.Authentication;

namespace PaylocityChallenge.Models.Api
{
    public class Dependent
    {
        public string Id { get; set; }
        public string NameFirst { get; set; }
        public string NameLast { get; set; }
        public decimal? BenefitsCost { get; set; }
        public decimal? Modifier { get; set; }
    }

    public class DependentUpdate : Dependent
    {
        public string UserId { get; set; }
    }

    /// <summary>
    /// Api container for servicing employee write requests
    /// </summary>
    public class EmployeeUpdate
    {
        public string Id { get; set; }

        public string NameFirst { get; set; }
        public string NameLast { get; set; }
        public decimal? SalaryBase { get; set; }

        public decimal? BenefitsCost { get; set; }
        public decimal? Modifier { get; set; }

        public EmployeeUpdate() { }

        /// <summary>
        /// Convert ApplicationUser to Employee api response
        /// </summary>
        /// <param name="user"></param>
        protected EmployeeUpdate(ApplicationUser user)
        {
            Id = user.Id;

            if (user.Employee != null)
            {
                var emp = user.Employee;
                NameFirst = emp.NameFirst;
                NameLast = emp.NameLast;
                SalaryBase = emp.SalaryBase;

                if (emp.BenefitsEmployee != null)
                {
                    var ben = emp.BenefitsEmployee;

                    BenefitsCost = ben.BenefitsCost;
                    Modifier = ben.Modifier;
                }
            }
        }

    }

    /// <summary>
    /// Api container for servicing employee create request
    /// </summary>
    public class EmployeeNew : EmployeeUpdate
    {
        public string Email { get; set; }
        public string Password { get; set; }

        public EmployeeNew() : base() { }
    }

    /// <summary>
    /// Api container for servicing extended employee read requests
    /// </summary>
    public class Employee : EmployeeUpdate
    {
        //public string UserName { get; set; }
        public string Email { get; set; }

        public List<Dependent> Dependents { get; set; }
        public List<string> Roles { get; set; }

        /// <summary>
        /// Not used
        /// </summary>
        private Employee() : base() { }

        /// <summary>
        /// Convert ApplicationUser to Employee api response
        /// </summary>
        /// <param name="user"></param>
        public Employee(ApplicationUser user) : base(user)
        {
            //UserName = user.UserName;
            Email = user.Email;

            if (user.Employee != null && user.Employee.BenefitsDependents != null)
            {
                Dependents = new List<Dependent>();

                foreach (var dep in user.Employee.BenefitsDependents)
                {
                    Dependents.Add(new Dependent
                    {
                        Id = dep.Id,
                        NameFirst = dep.NameFirst,
                        NameLast = dep.NameLast,
                        BenefitsCost = dep.BenefitsCost,
                        Modifier = dep.Modifier,
                    });
                }
            }
        }

        public Employee(ApplicationUser user, List<string> roles) : this(user)
        {
            Roles = roles;
        }
    }
}
