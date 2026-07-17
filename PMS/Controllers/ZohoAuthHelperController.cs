using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;

namespace PMS.Controllers
{
    //public static class ZohoAuthHelper
    //{
    //    private const string ZohoTokenUrl = "https://accounts.zoho.com/oauth/v2/token";
    //    private const string ZohoAuthorizationUrl = "https://accounts.zoho.com/oauth/v2/auth";

    //    public static ZohoTokenReponse GetAccessToken(string clientId, string clientSecret, string redirectUrl, string code)
    //    {
    //        using (var client = new HttpClient())
    //        {
    //            var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
    //        {
    //            { "grant_type", "authorization_code" },
    //            { "client_id", clientId },
    //            { "client_secret", clientSecret },
    //            { "redirect_uri", redirectUrl },
    //            { "code", code }
    //        });

    //            var tokenResponse = client.PostAsync(ZohoTokenUrl, tokenRequestContent).Result;
    //            var tokenResponseContent = tokenResponse.Content.ReadAsStringAsync().Result;

    //            if (tokenResponse.IsSuccessStatusCode)
    //            {
    //                var json = JsonConvert.DeserializeObject<ZohoTokenReponse>(tokenResponseContent);
    //                return json;
    //            }
    //            else
    //            {
    //                return null;
    //            }
    //        }
    //    }

    //    public static ZohoTokenReponse RefreshAccessToken(string clientId, string clientSecret, string refreshToken)
    //    {
    //        using (var client = new HttpClient())
    //        {
    //            var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
    //        {
    //            { "grant_type", "refresh_token" },
    //            { "client_id", clientId },
    //            { "client_secret", clientSecret },
    //            { "refresh_token", refreshToken }
    //        });

    //            var tokenResponse = client.PostAsync(ZohoTokenUrl, tokenRequestContent).Result;
    //            var tokenResponseContent = tokenResponse.Content.ReadAsStringAsync().Result;

    //            if (tokenResponse.IsSuccessStatusCode)
    //            {
    //                var json = JsonConvert.DeserializeObject<ZohoTokenReponse>(tokenResponseContent);
    //                return json;
    //            }
    //            else
    //            {
    //                return null;
    //            }
    //        }
    //    }

    //    public static string GetAuthorizationUrl(string clientId, string redirectUrl)
    //    {
    //        var queryParams = new Dictionary<string, string>
    //    {
    //         { "client_id", clientId },
    //         { "response_type", "code" },
    //         { "access_type", "offline" },
    //         { "redirect_uri", redirectUrl },
    //         { "scope", "ZohoBooks.fullaccess.all offline_access" }
    //     };

    //        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"));
    //        return $"{ZohoAuthorizationUrl}?{queryString}";
    //    }
    //    public static Dictionary<string, string> GetZohoChartOfAccounts(string accessToken, string organizationId)
    //    {
    //        var client = new RestClient("https://books.zoho.com/api/v3");
    //        client.AddDefaultHeader("Authorization", $"Zoho-oauthtoken {accessToken}");

    //        var request = new RestRequest("/chartofaccounts", Method.GET);
    //        request.AddParameter("organization_id", organizationId);
    //        var response = client.Execute(request);

    //        if (response.IsSuccessful)
    //        {
    //            var json = JObject.Parse(response.Content);
    //            var zohoAccounts = JsonConvert.DeserializeObject<List<ZohoChartOfAccount>>(json["chartofaccounts"].ToString());

    //            var accountDictionary = zohoAccounts
    //                .Where(x => !string.IsNullOrEmpty(x.account_code)) // Filter out accounts with empty account_code
    //                .ToDictionary(x => x.account_code.ToUpper(), x => x.account_id);

    //            return accountDictionary;
    //        }
    //        else
    //        {
    //            throw new Exception("Error occurred: " + response.ErrorMessage);
    //        }
    //    }

    //}
    //public class ZohoTokenReponse
    //{
    //    [JsonProperty("access_token")]
    //    public string AccessToken { get; set; }

    //    [JsonProperty("expires_in")]
    //    public int AccessTokenExpiresIn { get; set; }

    //    [JsonProperty("refresh_token")]
    //    public string RefreshToken { get; set; }
    //}
    //public class ZohoJournal
    //{
    //    public string journal_date { get; set; }
    //    public string reference_number { get; set; }
    //    public List<ZohoLineItem> line_items { get; set; }

    //}
    //public class ZohoLineItem
    //{
    //    public string account_id { get; set; }
    //    public string debit_or_credit { get; set; }
    //    public double amount { get; set; }
    //    public string description { get; set; }
    //    public string notes { get; set; }
    //    public string account_name { get; set; }
    //    public int item_order { get; set; }
    //}
    //public class ZohoOperationResult
    //{
    //    public bool Success { get; set; }
    //    public string ErrorMessage { get; set; }
    //    public string ExtractedErrorMessage { get; set; }

    //}
    //public class ZohoChartOfAccount
    //{
    //    public string account_id { get; set; }
    //    public string account_code { get; set; }
    //    public string account_name { get; set; }
    //}
}