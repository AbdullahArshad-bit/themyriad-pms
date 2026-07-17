using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.Services.Services.Integration;
using PMSAPI.Classes;
using PMSAPI.Models;
using System;
using System.Net;
using System.Web.Http;

namespace PMSAPI.Controllers
{
    /// <summary>
    /// Issues JWT access tokens for system-to-system OAuth client credentials.
    /// </summary>
    public class OAuthController : ApiController
    {
        private readonly IIntegrationAuthService integrationAuthService;

        public OAuthController(IIntegrationAuthService _integrationAuthService)
        {
            integrationAuthService = _integrationAuthService;
        }

        [HttpPost]
        [Route("auth/token")]
        public ApiResponse<TokenResponse> Token(AuthTokenRequest model)
        {
            try
            {
                if (model == null)
                {
                    return Fail("Request body is required.");
                }

                var grantType = model.grant_type?.Trim();
                if (!string.Equals(grantType, "client_credentials", StringComparison.OrdinalIgnoreCase))
                {
                    return Fail("grant_type must be client_credentials.", HttpStatusCode.BadRequest);
                }

                var clientId = model.client_id;
                var clientSecret = model.client_secret;
                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                {
                    return Fail("client_id and client_secret are required for client_credentials grant type.");
                }

                var client = integrationAuthService.AuthenticateClient(clientId, clientSecret);
                if (client == null)
                {
                    return Fail("Invalid client_id or client_secret.", HttpStatusCode.Unauthorized);
                }

                var expireMinutes = AuthHelper.DefaultExpireMinutes;
                var token = AuthHelper.GenerateTokenForClient(client.Client_ID, expireMinutes);
                integrationAuthService.StoreAccessToken(client, token, DateTime.UtcNow.AddMinutes(expireMinutes));

                return new ApiResponse<TokenResponse>
                {
                    Success = true,
                    Code = (int)HttpStatusCode.OK,
                    Message = "Token generated.",
                    Data = new TokenResponse
                    {
                        AccessToken = token,
                        TokenType = "Bearer",
                        ExpiresIn = expireMinutes * 60,
                        UserId = 0,
                        UserName = client.Client_ID,
                        Email = client.Client_ID,
                        AssignedLocations = null
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<TokenResponse>
                {
                    Success = false,
                    Code = (int)HttpStatusCode.Unauthorized,
                    Message = ex.GetBaseException().Message,
                    Data = null
                };
            }
        }

        private static ApiResponse<TokenResponse> Fail(string message, HttpStatusCode code = HttpStatusCode.BadRequest)
        {
            return new ApiResponse<TokenResponse>
            {
                Success = false,
                Code = (int)code,
                Message = message,
                Data = null
            };
        }
    }
}
