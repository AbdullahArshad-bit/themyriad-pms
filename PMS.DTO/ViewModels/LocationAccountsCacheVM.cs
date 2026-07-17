using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels
{
    public class LocationAccountsCacheVM
    {
        public int LocationId { get; set; }
        public int? AccountReceivableId { get; set; }
        public int? AccountPayableId { get; set; }
        public int? RevenueAccountId { get; set; }
        public int? AdvancePaymentAccountId { get; set; }
        public string TransactionPassword { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
