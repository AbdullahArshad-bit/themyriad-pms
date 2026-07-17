using Newtonsoft.Json;
using PMS.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.TTLockViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using static PMS.Classes.OAuthHelper;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using System;
using System.Configuration;
using System.Linq;

namespace PMS.Controllers
{
    public class ScienerAppController : BaseController
    {
        private static string scienerClientId = ConfigurationManager.AppSettings["scienerClientId"];
        private static string scienerClientSecret = ConfigurationManager.AppSettings["scienerClientSecret"];
        private static string scienerUsername = ConfigurationManager.AppSettings["scienerUsername"];
        private static string scienerPassword = ConfigurationManager.AppSettings["scienerPassword"];

        private readonly UnitOfWork<PMSEntities> uow;
        private readonly OAuthHelper oAuthHelper;

        public ScienerAppController(UnitOfWork<PMSEntities> _uow, OAuthHelper _oAuthHelper)
        {
            uow = _uow;
            oAuthHelper = _oAuthHelper;
        }
        public ActionResult Sciener()
        {
            
            try
            {
                // Exchange authorization code for access token
                var clientIntegration = uow.GenericRepository<ClientIntegration>().Table.FirstOrDefault(x => x.Client_Name == "ScienerApp");
                if (clientIntegration == null || clientIntegration.Client_Name?.ToLower() != "scienerapp")
                {
                    TTLockTokenVM tokenResponse = OAuthHelper.GetScienerAccessToken(scienerClientId, scienerClientSecret, scienerUsername, scienerPassword);

                    if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                    {

                        // Insert a new record
                        clientIntegration = new ClientIntegration
                        {
                            Client_Name = "ScienerApp",
                            Access_Token = tokenResponse.AccessToken,
                            Access_Token_Expiry = DateTime.Now.AddSeconds(tokenResponse.AccessTokenExpiresIn).ToString(),
                            Refresh_Token = tokenResponse.RefreshToken,
                            Refresh_Token_Expiry = DateTime.Now.AddDays(30).ToString() // Set refresh token expiry to 30 days
                        };
                        uow.GenericRepository<ClientIntegration>().Insert(clientIntegration);
                        uow.SaveChanges();
                    }

                }
                return RedirectToAction("TriggerScienerCall", "ScienerApp");

            }
            catch (Exception)
            {
                return RedirectToAction("Error", "Home");
            }
        }

        public async Task<ActionResult> TriggerScienerCall()
        {
            var clientIntegration = uow.GenericRepository<ClientIntegration>().Table.FirstOrDefault(x => x.Client_Name == "ScienerApp");

            if (clientIntegration != null && !string.IsNullOrEmpty(clientIntegration.Access_Token))
            {
                DateTime date = clientIntegration.Last_Journal_Entry.HasValue ? clientIntegration.Last_Journal_Entry.Value : DateTime.Today.AddDays(-1);

                if (DateTime.Parse(clientIntegration.Access_Token_Expiry) <= DateTime.Now)
                {
                    var refreshTokenResult = await oAuthHelper.CheckAndRefreshScienerAccessToken(clientIntegration);

                    if (!refreshTokenResult.Success)
                    {
                        return RedirectToAction("Error", "Home");
                    }
                }

                var getGatewayListResult = await GetGatewayList(scienerClientId, clientIntegration.Access_Token, 1, 1000, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                var gatewayId = getGatewayListResult.FirstOrDefault()?.GatewayId;

                var lockList = await GetLockListOfGateway(scienerClientId, clientIntegration.Access_Token, gatewayId.Value, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                var allLocks = lockList.ToList();
                var lockId = allLocks.FirstOrDefault()?.LockId;

                var icCardsList = await GetAllICCardsOfLock(scienerClientId, clientIntegration.Access_Token, lockId.Value, 1, 1000, 1, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                var reversedCardNumber = await AddReversedCardNumber(scienerClientId, clientIntegration.Access_Token, lockId.Value, "12345678", 0, 0, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                var cardId = reversedCardNumber;

                var deleteCardAsync = await DeleteCardAsync(scienerClientId, clientIntegration.Access_Token, lockId.Value, cardId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());


                //var lockListByHotelResult = await LocklistByHotel(scienerClientId, clientIntegration.Access_Token, pageNo: 1, pageSize: 1000, date: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                //var lockDetailResponse = await LockDetailById(scienerClientId, clientIntegration.Access_Token, lockId: 7465116, date: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());


                return RedirectToAction("Dashboard", "Home");
            }

            return RedirectToAction("Dashboard", "Home");
        }

        public static HttpClient HttpClientInstance = new HttpClient();

        public static async Task<List<GeteWayDetail>> GetGatewayList(string clientId, string accessToken, int pageNo, int pageSize, long date)
        {
            try
            {
                string apiUrl = $"https://euapi.sciener.com/v3/gateway/list";
                var requestParameters = new Dictionary<string, string>
        {
            {"clientId", clientId},
            {"accessToken", accessToken},
            {"pageNo", pageNo.ToString()},
            {"pageSize", pageSize.ToString()},
            {"date", date.ToString()}
        };

                var queryString = new FormUrlEncodedContent(requestParameters);
                var fullUrl = $"{apiUrl}?{await queryString.ReadAsStringAsync()}";

                var response = await HttpClientInstance.GetAsync(fullUrl).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var gatewaylistResponse = JsonConvert.DeserializeObject<GateWayResponse>(responseContent);

                    return gatewaylistResponse?.list?.Select(list => new GeteWayDetail
                    {
                        GateWayMac = list.gatewayMac,
                        LockNum = list.lockNum,
                        GatewayName = list.gatewayName,
                        IsOnline = list.isOnline,
                        GatewayVersion = list.gatewayVersion,
                        GatewayId = list.gatewayId
                    }).ToList() ?? new List<GeteWayDetail>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new HttpRequestException($"Error: {response.StatusCode} - {response.ReasonPhrase}. Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetGatewayList: {ex.Message}", ex);
            }
        }

        public static async Task<List<LockListDetail>> GetLockListOfGateway(string clientId, string accessToken, int gatewayId, long date)
        {
            try
            {
                string apiUrl = $"https://euapi.sciener.com/v3/gateway/listLock";
                var requestParameters = new Dictionary<string, string>
        {
            {"clientId", clientId},
            {"accessToken", accessToken},
            {"gatewayId", gatewayId.ToString()},
            {"date", date.ToString()}
        };

                var queryString = new FormUrlEncodedContent(requestParameters);
                var fullUrl = $"{apiUrl}?{await queryString.ReadAsStringAsync()}";

                var response = await HttpClientInstance.GetAsync(fullUrl).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var lockListResponse = JsonConvert.DeserializeObject<LockListResponse>(responseContent);

                    return lockListResponse?.list?.Select(list => new LockListDetail
                    {
                        LockId = list.lockId,
                        LockMac = list.lockMac,
                        LockName = list.lockName,
                        LockAlias = list.lockAlias,
                        Rssi = list.rssi,
                        UpdateDate = list.updateDate
                    }).ToList() ?? new List<LockListDetail>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new HttpRequestException($"Error: {response.StatusCode} - {response.ReasonPhrase}. Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetLockListOfGateway: {ex.Message}", ex);
            }
        }


        public static async Task<List<ICCardDetail>> GetAllICCardsOfLock(string clientId, string accessToken, int lockId, int pageNo, int pageSize, int orderBy, long date)
        {
            try
            {
                string apiUrl = $"https://euapi.sciener.com/v3/identityCard/list";
                var requestParameters = new Dictionary<string, string>
        {
            {"clientId", clientId},
            {"accessToken", accessToken},
            {"lockId", lockId.ToString()},
            {"pageNo", pageNo.ToString()},
            {"pageSize", pageSize.ToString()},
            {"orderBy", orderBy.ToString()},
            {"date", date.ToString()}
        };

                var queryString = new FormUrlEncodedContent(requestParameters);
                var fullUrl = $"{apiUrl}?{await queryString.ReadAsStringAsync()}";

                var response = await HttpClientInstance.GetAsync(fullUrl).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var iCCardResponse = JsonConvert.DeserializeObject<OAuthHelper.ICCardResponse>(responseContent);

                    return iCCardResponse?.list?.Select(list => new ICCardDetail
                    {
                        CardId = list.cardId,
                        LockId = list.lockId,
                        CardNumber = list.cardNumber,
                        CardName = list.cardName,
                        StartDate = list.startDate,
                        EndDate = list.endDate,
                        CreateDate = list.createDate,
                        SenderUsername = list.senderUsername,
                        CardType = list.cardType
                    }).ToList() ?? new List<ICCardDetail>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new HttpRequestException($"Error: {response.StatusCode} - {response.ReasonPhrase}. Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetLockListOfGateway: {ex.Message}", ex);
            }
        }


        public static async Task<int> AddReversedCardNumber(string clientId, string accessToken, int lockId, string cardNumber, long startDate, long endDate, long date)
        {
            try
            {
                // API URL for POST request
                string apiUrl = "https://euapi.sciener.com/v3/identityCard/addForReversedCardNumber";

                // Create the request parameters
                var requestParameters = new Dictionary<string, string>
        {
            {"clientId", clientId},
            {"accessToken", accessToken},
            {"lockId", lockId.ToString()},
            {"cardNumber", cardNumber},
            {"startDate", startDate.ToString()},
            {"endDate", endDate.ToString()},
            {"date", date.ToString()}
        };

                // Convert parameters to FormUrlEncodedContent for a POST request
                var content = new FormUrlEncodedContent(requestParameters);

                // Make the POST request
                var response = await HttpClientInstance.PostAsync(apiUrl, content).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    // Read the response content as a string
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    // Deserialize the JSON response to get cardId
                    var iCCardResponse = JsonConvert.DeserializeObject<OAuthHelper.ICCardResponse>(responseContent);

                    // Return the cardId
                    return iCCardResponse?.CardId ?? 0;
                }
                else
                {
                    // Handle unsuccessful response
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new HttpRequestException($"Error: {response.StatusCode} - {response.ReasonPhrase}. Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in AddReversedCardNumber: {ex.Message}", ex);
            }
        }

        public static async Task<DeleteCardResponse> DeleteCardAsync(string clientId, string accessToken, int lockId, int cardId, long date)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    string apiUrl = "https://euapi.sciener.com/v3/identityCard/delete";

                    // Create request parameters
                    var requestParameters = new Dictionary<string, string>
            {
                {"clientId", clientId},
                {"accessToken", accessToken},
                {"lockId", lockId.ToString()},
                {"cardId", cardId.ToString()},
                {"date", date.ToString()}
            };

                    // Format parameters as application/x-www-form-urlencoded
                    var content = new FormUrlEncodedContent(requestParameters);

                    // Send POST request
                    var response = await httpClient.PostAsync(apiUrl, content).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return JsonConvert.DeserializeObject<DeleteCardResponse>(responseContent);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new HttpRequestException($"Error: {response.StatusCode} - {response.ReasonPhrase}. Content: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in DeleteCardAsync: {ex.Message}", ex);
            }
        }

        public static async Task<ChangePeriodResponse> ChangePeriodAsync(string clientId, string accessToken, int lockId, int cardId, long startDate, long endDate, int changeType, List<CyclicConfig> cyclicConfig, long date)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    string apiUrl = "https://euapi.sciener.com/v3/identityCard/changePeriod";

                    // Create request parameters
                    var requestParameters = new Dictionary<string, string>
            {
                {"clientId", clientId},
                {"accessToken", accessToken},
                {"lockId", lockId.ToString()},
                {"cardId", cardId.ToString()},
                {"startDate", startDate.ToString()},
                {"endDate", endDate.ToString()},
                {"changeType", changeType.ToString()},
                {"date", date.ToString()},
                {"cyclicConfig", JsonConvert.SerializeObject(cyclicConfig)} // Convert cyclicConfig to JSON string
            };

                    // Format parameters as application/x-www-form-urlencoded
                    var content = new FormUrlEncodedContent(requestParameters);

                    // Send POST request
                    var response = await httpClient.PostAsync(apiUrl, content).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return JsonConvert.DeserializeObject<ChangePeriodResponse>(responseContent);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new HttpRequestException($"Error: {response.StatusCode} - {response.ReasonPhrase}. Content: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in ChangePeriodAsync: {ex.Message}", ex);
            }
        }

        public static async Task<RenameResponse> RenameCardAsync(string clientId, string accessToken, int lockId, int cardId, string cardName, long date)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    string apiUrl = "https://euapi.sciener.com/v3/identityCard/rename";

                    // Create request parameters
                    var requestParameters = new Dictionary<string, string>
            {
                {"clientId", clientId},
                {"accessToken", accessToken},
                {"lockId", lockId.ToString()},
                {"cardId", cardId.ToString()},
                {"cardName", cardName},
                {"date", date.ToString()}
            };

                    // Format parameters as application/x-www-form-urlencoded
                    var content = new FormUrlEncodedContent(requestParameters);

                    // Send POST request
                    var response = await httpClient.PostAsync(apiUrl, content).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return JsonConvert.DeserializeObject<RenameResponse>(responseContent);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new HttpRequestException($"Error: {response.StatusCode} - {response.ReasonPhrase}. Content: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in RenameCardAsync: {ex.Message}", ex);
            }
        }


        public static async Task<List<LockDetails>> LocklistByHotel(string clientId, string accessToken, int pageNo, int pageSize, long date)
        {
            using (var httpClient = new HttpClient())
            {
                string apiUrl = $"https://api.sciener.com/v3/lock/listByHotel";

                var requestParameters = new Dictionary<string, string>
        {
            {"clientId", clientId},
            {"accessToken", accessToken},
            {"pageNo", pageNo.ToString()},
            {"pageSize", pageSize.ToString()},
            {"date", date.ToString()}
        };

                var queryString = string.Join("&", requestParameters.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
                var fullUrl = $"{apiUrl}?{queryString}";

                var response = await httpClient.GetAsync(fullUrl);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var locklistResponse = JsonConvert.DeserializeObject<LocklistResponse>(responseContent);

                    return locklistResponse?.list?.Select(lockItem => new LockDetails
                    {
                        LockId = lockItem.lockId,
                        LockMac = lockItem.lockMac,
                        LockAlias = lockItem.lockAlias,
                        LockName = lockItem.lockName
                    }).ToList();
                }
                else
                {
                    // Handle error
                    throw new HttpRequestException($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
        }

        public static async Task<LockDetailDetails> LockDetailById(string clientId, string accessToken, long lockId, long date)
        {
            using (var httpClient = new HttpClient())
            {
                string apiUrl = $"https://euapi.sciener.com/v3/lock/detail";

                // You can customize the request parameters as needed
                var requestParameters = new Dictionary<string, string>
        {
            {"clientId", clientId},
            {"accessToken", accessToken},
            {"lockId", lockId.ToString()},
            {"date", date.ToString()}
        };

                var queryString = string.Join("&", requestParameters.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
                var fullUrl = $"{apiUrl}?{queryString}";

                var response = await httpClient.GetAsync(fullUrl);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var lockDetailResponse = JsonConvert.DeserializeObject<LockDetailResponse>(responseContent);

                    if (lockDetailResponse?.lockDetail != null)
                    {
                        return new LockDetailDetails
                        {
                            ModelNum = lockDetailResponse.lockDetail.modelNum,
                            LockMac = lockDetailResponse.lockDetail.lockMac,
                            LockKey = lockDetailResponse.lockDetail.lockKey,
                            LockName = lockDetailResponse.lockDetail.lockName
                        };
                    }
                    else
                    {
                        throw new JsonSerializationException("LockDetail is null or not in the expected format.");
                    }
                }
                else
                {
                    // Handle error
                    throw new HttpRequestException($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
        }

    }
}