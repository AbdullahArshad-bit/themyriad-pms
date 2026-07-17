using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels
{
    public class JobConfigurationVM
    {
        public int Id { get; set; }
        public int CategoryId {get;set;}
        public string PropertyValue { get; set; }
        public string FirstExecutuonOn { get; set; }
        public int FirstExecutionActionId { get; set; }
        public string SecondExecutionOn { get; set; }
        public int SecondExecutionACtionId { get; set; }
        public bool IsEmail { get; set; }
        public bool IsNotify { get; set; }
        public string PropertyName { get; set; }
    }
}
