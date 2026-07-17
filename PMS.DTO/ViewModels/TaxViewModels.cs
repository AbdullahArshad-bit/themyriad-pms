using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.TaxViewModels
{
    public class TaxVM
    {
        public int TaxId { get; set; }
        public string Account { get; set; }
        public string TaxName { get; set; }
        public string Code { get; set; }
        public decimal TaxPercentage { get; set; }
        public bool IsActive { get; set; }
        public string LocationName { get; set; }

    }
    public class AddTaxVM
    {
        public int TaxId { get; set; }

        [Required, Display(Name = "Name")]
        public string TaxName { get; set; }

        [Required, Display(Name = "Code")]
        [MaxLength(10)]
        public string Code { get; set; }

        [Required, Display(Name = "Percentage")]
        public decimal TaxPercentage { get; set; }

        [Required, Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Required, Display(Name = "Account")]
        public int? AccountId { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public int? LocationId { get; set; }

    }
}
