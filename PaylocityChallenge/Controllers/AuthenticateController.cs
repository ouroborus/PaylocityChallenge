using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using PaylocityChallenge.Authentication;
using Db = PaylocityChallenge.Models.Db;
using System.Linq;
using System.Net;

namespace PaylocityChallenge.Controllers
{
    [Route("api/authenticate")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration _configuration;

        public AuthenticateController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            this.userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await userManager.FindByNameAsync(model.Username);
            if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                    );
                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new
                {
                    token = tokenString,
                    expiration = token.ValidTo
                });
            }
            return Unauthorized();
        }

        [HttpPost]
        [Route("register")]
        [Authorize(Roles = "AddEdit")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var userExists = await userManager.FindByNameAsync(model.Email);
            if (userExists != null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "User already exists!" });
            }

            PasswordHasher<ApplicationUser> hasher = new PasswordHasher<ApplicationUser>();
            string id = Guid.NewGuid().ToString();
            var newUser = new ApplicationUser()
            {
                Id = id,
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                //PasswordHash = hasher.HashPassword(null, model.Password),
                Employee = new Db.Employee()
                {
                    Id = id,
                    NameFirst = model.NameFirst,
                    NameLast = model.NameLast,
                    //SalaryBase = model.SalaryBase,
                    BenefitsEmployee = new Db.BenefitsEmployee()
                    {
                        Id = id,
                        //BenefitsCost = model.BenefitsCost,
                        //Modifier = model.Modifier,
                    },
                }
            };

            if (model.SalaryBase != null) newUser.Employee.SalaryBase = (decimal)model.SalaryBase;
            if (model.BenefitsCost != null) newUser.Employee.BenefitsEmployee.BenefitsCost = (decimal)model.BenefitsCost;
            if (model.Modifier != null) newUser.Employee.BenefitsEmployee.Modifier = (decimal)model.Modifier;
            else if (model.NameFirst[0] == 'A') newUser.Employee.BenefitsEmployee.Modifier = 0.9m;

            //DbContextOptionsBuilder<ApplicationDbContext> optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            //optionsBuilder.UseSqlServer(_configuration.GetConnectionString("ConnStr"));
            //using var context = new ApplicationDbContext(optionsBuilder.Options);

            IdentityResult result = await userManager.CreateAsync(newUser, model.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(x => x.Description);
                return new JsonResult(new { Error = "Request contains errors.", Errors = errors }) { StatusCode = (int)HttpStatusCode.BadRequest };
            }

            //await context.SaveChangesAsync();

            return Ok(new { Status = "Success", Id = id });
        }

    }
}
