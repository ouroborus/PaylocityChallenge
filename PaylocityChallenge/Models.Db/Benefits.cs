using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PaylocityChallenge.Models.Db
{
    public class Benefits
    {
        public string Id { get; set; }

        [Column(TypeName = "money")]
        public decimal? BenefitsCost { get; set; }

        [Column(TypeName = "decimal(16,8)")]
        public decimal? Modifier { get; set; }

        public Benefits()
        {
            Modifier = 1;
        }
    }

    public class BenefitsEmployee : Benefits
    {
        public BenefitsEmployee() : base()
        {
            BenefitsCost = 1000;
        }

        [ForeignKey("Id"), JsonIgnore]
        public virtual Employee Employee { get; set; }
    }

    public class BenefitsDependent : Benefits
    {
        public BenefitsDependent() : base()
        {
            BenefitsCost = 500;
        }

        public string EmployeeId { get; set; }

        [ForeignKey("EmployeeId"), JsonIgnore]
        public virtual Employee Employee { get; set; }

        [Required]
        public string NameFirst { get; set; }

        [Required]
        public string NameLast { get; set; }

        public static BenefitsDependent FromSeed(
            string nameFirst = null,
            string nameLast = null,
            decimal? benefitsCost = null,
            decimal? modifier = null
            )
        {
            if (nameFirst == null) throw new ArgumentNullException(nameof(nameFirst));
            if (nameFirst.Length == 0) throw new ArgumentException("Length must be greater than 0.", nameof(nameFirst));

            if (nameLast == null) throw new ArgumentNullException(nameof(nameLast));
            if (nameLast.Length == 0) throw new ArgumentException("Length must be greater than 0.", nameof(nameLast));

            var dependent = new BenefitsDependent()
            {
                Id = Guid.NewGuid().ToString(),
                NameFirst = nameFirst,
                NameLast = nameLast
            };
            
            if (benefitsCost != null) dependent.BenefitsCost = (decimal)benefitsCost;
            if (modifier == null && nameFirst[0] == 'A') dependent.Modifier = 0.9m;
            else if (modifier != null) dependent.Modifier = (decimal)modifier;

            return dependent;
        }
    }
}
