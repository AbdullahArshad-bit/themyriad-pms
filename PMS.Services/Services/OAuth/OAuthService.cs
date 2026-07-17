using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace PMS.Services.Services.OAuth
{
    public class OAuthService : IOAuthService
    {

        private const string ZohoTokenUrl = "https://accounts.zoho.com/oauth/v2/token";
        private const string ZohoAuthorizationUrl = "https://accounts.zoho.com/oauth/v2/auth";

        private readonly IUnitOfWork<PMSEntities> uow;

        public OAuthService(IUnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
        }

        public ZohoTokenReponse GetAccessToken(string clientId, string clientSecret, string redirectUrl, string code)
        {
            using (var client = new HttpClient())
            {
                var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUrl },
                { "code", code }
            });

                var tokenResponse = client.PostAsync(ZohoTokenUrl, tokenRequestContent).Result;
                var tokenResponseContent = tokenResponse.Content.ReadAsStringAsync().Result;

                if (tokenResponse.IsSuccessStatusCode)
                {
                    var json = JsonConvert.DeserializeObject<ZohoTokenReponse>(tokenResponseContent);
                    return json;
                }
                else
                {
                    return null;
                }
            }
        }

        public ZohoTokenReponse RefreshAccessToken(string clientId, string clientSecret, string refreshToken)
        {
            using (var client = new HttpClient())
            {
                var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "refresh_token", refreshToken }
            });

                var tokenResponse = client.PostAsync(ZohoTokenUrl, tokenRequestContent).Result;
                var tokenResponseContent = tokenResponse.Content.ReadAsStringAsync().Result;

                if (tokenResponse.IsSuccessStatusCode)
                {
                    var json = JsonConvert.DeserializeObject<ZohoTokenReponse>(tokenResponseContent);
                    return json;
                }
                else
                {
                    return null;
                }
            }
        }
        public string GenerateAuthorizationUrl(string clientId, string redirectUrl)
        {
            var queryParams = new Dictionary<string, string>
        {
             { "client_id", clientId },
             { "response_type", "code" },
             { "access_type", "offline" },
             { "redirect_uri", redirectUrl },
             { "scope", "ZohoBooks.fullaccess.all offline_access" }
         };

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"));
            return $"{ZohoAuthorizationUrl}?{queryString}";
        }

        public Dictionary<string, string> GetZohoChartOfAccounts(string accessToken, string organizationId)
        {
            var client = new RestClient("https://books.zoho.com/api/v3");
            client.AddDefaultHeader("Authorization", $"Zoho-oauthtoken {accessToken}");
            var request = new RestRequest("/chartofaccounts", Method.Get);
            request.AddParameter("organization_id", organizationId);
            var response = client.Execute(request);

            if (response.IsSuccessful)
            {
                var json = JObject.Parse(response.Content);
                var zohoAccounts = JsonConvert.DeserializeObject<List<ZohoChartOfAccount>>(json["chartofaccounts"].ToString());

                var accountDictionary = zohoAccounts
                    .Where(x => !string.IsNullOrEmpty(x.account_code)) // Filter out accounts with empty account_code
                    .ToDictionary(x => x.account_code.ToUpper(), x => x.account_id);

                return accountDictionary;
            }
            else
            {
                throw new Exception("Error occurred: " + response.ErrorMessage);
            }
        }

        public List<LedgerData> GetLedgerData(UnitOfWork<PMSEntities> uow, DateTime date)
        {
            var ledgerData = (from ledger in uow.GenericRepository<StudentLedger>().Table
                              join invoicing in uow.GenericRepository<Invoicing>().Table on ledger.InvoiceId equals invoicing.Id
                              join invoicingDetails in uow.GenericRepository<InvoicingDetail>().Table on invoicing.Id equals invoicingDetails.InvoicingId
                              join service in uow.GenericRepository<EF.Service>().Table on invoicingDetails.ServiceId equals service.ServiceId
                              join chartOfAccount in uow.GenericRepository<ChartOfAccount>().Table on service.AccountId equals chartOfAccount.Id
                              join paymentType in uow.GenericRepository<PaymentType>().Table on ledger.PaymentTypeId equals paymentType.PaymentId into pt
                              from paymentType in pt.DefaultIfEmpty()
                              where DbFunctions.TruncateTime(ledger.CreatedDate) == DateTime.Today
                                  && ledger.CreatedDate > date
                                  && invoicing.CreatedDate > date
                                  && invoicing.IsApproved == true
                                  && ledger.IsApproved == true
                              select new LedgerData
                              {
                                  Ledger = ledger,
                                  Invoicing = invoicing,
                                  ChartOfAccount = chartOfAccount,
                                  PaymentType = paymentType
                              }).ToList();

            return ledgerData;
        }
    }
}
