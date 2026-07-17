using PMS.Common;
using PMS.DTO;
using PMS.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using PMS.EF;
using PMS.Services.Services.News;
using PMS.Repository.UnitOfWork;

namespace PMS.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [BasicAuthenticationApi]
    public class ApiNewsController : ApiController
    {

        private UnitOfWork<PMSEntities> uow;
        private INewsService newsService;
        public ApiNewsController(UnitOfWork<PMSEntities> _uow, INewsService _newsService )
        {
            uow = _uow;
            newsService = _newsService;
        }

        [HttpGet]
        [Route("api/getnews")]
        public DTO.ApiResponse<List<DTO.ViewModels.NewsViewModels.News>> GetHomeNews(int newsCategoryID, int count, int newsId) 
        {
            Globals.BaseUrl = Helper.GetBaseUrl(Request);

            try
            {
                Globals.BaseUrl = Helper.GetBaseUrl(Request);
                return new ApiResponse<List<DTO.ViewModels.NewsViewModels.News>>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "",
                    Data = newsService.GetAllNews(newsCategoryID, count, newsId)
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<DTO.ViewModels.NewsViewModels.News>>
                {
                    Success = false,
                    Code = Convert.ToInt32(HttpStatusCode.InternalServerError),
                    Message = ex.Message,
                    Data = null
                };
            }
        }

     




        [HttpGet]
        [Route("api/getapinewsbyid/{id}")]
        public DTO.ApiResponse<DTO.ViewModels.NewsViewModels.News> GetApiNewsByID(int id)
        {
            Globals.BaseUrl = Helper.GetBaseUrl(Request);

            try
            {
                Globals.BaseUrl = Helper.GetBaseUrl(Request);
                return new ApiResponse<DTO.ViewModels.NewsViewModels.News>
                {
                    Success = true,
                    Code = Convert.ToInt32(HttpStatusCode.OK),
                    Message = "",
                    Data = newsService.GetApiNewsById(id)
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DTO.ViewModels.NewsViewModels.News>
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