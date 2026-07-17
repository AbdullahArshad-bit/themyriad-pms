using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.Services.Services.Account;
using PMS.StudentApi.Classes;
using PMS.StudentApi.Models;
using System;
using System.Net;
using System.Web.Http;

namespace PMS.StudentApi.Controllers
{
    public class OAuthController : ApiController
    {
        private readonly IAccountService accountService;
        public OAuthController(IAccountService _accountService)
        {
            accountService = _accountService;
        }
        [HttpPost]
        [Route("token")]
        public ApiResponse<object> token(LoginVM model)
        {
            try
            {
                var user = accountService.AuthenticateUser(model.Email, model.Password);
                var token = AuthHelper.GenerateToken(user.Email,user.ID,2);
                var Data = new
                {
                    Id = user.ID,
                    token = token,
                    IsStudent = user.IsStudent
                };
                return new ApiResponse<object>
                {
                    Success = true,
                    Code = (int)HttpStatusCode.OK,
                    Message = "Generated",
                    Data = Data
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

        [HttpPost]
        [ApiAuthorize(Roles ="Jawad")]
        [Route("validate")]
        public ApiResponse<object> Validate(TokenViewModel model)
        {
            try
            {
                
                var valid = AuthHelper.VerifyAndDecodeJwt(model.AccessToken);
                var response = new ApiResponse<object>()
                {
                    Success = true,
                    Code = (int)HttpStatusCode.OK,
                    Message = "Success!",
                    Data = "Valid"
                };
                return response;
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
