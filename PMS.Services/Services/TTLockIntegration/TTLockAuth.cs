using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PMS.Common.Classes;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.DTO.ViewModels.TTLockViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.TTLockRequestHandler;

namespace PMS.Services.Services.TTLockIntegration
{
    public class TTLockAuth : ITTLockAuth
    {
        private readonly UnitOfWork<PMSEntities> _uow;
        private readonly ITTLockRequestHandler _requestHandler;
        public TTLockAuth(UnitOfWork<PMSEntities> uow, ITTLockRequestHandler requestHandler)
        {
            _uow = uow;
            _requestHandler = requestHandler;
        }

        public async Task<string> GetAuthTokenAsync()
        {
            var clientIntegration = await _uow.GenericRepository<ClientIntegration>()
                .Table.FirstOrDefaultAsync(x => x.Client_Name.ToLower() == "scienerapp");

            if (DateTime.Parse(clientIntegration.Access_Token_Expiry) <= DateTime.Now)
            {
                var result = await RefreshScienerAccessTokenAsync(clientIntegration);
                if (!result.Success)
                {
                    return null;
                }
            }

            return clientIntegration.Access_Token;
        }

        public async Task<OperationResult> RefreshScienerAccessTokenAsync(ClientIntegration clientIntegrationScienerApp)
        {
            var requestParams = new Dictionary<string, string>
    {
        { "grant_type", "refresh_token" },
        { "clientId", clientIntegrationScienerApp.Client_ID },
        { "clientSecret", clientIntegrationScienerApp.Client_Secret },
        { "refresh_token", clientIntegrationScienerApp.Refresh_Token }
    };

            var tokenResponse = await _requestHandler.PostAsync<TTLockTokenVM>(TTLockResources.ScienerTokenUrl, requestParams);

            if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                clientIntegrationScienerApp.Access_Token = tokenResponse.AccessToken;
                clientIntegrationScienerApp.Refresh_Token = tokenResponse.RefreshToken;
                clientIntegrationScienerApp.Access_Token_Expiry = DateTime.Now.AddSeconds(tokenResponse.AccessTokenExpiresIn).ToString();
                clientIntegrationScienerApp.Refresh_Token_Expiry = DateTime.Now.AddSeconds(tokenResponse.AccessTokenExpiresIn).ToString();

                _uow.GenericRepository<ClientIntegration>().Update(clientIntegrationScienerApp);
                _uow.SaveChanges();

                return new OperationResult { Success = true };
            }
            else
            {
                return new OperationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to refresh access token."
                };
            }
        }

        public async Task<long> GetStartDate(string clientId, string accessToken, int lockId, long date)
        {
            try
            {
                // Create request parameters
                var requestParams = new Dictionary<string, string>
            {
                {"clientId", clientId},
                {"accessToken", accessToken},
                {"lockId", lockId.ToString()},
                {"date", date.ToString()}
            };

                // Construct the query string from parameters
                var queryString = new FormUrlEncodedContent(requestParams);
                var fullUrl = $"{TTLockResources.GetStartDate}?{await queryString.ReadAsStringAsync()}";

                // Make the API call using GET (using PostAsync pattern)
                var response = await _requestHandler.GetAsync<Dictionary<string, long>>(fullUrl);

                // Check if the response contains the "date" field
                if (response != null && response.ContainsKey("date"))
                {
                    return response["date"];
                }
                else
                {
                    throw new Exception("Date not found in the queryDate API response.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetStartDate: {ex.Message}", ex);
            }
        }

        public async Task<int> AddReversedCardNumber(string clientId, string accessToken, int lockId, string cardNumber, long startDate, long endDate, int addType, long date)
        {
            try
            {
                var requestParams = new Dictionary<string, string>
        {
            {"clientId", clientId},
            {"accessToken", accessToken},
            {"lockId", lockId.ToString()},
            {"cardNumber", cardNumber},
            {"startDate", startDate.ToString()},
            {"endDate", endDate.ToString()},
            {"addType", addType.ToString()},
            {"date", date.ToString()}
        };

                var response = await _requestHandler.PostAsync<DTO.ViewModels.TTLockViewModels.ICCardResponse>(
                    TTLockResources.AddForReversedCardNumberUrl, requestParams);

                if (response != null && response.CardId > 0)
                {
                    return response.CardId;
                }
                else
                {
                    throw new Exception("CardId not found in response.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in AddReversedCardNumber: {ex.Message}", ex);
            }
        }

        public async Task<List<RoomDetails>> GetRooms(string baseUrl, string accessToken)
        {
            try
            {
                var requestUrl = $"{baseUrl}/config/rooms";
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.GetAsync(requestUrl);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error: {response.StatusCode}, Content: {errorContent}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                // Preprocess the JSON to remove duplicate status keys
                var cleanedJson = RemoveDuplicateStatus(jsonResponse);

                // Deserialize the cleaned JSON
                var roomWrapper = JsonConvert.DeserializeObject<RoomWrapper>(cleanedJson);
                return roomWrapper.Rooms;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"HTTP request error while fetching rooms: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetRooms: {ex.Message}", ex);
            }
        }

        private string RemoveDuplicateStatus(string jsonResponse)
        {
            // Regex to remove duplicate "status" keys and retain only the first
            var pattern = @"(,\s*""status"":\s*""[^""]*"")(,?\s*""status"":\s*""[^""]*"")";
            return Regex.Replace(jsonResponse, pattern, "$1");
        }

        public async Task<CheckInResponse> CheckInGuest(string room, string moveIn, string moveOut, string enco)
        {
            try
            {
                var requestParams = new Dictionary<string, string>
        {
            { "room", room },
            { "movein", moveIn },
            { "moveout", moveOut },
            { "enco", enco }
        };

                var checkInResponse = await _requestHandler.PostAsyncNew<CheckInResponse>(TTLockResources.CheckInUrl, requestParams);

                return checkInResponse ?? throw new Exception("Invalid response from CheckIn API.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in CheckInGuest: {ex.Message}", ex);
            }
        }

        public async Task<MesserschmittResponse> EMSCheckinOLD(string roomName)
        {
            try
            {
                var requestParams = new Dictionary<string, string>
        {
            { "rm", roomName },
        };

                var EMSResponse = await _requestHandler.PostAsync<MesserschmittResponse>(TTLockResources.EMSUrl, requestParams);

                if (EMSResponse == null)
                {
                    throw new Exception("Received a null response from EMS Check-in API.");
                }

                return EMSResponse;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in EMSCheckin for Room {roomName}: {ex.Message}", ex);
            }
        }

        public async Task<MesserschmittResponse> EMSCheckin(string roomName, string accessToken)
        {
            try
            {
                var requestParams = new Dictionary<string, string>
        {
            { "rm", roomName },
        };
                var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {accessToken}" }, // Add Authorization Header
            { "Content-Type", "application/json" }
        };

                var EMSResponse = await _requestHandler.PostAsync<MesserschmittResponse>(TTLockResources.EMSUrl, requestParams, headers);

                if (EMSResponse == null)
                {
                    throw new Exception("Received a null response from EMS Check-in API.");
                }

                return EMSResponse;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in EMSCheckin for Room {roomName}: {ex.Message}", ex);
            }
        }

        public async Task<MesserschmittResponse> CheckOutMesserschmitt(string roomName, string tid, string accessToken)
        {
            try
            {
                var requestParams = new Dictionary<string, string>
        {
            { "rm", roomName },
            { "tid", tid },
        };
                var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {accessToken}" }, // Add Authorization Header
            { "Content-Type", "application/json" }
        };

                var messerschmittResponse = await _requestHandler.PostAsync<MesserschmittResponse>(TTLockResources.MesserschmittCheckOutUrl, requestParams, headers);

                if (messerschmittResponse == null)
                {
                    throw new Exception("Received a null response from Messerschmitt Check-out API.");
                }

                return messerschmittResponse;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in Messerschmitt Check-out for Room {roomName}: {ex.Message}", ex);
            }
        }


    }

}

