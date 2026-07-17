using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.ServiceViewModels
{
    public class ServicesListVM
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string LocationName { get; set; }
        public decimal ServiceAmount { get; set; }

        public string Account { get; set; }
        public bool IsActive { get; set; }
        public int? ServiceTypeId { get; set; }
        public bool IsPrePlacementService { get; set; }
        public decimal? DefaultPercentage { get; set; }
        public int? TaxId { get; set; }
        public int? LocationId { get; set; }

    }

    public class AddServiceVM
    {
        public int serviceId { get; set; }

        [Required, Display(Name = "Name")]
        public string ServiceName { get; set; }

        [Required, Display(Name = "Amount")]
        public decimal ServiceAmount { get; set; }

        [Required, Display(Name = "Active")]
        public bool IsActive { get; set; }
        [Display(Name = "Account")]
        public int? AccountId { get; set; }
        public int? ServiceTypeId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }

        public bool IsPrePlacementService { get; set; }
        public int? TaxId { get; set; }
        public int? LocationId { get; set; }

    }

    public class ServiceTypes
    {
        public int Id { get; set; }
        public string Name {get;set;}
    }
}
