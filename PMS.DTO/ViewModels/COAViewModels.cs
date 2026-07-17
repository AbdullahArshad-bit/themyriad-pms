using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.COAViewModels
{
    public class COAVM
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public int AccountTypeId { get; set; }
        public bool Status { get; set; }
        public int CreatedBy { get; set; }
        public int Updatedby { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string LocationName { get; set; }
        public string AccountType { get; set; }
    }
    public class AddCOAVM
    {
        public int Id { get; set; }
        [Required,MaxLength(10)]
        public string Code { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Alias { get; set; }
        [Required]
        public int AccountTypeId { get; set; }
        public bool Status { get; set; }
        public int CreatedBy { get; set; }
        public int Updatedby { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public int? LocationId { get; set; }
    }
}



