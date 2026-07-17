using PMS.Classes;
using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.Filters;
using PMS.Services.Services.StudentPortal.Devices;
using System;
using System.Net;
using System.Web.Http;
using System.Web.Http.Cors;

namespace PMS.Controllers.Api
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [StudentBearerAuthorize]
    [RoutePrefix("api/Student/Device")]
    public class StudentDeviceApiController : ApiController
    {
        private readonly IStudentDeviceService studentDeviceService;

        public StudentDeviceApiController(IStudentDeviceService _studentDeviceService)
        {
            studentDeviceService = _studentDeviceService;
        }

        [HttpPost]
        [Route("Register")]
        public ApiResponse<object> Register(RegisterStudentDeviceVM model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.DeviceToken))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.BadRequest,
                        Message = "DeviceToken is required.",
                        Data = null
                    };
                }

                var userId = StudentAuthTokenHelper.GetAuthenticatedUserId(Request);
                var personId = StudentAuthTokenHelper.GetAuthenticatedPersonId(Request);
                if (userId <= 0)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.Unauthorized,
                        Message = "Invalid or expired token.",
                        Data = null
                    };
                }

                var registered = studentDeviceService.RegisterDevice(
                    userId,
                    personId,
                    model.DeviceToken,
                    model.Platform,
                    model.DeviceId);

                return new ApiResponse<object>
                {
                    Success = registered,
                    Code = registered ? (int)HttpStatusCode.OK : (int)HttpStatusCode.BadRequest,
                    Message = registered ? "Device registered successfully." : "Unable to register device.",
                    Data = new { UserId = userId, PersonId = personId }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Code = (int)HttpStatusCode.InternalServerError,
                    Message = ex.GetBaseException().Message,
                    Data = null
                };
            }
        }

        [HttpPost]
        [Route("Unregister")]
        public ApiResponse<object> Unregister(UnregisterStudentDeviceVM model)
        {
            try
            {
                var userId = StudentAuthTokenHelper.GetAuthenticatedUserId(Request);
                if (userId <= 0)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.Unauthorized,
                        Message = "Invalid or expired token.",
                        Data = null
                    };
                }

                studentDeviceService.UnregisterDevice(userId, model?.DeviceToken);

                return new ApiResponse<object>
                {
                    Success = true,
                    Code = (int)HttpStatusCode.OK,
                    Message = "Device unregistered successfully.",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Code = (int)HttpStatusCode.InternalServerError,
                    Message = ex.GetBaseException().Message,
                    Data = null
                };
            }
        }
    }
}
