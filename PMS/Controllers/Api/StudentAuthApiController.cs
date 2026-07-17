using PMS.Classes;
using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.Services.Services.Account;
using PMS.Services.Services.StudentPortal.Devices;
using System;
using System.Net;
using System.Web.Http;
using System.Web.Http.Cors;

namespace PMS.Controllers.Api
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/Student/Auth")]
    public class StudentAuthApiController : ApiController
    {
        private readonly IAccountService accountService;
        private readonly IStudentDeviceService studentDeviceService;

        public StudentAuthApiController(IAccountService _accountService, IStudentDeviceService _studentDeviceService)
        {
            accountService = _accountService;
            studentDeviceService = _studentDeviceService;
        }

        /// <summary>
        /// Student mobile login. Returns bearer token and optionally registers the FCM device token.
        /// </summary>
        [HttpPost]
        [Route("Login")]
        public ApiResponse<object> Login(StudentLoginVM model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.BadRequest,
                        Message = "Email and Password are required.",
                        Data = null
                    };
                }

                var user = accountService.AuthenticateUser(model.Email, model.Password);
                if (user == null || user.IsStudent != true)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.BadRequest,
                        Message = "Invalid student credentials.",
                        Data = null
                    };
                }

                var token = StudentAuthTokenHelper.GenerateToken(
                    user.ID,
                    user.PersonId,
                    user.Email,
                    StudentAuthTokenHelper.GetExpireMinutes());

                if (!string.IsNullOrWhiteSpace(model.DeviceToken))
                {
                    studentDeviceService.RegisterDevice(
                        user.ID,
                        user.PersonId,
                        model.DeviceToken,
                        model.Platform,
                        model.DeviceId);
                }

                var returnUrl = string.IsNullOrWhiteSpace(model.ReturnURL)
                    ? StudentAuthRedirectHelper.DefaultStudentReturnUrl
                    : model.ReturnURL.Trim();

                return new ApiResponse<object>
                {
                    Success = true,
                    Code = (int)HttpStatusCode.OK,
                    Message = "Login successful.",
                    Data = new
                    {
                        Id = user.ID,
                        PersonId = user.PersonId,
                        token = token,
                        IsStudent = user.IsStudent,
                        returnUrl = returnUrl,
                        redirectUrl = StudentAuthRedirectHelper.BuildWebSignInUrl(token, returnUrl)
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Code = (int)HttpStatusCode.BadRequest,
                    Message = ex.GetBaseException().Message,
                    Data = null
                };
            }
        }
    }
}
