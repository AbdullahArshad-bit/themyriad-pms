using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.CommonDTO
{
   public class RDLCSubreportDTO<T>
    {
            public string DataSetName { get; set; }
            public List<T> list { get; set; }
    }
}
