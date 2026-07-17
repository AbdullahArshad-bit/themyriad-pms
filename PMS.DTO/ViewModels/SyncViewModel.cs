using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels
{
   public class SyncViewModel
    { 
        public int SyncId { get; set; }
        public int SyncCategoryId { get; set; }
        public int SyncTypeId { get; set; }
        public int EnitityId { get; set; }
        public string Scope { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string PropertyValue { get; set; }
        public DateTime? FirstExecution { get; set; }
        public DateTime? LastUsedOn { get; set; }
        public DateTime? NextExcecutionOn { get; set; }
        public int? NextExecutionActionId { get; set; }
        public int? FirstExecutionActionId { get; set; }
        public string PropertyName { get; set; }
        public bool IsNotify { get; set; }
        public bool IsEmail { get; set; }
        public string EntityValue { get; set; }

    }
}
