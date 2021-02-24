using System.ComponentModel.DataAnnotations;

namespace PaylocityChallenge
{
    public class RegisterModel
    {
        [EmailAddress]
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        [Required(ErrorMessage = "First name is required")]
        public string NameFirst { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        public string NameLast { get; set; }

        public decimal? SalaryBase { get; set; }

        public decimal? BenefitsCost { get; set; }

        public decimal? Modifier { get; set; }
    }
}
