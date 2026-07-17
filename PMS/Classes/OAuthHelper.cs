using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using PMS.DTO.ViewModels.TTLockViewModels;

//using PMS.DTO.ViewModels.TTLockViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;

using System.Threading.Tasks;
using System.Web;

namespace PMS.Classes
{
    public class OAuthHelper
    {
        private static string zohoClientId = ConfigurationManager.AppSettings["zohoClientId"];
        private static string zohoClientSecret = ConfigurationManager.AppSettings["zohoClientSecret"];
        private const string ZohoTokenUrl = "https://accounts.zoho.com/oauth/v2/token";
        private const string ZohoAuthorizationUrl = "https://accounts.zoho.com/oauth/v2/auth";

        private static string scienerClientId = ConfigurationManager.AppSettings["scienerClientId"];
        private static string scienerClientSecret = ConfigurationManager.AppSettings["scienerClientSecret"];
        private static string scienerUsername = ConfigurationManager.AppSettings["scienerUsername"];
        private static string scienerPassword = ConfigurationManager.AppSettings["scienerPassword"];
        private static string scienerTokenUrl = "https://euapi.sciener.com/oauth2/token";
        private readonly UnitOfWork<PMSEntities> uow;
        public OAuthHelper(UnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
        }

        public static ZohoTokenReponse GetAccessToken(string clientId, string clientSecret, string redirectUrl, string code)
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

        public static async Task<ZohoTokenReponse> RefreshAccessTokenAsync(string clientId, string clientSecret, string refreshToken)
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

                var tokenResponse = await client.PostAsync(ZohoTokenUrl, tokenRequestContent);
                var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();

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

        public async Task<OperationResult> CheckAndRefreshAccessToken(ClientIntegration clientIntegration)
        {
            OperationResult result = new OperationResult();

            var refreshResponse = await RefreshAccessTokenAsync(zohoClientId, zohoClientSecret, clientIntegration.Refresh_Token);

            if (refreshResponse != null && !string.IsNullOrEmpty(refreshResponse.AccessToken))
            {
                clientIntegration.Access_Token = refreshResponse.AccessToken;
                clientIntegration.Access_Token_Expiry = DateTime.Now.AddSeconds(refreshResponse.AccessTokenExpiresIn).ToString();
                uow.GenericRepository<ClientIntegration>().Update(clientIntegration);
                uow.SaveChanges();

                result.Success = true;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Failed to refresh access token.";
            }

            return result;
        }

        public static string GenerateAuthorizationUrl(string clientId, string redirectUrl)
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

        public static Dictionary<string, string> GetZohoChartOfAccounts(string accessToken, string organizationId)
        {
            var client = new RestClient("https://books.zoho.com/api/v3");
            client.AddDefaultHeader("Authorization", $"Zoho-oauthtoken {accessToken}");

            var request = new RestRequest("/chartofaccounts", Method.GET);
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
                              join service in uow.GenericRepository<Service>().Table on invoicingDetails.ServiceId equals service.ServiceId
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

        public List<InvoingData> GetInvoicingData(UnitOfWork<PMSEntities> uow, DateTime date)
        {


            //var T = uow.GenericRepository<Invoicing>().Table.Include(X => X.InvoicingDetails).Where(invoicing => invoicing.IsApproved == true && invoicing.ZohoInvoiceId == null).Include(X).ToList();


            var invoicingData = (from invoicing in uow.GenericRepository<Invoicing>().Table
                                 where 
                                       invoicing.IsApproved == true && invoicing.ZohoInvoiceId ==null
                                 join invoicingDetail in uow.GenericRepository<InvoicingDetail>().Table on invoicing.Id equals invoicingDetail.InvoicingId
                                 
                                 join taxes in uow.GenericRepository<Tax>().Table on invoicingDetail.TaxesIds equals taxes.TaxId.ToString() into taxJoin
                                 from tax in taxJoin.DefaultIfEmpty()
                                 select new InvoingData
                                 {
                                     Invoicing = invoicing,
                                     InvoicingDetail = invoicingDetail,
                                     Tax = tax,
                                 }).ToList();

            return invoicingData;
        }

        public static TTLockTokenVM GetScienerAccessToken(string scienerClientId, string scienerClientSecret, string scienerUsername, string scienerPassword)
        {
            using (var client = new HttpClient())
            {
                var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "clientId", scienerClientId },
                { "clientSecret", scienerClientSecret },
                { "username", scienerUsername },
                { "password", scienerPassword }
            });

                var tokenResponse = client.PostAsync(scienerTokenUrl, tokenRequestContent).Result;
                var tokenResponseContent = tokenResponse.Content.ReadAsStringAsync().Result;

                if (tokenResponse.IsSuccessStatusCode)
                {
                    var json = JsonConvert.DeserializeObject<TTLockTokenVM>(tokenResponseContent);
                    return json;
                }
                else
                {
                    return null;
                }
            }
        }

        public static async Task<TTLockTokenVM> RefreshScienerAccessTokenAsync(string clientId, string clientSecret, string refreshToken)
        {
            using (var client = new HttpClient())
            {
                var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "clientId", clientId },
            { "clientSecret", clientSecret },
            { "refresh_token", refreshToken }
        });

                var tokenResponse = await client.PostAsync(scienerTokenUrl, tokenRequestContent);
                var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();

                if (tokenResponse.IsSuccessStatusCode)
                {
                    var json = JsonConvert.DeserializeObject<TTLockTokenVM>(tokenResponseContent);
                    return json;
                }
                else
                {
                    return null;
                }
            }
        }

        public  async Task<OperationResult> CheckAndRefreshScienerAccessToken(ClientIntegration clientIntegration)
        {
            OperationResult result = new OperationResult();

            var refreshResponse = await RefreshScienerAccessTokenAsync(scienerClientId, scienerClientSecret, clientIntegration.Refresh_Token);

            if (refreshResponse != null && !string.IsNullOrEmpty(refreshResponse.AccessToken))
            {
                clientIntegration.Access_Token = refreshResponse.AccessToken;
                clientIntegration.Access_Token_Expiry = DateTime.Now.AddSeconds(refreshResponse.AccessTokenExpiresIn).ToString();
                uow.GenericRepository<ClientIntegration>().Update(clientIntegration);
                uow.SaveChanges();

                result.Success = true;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Failed to refresh access token.";
            }

            return result;
        }

        public class OperationResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public string ExtractedErrorMessage { get; set; }
            public DateTime MaxDate { get; set; }
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

        public class ZohoCustomer
        {
            [JsonProperty("contact_name")]
            public string contact_name { get; set; }

            [JsonProperty("contact_persons")]
            public List<ZohoContactPerson> contact_persons { get; set; }
        }

        public class ZohoContactPerson
        {
            [JsonProperty("first_name")]
            public string first_name { get; set; }

            [JsonProperty("email")]
            public string email { get; set; }

            [JsonProperty("phone")]
            public string phone { get; set; }

            [JsonProperty("salutation")]
            public string salutation { get; set; }

            [JsonProperty("company_name")]
            public string company_name { get; set; }
        }

        public class ZohoChartOfAccount
        {
            [JsonProperty("account_id", NullValueHandling = NullValueHandling.Ignore)]
            public string account_id { get; set; }
            public string account_code { get; set; }

            public string account_name { get; set; }
            public string account_type { get; set; }
        }

        public class ZohoChartOfAccountResponse
        {
            public ZohoChartOfAccount chart_of_account { get; set; }
        }

        public class ZohoTax
        {
            public string tax_name { get; set; }
            public decimal tax_percentage { get; set; }
        }

        public class ZohoTaxDetails
        {
            public string tax_id { get; set; }
            public string tax_name { get; set; }
            public decimal tax_percentage { get; set; }
        }

        public class ZohoTaxResponse
        {
            public ZohoTaxDetails tax { get; set; }
        }

        public class InvoingData
        {
            public Invoicing Invoicing { get; set; }
            public InvoicingDetail InvoicingDetail { get; set; }
            public Tax Tax { get; set; }
        }

        public class ZohoInvoice
        {
            [JsonProperty("customer_id")]
            public string CustomerId { get; set; }

            [JsonProperty("date")]
            public string Date { get; set; }
            [JsonProperty("reference_number")]
            public string ReferenceNumber { get; set; }
            [JsonProperty("line_items")]
            public List<ZohoInvoiceLineItem> LineItems { get; set; }
        }

        public class ZohoInvoiceLineItem
        {
            [JsonProperty("name")]
            public string Name { get; set; } 

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("quantity")]
            public int Quantity { get; set; }

            [JsonProperty("rate")]
            public double Rate { get; set; }

            [JsonProperty("tax_percentage")]
            public double TaxPercentage { get; set; }

            [JsonProperty("tax_id")]
            public string TaxId { get; set; }

            [JsonProperty("tax_name")]
            public string TaxName { get; set; }
        }

        public class ZohoJournal
        {
            public string journal_date { get; set; }
            public string reference_number { get; set; }
            public List<ZohoJournalLineItem> line_items { get; set; }

        }

        public class ZohoJournalLineItem
        {
            public string account_id { get; set; }
            public string debit_or_credit { get; set; }
            public double amount { get; set; }
            public string description { get; set; }
            public string notes { get; set; }
            public string account_name { get; set; }
            public int item_order { get; set; }
        }

        public class LedgerData
        {
            public StudentLedger Ledger { get; set; }
            public Invoicing Invoicing { get; set; }
            public ChartOfAccount ChartOfAccount { get; set; }
            public PaymentType PaymentType { get; set; }
        }

        public class LocklistResponse
        {
            public List<LockItem> list { get; set; }
        }
        public class GateWayResponse
        {
            public List<GateWayItem> list { get; set; }
        }
      
        public class GateWayItem
        {
            public string gatewayMac { get; set; }
            public string lockNum { get; set; }
            public string gatewayName { get; set; }
            public int isOnline { get; set; }
            public int gatewayVersion { get; set; }
            public int gatewayId { get; set; }
        }
        public class GeteWayDetail
        {
            public string GateWayMac { get; set; }
            public string LockNum { get; set; }
            public string GatewayName { get; set; }
            public int IsOnline { get; set; }
            public int GatewayVersion { get; set; }
            public int GatewayId { get; set; }
        }
        public class LockListResponse
        {
            public List<LockListItem> list { get; set; }
        }
        public class LockListItem
        {
            public int lockId { get; set; }
            public string lockMac { get; set; }
            public string lockName { get; set; }
            public string lockAlias { get; set; }
            public int rssi { get; set; }
            public long updateDate { get; set; }
        }
        public class LockListDetail
        {
            public int LockId { get; set; }
            public string LockMac { get; set; }
            public string LockName { get; set; }
            public string LockAlias { get; set; }
            public int Rssi { get; set; }
            public long UpdateDate { get; set; }
        }

        public class ICCardResponse
        {
            public List<ICCardItem> list { get; set; }
            [JsonProperty("cardId")]
            public int CardId { get; set; }
        }
        public class ICCardItem
        {
            public int cardId { get; set; }
            public int lockId { get; set; }
            public string cardNumber { get; set; }
            public string cardName { get; set; }
            public long startDate { get; set; }
            public long endDate { get; set; }
            public long createDate { get; set; }
            public string senderUsername { get; set; }
            public int cardType { get; set; }
        }
        public class ICCardDetail
        {
            public int CardId { get; set; }
            public int LockId { get; set; }
            public string CardNumber { get; set; }
            public string CardName { get; set; }
            public long StartDate { get; set; }
            public long EndDate { get; set; }
            public long CreateDate { get; set; }
            public string SenderUsername { get; set; }
            public int CardType { get; set; }
        }

        public class DeleteCardResponse
        {
            public int ErrCode { get; set; }
            public string ErrMsg { get; set; }
        }
        public class ChangePeriodResponse
        {
            public int ErrCode { get; set; }
            public string ErrMsg { get; set; }
        }

        public class CyclicConfig
        {
            public int WeekDay { get; set; } // 1-7 (1 = Monday, 2 = Tuesday, ..., 7 = Sunday)
            public int StartTime { get; set; } // Valid start time in minutes
            public int EndTime { get; set; } // Valid end time in minutes
        }

        public class RenameResponse
        {
            public int ErrCode { get; set; }
            public string ErrMsg { get; set; }
        }



        public class LockItem
        {
            public long lockId { get; set; }
            public string lockMac { get; set; }
            public string lockAlias { get; set; }
            public string lockName { get; set; }
        }

        public class LockDetails
        {
            public long LockId { get; set; }
            public string LockMac { get; set; }
            public string LockAlias { get; set; }
            public string LockName { get; set; }
        }

        public class LockDetailResponse
        {
            public LockDetailItem lockDetail { get; set; }
        }

        public class LockDetailItem
        {
            public string modelNum { get; set; }
            public string lockMac { get; set; }
            public string lockKey { get; set; }
            public string lockName { get; set; }
        }

        public class LockDetailDetails
        {
            public string ModelNum { get; set; }
            public string LockMac { get; set; }
            public string LockKey { get; set; }
            public string LockName { get; set; }
        }

    }
}