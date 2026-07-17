using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels
{
    public class FeeAssessmentJobStatusVM
    {
        public Guid JobId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
    }
}
