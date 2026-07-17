using PMS.Common.Filters;
using PMS.Services.Services.News;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PMS.Common.Classes;

namespace PMS.Controllers
{
    public class NewsController : BaseController
    {
        private readonly INewsService newsService;

        public NewsController(INewsService _newsService)
        {
            newsService = _newsService;
        }

        [AuthorizeUser(Roles = AppUserRoles.view_news)]
        [Route("all-news")]
        public ActionResult All()
        {
            var news = newsService.GetAllNews();
            ViewBag.News = news;
            return View();
        }

        [AuthorizeUser(Roles = AppUserRoles.add_news)]
        [Route("add-news/{id?}")]
        public ActionResult News(int? id)
        {
            PMS.DTO.ViewModels.NewsViewModels.NewsVM model = new DTO.ViewModels.NewsViewModels.NewsVM();
            model.NewsDate = DateTime.Today;
            model.IsActive = true;
            model.IsEnable = true;
            model.NewsCategories = newsService.GetNewsCatogries();


            if (id != null)
            {
                var news = newsService.GetNewsById(Convert.ToInt32(id));
                model.NewsId = news.NewsID;
                model.Heading = news.Heading;
                model.Ar_Heading = news.Ar_Heading;
                model.Headline = news.Headline;
                model.Ar_Headline = news.Ar_Headline;
                model.NewsDate = news.NewsDate;
                model.SourceLink = news.SourceLink;
                model.SelectedCategory = news.NewsCategoryID;
                model.IsActive = news.IsActive;
                model.IsEnable = news.IsEnable;

                model.ThumbnailImageUrl = news.Thumbnail;
                model.Ar_ThumbnailImageUrl = news.Ar_Thumbnail;
                model.HeadlineImageUrl = news.HeadlineImage;
                model.Ar_HeadlineImageUrl = news.Ar_HeadlineImage;
            }

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.add_news)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddNews(DTO.ViewModels.NewsViewModels.NewsVM newsVM)
        {
            try
            {
                if (newsVM.NewsId > 0)
                {
                    ModelState.Remove("ThumbnailImage");
                }

                if (ModelState.IsValid)
                {
                    if (newsVM.NewsId > 0)
                    {
                        if (newsService.UpdateNews(newsVM))
                        {
                            TempData["success"] = "News updated succesfully";
                            return RedirectToAction("All");
                        }

                        ViewBag.error = "Something went wrong, news not updated.";
                    }
                    else
                    {
                        if (newsService.AddNew(newsVM))
                        {
                            TempData["success"] = "News added succesfully";
                            return RedirectToAction("All");
                        }

                        ViewBag.error = "Something went wrong, news not saved.";
                    }
                }
                else
                {
                    ViewBag.error = Common.Helper.GetModelError(ModelState);
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }

            newsVM.NewsCategories = newsService.GetNewsCatogries();
            return View("News", newsVM);
        }

        [AuthorizeUser(Roles = AppUserRoles.view_news_detail + "," + AppUserRoles.add_news_detail)]
        [Route("news/detail/{id}/{newsDetailId?}")]
        public ActionResult NewsDetail(int id, int? newsDetailId)
        {
            DTO.ViewModels.NewsViewModels.NewsDetailVM model = new DTO.ViewModels.NewsViewModels.NewsDetailVM();
            model.NewsId = id;
            model.NewsDetailId = (newsDetailId == null) ? 0 : Convert.ToInt32(newsDetailId);
            model.ContentTypes = newsService.GetContentType();

            //model.SelectedContentType = "Image";

            if (newsDetailId != null)
            {
                var news = newsService.GetNewsDetailById(Convert.ToInt32(newsDetailId));
                model.SelectedContentType = news.ContentType;
                if (news.ContentType.ToLower() == "image")
                {
                    model.ImageUrl = news.ContentValue;
                    model.Ar_ImageUrl = news.Ar_ContentValue;
                }
                else
                {
                    model.ContentValue = news.ContentValue;
                    model.Ar_ContentValue = news.Ar_ContentValue;
                }
            }
            else
            {
                var newsDetail = newsService.GetNewsDetails(id);
                ViewBag.NewsDetail = newsDetail;
            }

            return View(model);
        }


        [AuthorizeUser(Roles = AppUserRoles.add_news_detail)]
        [HttpPost]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult AddNewsDetail(DTO.ViewModels.NewsViewModels.NewsDetailVM model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model.NewsDetailId > 0)
                    {
                        //update
                        if (newsService.UpdateNewsDetail(model))
                        {
                            TempData["success"] = "News detail updated successfully.";
                        }
                        else
                        {
                            TempData["error"] = "Sorry something went wrong, news detail not updated.";
                        }
                    }
                    else
                    {
                        //insert
                        if (newsService.AddNewsDetail(model))
                        {
                            TempData["success"] = "News detail saved successfully.";
                        }
                        else
                        {
                            TempData["error"] = "Sorry something went wrong, news detail not saved.";
                        }
                    }
                }
                else
                {
                    TempData["error"] = Common.Helper.GetModelError(ModelState);
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("NewsDetail", new { id = model.NewsId });
        }

        [AuthorizeUser(Roles = AppUserRoles.delete_news_detail)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteNewsDetail(int newsDetailId, int newsId)
        {
            try
            {
                if (newsService.DeleteNewsDetail(newsDetailId))
                {
                    TempData["success"] = "News detail deleted successfully.";
                }
                else
                {
                    TempData["error"] = "Sorry something went wrong, news detail not deleted.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("NewsDetail", new { id = newsId });
        }


    }
}