using PMS.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using PMS.Repository.UnitOfWork;
using PMS.EF;
using PMS.Common;
using PMS.DTO;
using PMS.Services.Services.StudentPortal.MovieNights;

namespace PMS.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [BasicAuthenticationApi]
    public class ApiMovieNightsController : ApiController
    {
        private UnitOfWork<PMSEntities> uow;
        private IMovieNightsAdmin MovieNightsAdmin;

        public ApiMovieNightsController(UnitOfWork<PMSEntities> _uow, IMovieNightsAdmin _MovieNightsAdmin)
        {
            uow = _uow;
            MovieNightsAdmin = _MovieNightsAdmin;
        }

        [HttpGet]
        [Route("api/movies")]
        public DTO.ApiResponse<List<DTO.ViewModels.MovieNightsViewModels.MoviesVM>> Movies()
        {

            try
            {
                Globals.BaseUrl = Helper.GetBaseUrl(Request);
                return new ApiResponse<List<DTO.ViewModels.MovieNightsViewModels.MoviesVM>>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "",
                    Data = MovieNightsAdmin.GetMov()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<DTO.ViewModels.MovieNightsViewModels.MoviesVM>>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }


      

    }
}