using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Common.Classes
{
    public class TTLockResources
    {
        public const string ScienerTokenUrl = "https://euapi.sciener.com/oauth2/token";
        public const string AddForReversedCardNumberUrl = "https://euapi.sciener.com/v3/identityCard/addForReversedCardNumber";
        public const string GetStartDate = "https://euapi.sciener.com/v3/lock/queryDate";
        public const string CheckInUrlold = "http://192.168.50.50/prodapp/public/mmapi/checkin.php"; 
        public const string CheckInUrl = "http://tmds1.fortiddns.com:8023/prodapp/public/mmapi/checkin.php"; 
        //public const string EMSUrl = "http://192.168.50.50:7331/mst/enc/pms/ciems";
        public const string EMSUrl = "http://tmds1.fortiddns.com:7331/mst/enc/pms/ciems";
        public const string MesserschmittCheckOutUrl = "http://tmds1.fortiddns.com:7331/mst/enc/pms/cokey";
        //public const string MesserschmittCheckOutUrl = "http://192.168.50.50:7331/mst/enc/pms/cokey";

    }


}
