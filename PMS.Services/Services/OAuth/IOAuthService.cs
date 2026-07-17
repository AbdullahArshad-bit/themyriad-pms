using Newtonsoft.Json;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.OAuth
{
    public interface IOAuthService
    {
        ZohoTokenReponse GetAccessToken(string clientId, string clientSecret, string redirectUrl, string code);
        ZohoTokenReponse RefreshAccessToken(string clientId, string clientSecret, string refreshToken);
        string GenerateAuthorizationUrl(string clientId, string redirectUrl);
        Dictionary<string, string> GetZohoChartOfAccounts(string accessToken, string organizationId);
        List<LedgerData> GetLedgerData(UnitOfWork<PMSEntities> uow, DateTime date);

    }
    public class ZohoTokenReponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int AccessTokenExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }
    public class ZohoJournal
    {
        public string journal_date { get; set; }
        public string reference_number { get; set; }
        public List<ZohoLineItem> line_items { get; set; }

    }
    public class ZohoLineItem
    {
        public string account_id { get; set; }
        public string debit_or_credit { get; set; }
        public double amount { get; set; }
        public string description { get; set; }
        public string notes { get; set; }
        public string account_name { get; set; }
        public int item_order { get; set; }
    }
    public class ZohoOperationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string ExtractedErrorMessage { get; set; }

    }
    public class ZohoChartOfAccount
    {
        public string account_id { get; set; }
        public string account_code { get; set; }
        public string account_name { get; set; }
    }
    public class LedgerData
    {
        public StudentLedger Ledger { get; set; }
        public Invoicing Invoicing { get; set; }
        public ChartOfAccount ChartOfAccount { get; set; }
        public PaymentType PaymentType { get; set; }
    }
}
