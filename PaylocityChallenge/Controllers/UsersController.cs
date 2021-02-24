using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using PaylocityChallenge.Authentication;

using Api = PaylocityChallenge.Models.Api;
using PaylocityChallenge.Models.Db;

namespace PaylocityChallenge.Controllers
{
    [Route("api")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ApplicationDbContext dbContext;
        private readonly IConfiguration config;

        public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
            config = configuration;
        }

        /// <summary>
        /// Get current user.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("user")]
        [Authorize(Roles = "ViewSelf")]
        public async Task<IActionResult> GetSelf()
        {
            string id = userManager.GetUserId(User);
            ApplicationUser appUser = await userManager.Users.Include(x => x.Employee)
                                                             .Include(x => x.Employee.BenefitsEmployee)
                                                             .Include(x => x.Employee.BenefitsDependents)
                                                             .SingleOrDefaultAsync(x => x.Id == id);

            bool hasAddEdit = await userManager.IsInRoleAsync(appUser, "AddEdit");
            List<string> roles = hasAddEdit ? (await userManager.GetRolesAsync(appUser)).ToList() : null;

            Api.Employee emp = new Api.Employee(appUser, roles);

            return Ok(emp);
        }

        /// <summary>
        /// Get current user's roles
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("user/roles")]
        [Authorize(Roles = "AddEdit")]
        public async Task<IActionResult> GetRolesSelf()
        {
            string id = userManager.GetUserId(User);
            ApplicationUser appUser = await userManager.FindByIdAsync(id);
            IList<string> roles = await userManager.GetRolesAsync(appUser);

            return Ok(roles);
        }

        /// <summary>
        /// Get a user.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("user/{id}")]
        [Authorize(Roles = "ViewOther")]
        public async Task<IActionResult> GetUserById(string id)
        {

            ApplicationUser user = await userManager.Users.Include(x => x.Employee)
                                                          .Include(x => x.Employee.BenefitsEmployee)
                                                          .Include(x => x.Employee.BenefitsDependents)
                                                          .SingleOrDefaultAsync(x => x.Id == id);
            if (user == null)
            {
                return NotFound(new
                {
                    Id = id,
                    Error = "User not found",
                });
            }
            Api.Employee emp = new Api.Employee(user);

            return Ok(emp);
        }

        /// <summary>
        /// Get list of users by ids.
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("users")]
        [Authorize(Roles = "ViewOther")]
        public async Task<IActionResult> GetUsersById([FromBody] List<string> ids)
        {
            Dictionary<string, ApplicationUser> users;
            users = await userManager.Users.Include(x => x.Employee)
                                           .Include(x => x.Employee.BenefitsEmployee)
                                           .Include(x => x.Employee.BenefitsDependents)
                                           .ToDictionaryAsync(x => x.Id, x => x);

            var results = ids.Select(id =>
                (object)(users.TryGetValue(id, out ApplicationUser user)
                         ? new Api.Employee(user)
                         : new { Id = id, Error = "User not found" }));

            return Ok(results);
        }

        /// <summary>
        /// Get all users
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("users")]
        [Authorize(Roles = "ViewOther")]
        public async Task<IActionResult> GetUsers()
        {
            List<Api.Employee> users;
            users = await userManager.Users.Include(x => x.Employee)
                                           .Include(x => x.Employee.BenefitsEmployee)
                                           .Include(x => x.Employee.BenefitsDependents)
                                           .Select(x => new Api.Employee(x))
                                           .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Update a user.
        /// (Use "/api/authenticate/register" to create new users.)
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("user")]
        [Authorize(Roles = "AddEdit")]
        public async Task<IActionResult> UpdateUser([FromBody] Api.EmployeeNew employee)
        {
            if (employee.Id == ApplicationUser.SuperAdminId)
            {
                return new JsonResult(new { Error = "Cannot edit super admin." }) { StatusCode = (int)HttpStatusCode.Forbidden };
            }

            ApplicationUser user = await userManager.GetUserAsync(User);

            if (employee.Id == user.Id)
            {
                return new JsonResult(new { Error = "Cannot edit self." }) { StatusCode = (int)HttpStatusCode.Forbidden };
            }

            if (employee.Id == null)
            {
                // !!! Use "/api/authenticate/register" to create new users

                return new JsonResult(new { Error = "Id must be set."}) { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            // Update existing user

            List<string> errors = new List<string>();

            if (employee.NameFirst != null && employee.NameFirst.Length <= 0) errors.Add("NameFirst length must be greater than 0");
            if (employee.NameLast != null && employee.NameLast.Length <= 0) errors.Add("NameLast length must be greater than 0");
            if (employee.SalaryBase != null && employee.SalaryBase < 0) errors.Add("SalaryBase value must not be negative");
            if (employee.BenefitsCost != null && employee.BenefitsCost < 0) errors.Add("BenefitsCost value must not be negative");
            if (employee.Modifier != null && employee.Modifier < 0) errors.Add("Modifer value must not be negative");

            if (errors.Count > 0)
            {
                return new JsonResult(new { Error = "Request contains errors.", Errors = errors }) { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            ApplicationUser appUser = await userManager.Users.Include(x => x.Employee)
                                                                .Include(x => x.Employee.BenefitsEmployee)
                                                                .Include(x => x.Employee.BenefitsDependents)
                                                                .SingleOrDefaultAsync(x => x.Id == employee.Id);

            if(appUser == null)
            {
                return new JsonResult(new { Error = "User not found." }) { StatusCode = (int)HttpStatusCode.NotFound };
            }

            Employee emp = appUser.Employee;
            if (employee.NameFirst != null) emp.NameFirst = employee.NameFirst;
            if (employee.NameLast != null) emp.NameLast = employee.NameLast;
            if (employee.SalaryBase != null) emp.SalaryBase = (decimal)employee.SalaryBase;
            if (employee.BenefitsCost != null) emp.BenefitsEmployee.BenefitsCost = (decimal)employee.BenefitsCost;
            if (employee.Modifier != null) emp.BenefitsEmployee.Modifier = (decimal)employee.Modifier;

            await userManager.UpdateAsync(appUser);

            return Ok(new Api.Employee(appUser));
            

        }

        /// <summary>
        /// Get dependent by dependent id
        /// </summary>
        /// <param name="depId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("dependent/{depId}")]
        // TODO: Should this be "AddEdit" or "ViewOther"?
        [Authorize(Roles = "AddEdit")]
        public async Task<IActionResult> UpdateDependent(string depId)
        {
            //BenefitsDependent

            var dependent = await dbContext.BenefitsDependents.SingleOrDefaultAsync(x => x.Id == depId);
            if(dependent == null)
            {
                return new JsonResult(new { Error = "Dependent not found" }) { StatusCode = (int)HttpStatusCode.NotFound };
            }

            return Ok( dependent );
        
        }

        /// <summary>
        /// Create or update dependent
        /// </summary>
        /// <param name="dependent"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("dependent")]
        [Authorize(Roles = "AddEdit")]
        public async Task<IActionResult> UpdateDependent([FromBody] Api.DependentUpdate dependent)
        {
            if (dependent.UserId == ApplicationUser.SuperAdminId)
            {
                return new JsonResult(new { Error = "Cannot edit super admin." }) { StatusCode = (int)HttpStatusCode.Forbidden };
            }

            ApplicationUser user = await userManager.GetUserAsync(User);

            if (dependent.UserId == user.Id)
            {
                return new JsonResult(new { Error = "Cannot edit self." }) { StatusCode = (int)HttpStatusCode.Forbidden };
            }

            if (dependent.UserId == null)
            {
                return new JsonResult(new { Error = "UserId must be set." }) { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            List<string> errors = new List<string>();

            if (dependent.Id == null)
            {
                if (dependent.NameFirst == null) errors.Add("NameFirst must be set");
                if (dependent.NameLast == null) errors.Add("NameLast must be set");
            }

            if (dependent.NameFirst != null && dependent.NameFirst.Length <= 0) errors.Add("NameFirst length must be greater than 0");
            if (dependent.NameLast != null && dependent.NameLast.Length <= 0) errors.Add("NameLast length must be greater than 0");
            if (dependent.BenefitsCost != null && dependent.BenefitsCost < 0) errors.Add("BenefitsCost value must not be negative");
            if (dependent.Modifier != null && dependent.Modifier < 0) errors.Add("Modifer value must not be negative");

            if (errors.Count > 0)
            {
                return new JsonResult(new { Error = "Request contains errors.", Errors = errors }) { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            ApplicationUser appUser = await userManager.Users.Include(x => x.Employee)
                                                .Include(x => x.Employee.BenefitsEmployee)
                                                .Include(x => x.Employee.BenefitsDependents)
                                                .SingleOrDefaultAsync(x => x.Id == dependent.UserId);

            List<BenefitsDependent> dependents = appUser.Employee.BenefitsDependents;

            if (dependent.Id == null)
            {
                // Create new dependent
                var target = new BenefitsDependent() {
                    Id = Guid.NewGuid().ToString(),
                    NameFirst = dependent.NameFirst,
                    NameLast = dependent.NameLast,
                };

                if (dependent.BenefitsCost != null) target.BenefitsCost = (decimal)dependent.BenefitsCost;
                if (dependent.Modifier != null) target.Modifier = (decimal)dependent.Modifier;
                else if (dependent.NameFirst[0] == 'A') target.Modifier = 0.9m;

                dependents.Add(target);
            }
            else
            {
                // Update existing dependent
                BenefitsDependent target = dependents.Find(x => x.Id == dependent.Id);

                if (target == null)
                {
                    return new JsonResult(new { Error = "Dependent not found." }) { StatusCode = (int)HttpStatusCode.NotFound };
                }

                if (dependent.NameFirst != null) target.NameFirst = dependent.NameFirst;
                if (dependent.NameLast != null) target.NameLast = dependent.NameLast;
                if (dependent.BenefitsCost != null) target.BenefitsCost = (decimal)dependent.BenefitsCost;
                if (dependent.Modifier != null) target.Modifier = (decimal)dependent.Modifier;
            }

            await userManager.UpdateAsync(appUser);

            return Ok(new Api.Employee(appUser));
        }
    }
}
