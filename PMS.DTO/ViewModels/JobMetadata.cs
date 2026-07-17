using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels
{
   public class JobMetadata
    {
        public Guid JobId { get; set; }
        public Type JobType { get; set; }
        public string JobName { get; set; }
        public string CornExpression { get; set; }

        public JobMetadata(Guid Id, Type jobType,string jobName,string cornExpression)
        {
            JobId = Id;
            JobType = jobType;
            JobName = jobName;
            CornExpression = cornExpression;
            
        }
    }
}
