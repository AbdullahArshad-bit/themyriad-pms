using PMS.DTO;
using PMSAPI.Classes;
using System.Net;
using System.Web.Http;

namespace PMSAPI.Controllers.Api
{
    /// <summary>
    /// Base controller for all integration endpoints. Applies JWT authorization and
    /// provides consistent ApiResponse helpers.
    /// </summary>
    [ApiAuthorize]
    public abstract class IntegrationApiController : ApiController
    {
        // ── untyped helpers (backwards-compat for ApiResponse<object> returns) ──

        protected static ApiResponse<object> Success(object data, string message = "Success")
        {
            return new ApiResponse<object>
            {
                Success = true,
                Code = (int)HttpStatusCode.OK,
                Message = message,
                Data = data
            };
        }

        protected static ApiResponse<object> NotFound(string message)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Code = (int)HttpStatusCode.NotFound,
                Message = message,
                Data = null
            };
        }

        protected static ApiResponse<object> Fail(string message, HttpStatusCode code = HttpStatusCode.BadRequest)
        {
            return new ApiResponse<object>
            {
                Success = false,
                Code = (int)code,
                Message = message,
                Data = null
            };
        }

        // ── typed helpers (used by controllers with specific return types) ──

        protected static ApiResponse<T> Success<T>(T data, string message = "Success") where T : class
        {
            return new ApiResponse<T>
            {
                Success = true,
                Code = (int)HttpStatusCode.OK,
                Message = message,
                Data = data
            };
        }

        protected static ApiResponse<T> NotFound<T>(string message) where T : class
        {
            return new ApiResponse<T>
            {
                Success = false,
                Code = (int)HttpStatusCode.NotFound,
                Message = message,
                Data = null
            };
        }

        protected static ApiResponse<T> Fail<T>(string message, HttpStatusCode code = HttpStatusCode.BadRequest) where T : class
        {
            return new ApiResponse<T>
            {
                Success = false,
                Code = (int)code,
                Message = message,
                Data = null
            };
        }
    }
}
