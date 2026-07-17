using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using PMS.DTO;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.Services.Services.UserManage;
using PMS.StudentApi.Classes;

namespace PMS.StudentApi.Controllers.Api
{
    [ApiAuthorize]
    public class UserController : ApiController
    {
        private readonly IUserManageService UserManageService;
        public UserController(IUserManageService _UserManageService)
        {
            UserManageService = _UserManageService;
        }
        [HttpGet]
         public HttpResponseMessage Get(int id)
        {
            var objects = UserManageService.GetById(id);
            return Request.CreateResponse((HttpStatusCode)objects.Code, objects);
        }
        [HttpPost]
        public HttpResponseMessage updateImage(int id)
        {            
            var response= UserManageService.updateImage(id, HttpContext.Current.Request.Files);
            return Request.CreateResponse((HttpStatusCode)response.Code,response);
        }
    }

}
