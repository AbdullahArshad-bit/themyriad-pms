using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<T> ListData { get; set; }
    }
}
