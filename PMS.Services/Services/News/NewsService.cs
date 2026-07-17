using PMS.Common;
using PMS.DTO.ViewModels.NewsViewModels;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.EF;
using PMS.DTO;

using PMS.Repository.UnitOfWork;

namespace PMS.Services.Services.News
{
    public class NewsService : INewsService
    {
        private UnitOfWork<PMSEntities> uow;
        public NewsService(UnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
        }
        public List<NewsCategory> GetNewsCatogries()
        {
            return uow.GenericRepository<NewsCategory>().GetAll().ToList();
        }
        public bool AddNew(NewsVM newsVM)
        {
            EF.News news = new EF.News();
            try
            {
                ImageResult result = new ImageResult();

                Common.ImageUpload upload = new Common.ImageUpload()
                {
                    Width = 570,
                    Height = 403,
                    Quality = 80
                };
                result = upload.RenameUploadFile(newsVM.ThumbnailImage);

                if (!result.Success)
                    return false;

                news.Thumbnail = result.ImageName;

                if (newsVM.HeadlineImage != null)
                {
                    upload = new Common.ImageUpload()
                    {
                        Width = 811,
                        Height = 350,
                        Quality = 80
                    };
                    result = upload.RenameUploadFile(newsVM.HeadlineImage);

                    if (!result.Success)
                        return false;

                    news.HeadlineImage = result.ImageName;
                }

                news.NewsCategoryID = newsVM.SelectedCategory;
                news.Heading = newsVM.Heading;
                news.Headline = newsVM.Headline;
                news.NewsDate = newsVM.NewsDate;
                news.SourceLink = newsVM.SourceLink;
                news.IsEnable = newsVM.IsEnable;
                news.IsActive = newsVM.IsActive;
                news.CreatedDate = DateTime.Now;

                uow.GenericRepository<EF.News>().Insert(news);
                uow.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }


            return true;
        }

        public bool UpdateNews(NewsVM newsVM)
        {
            EF.News news = new EF.News();
            try
            {
                news = GetNewsById(newsVM.NewsId);

                ImageResult result = new ImageResult();

                Common.ImageUpload upload = new Common.ImageUpload()
                {
                    Width = 570,
                    Height = 403,
                    Quality = 80
                };

                if (newsVM.ThumbnailImage != null)
                {
                    result = upload.RenameUploadFile(newsVM.ThumbnailImage);
                    if (!result.Success)
                        return false;
                    news.Thumbnail = result.ImageName;
                }



                upload = new Common.ImageUpload()
                {
                    Width = 570,
                    Height = 403,
                    Quality = 80
                };

                if (newsVM.Ar_ThumbnailImage != null)
                {
                    result = upload.RenameUploadFile(newsVM.Ar_ThumbnailImage);
                    if (!result.Success)
                        return false;
                    news.Ar_Thumbnail = result.ImageName;
                }




                upload = new Common.ImageUpload()
                {
                    Width = 811,
                    Height = 350,
                    Quality = 80
                };

                if (newsVM.HeadlineImage != null)
                {
                    result = upload.RenameUploadFile(newsVM.HeadlineImage);
                    if (!result.Success)
                        return false;
                    news.HeadlineImage = result.ImageName;
                }



                upload = new Common.ImageUpload()
                {
                    Width = 811,
                    Height = 350,
                    Quality = 80
                };

                if (newsVM.Ar_HeadlineImage != null)
                {
                    result = upload.RenameUploadFile(newsVM.Ar_HeadlineImage);
                    if (!result.Success)
                        return false;
                    news.Ar_HeadlineImage = result.ImageName;
                }



                news.NewsCategoryID = newsVM.SelectedCategory;
                news.Heading = newsVM.Heading;
                news.Ar_Heading = newsVM.Ar_Heading;
                news.Headline = newsVM.Headline;
                news.Ar_Headline = newsVM.Ar_Headline;
                news.NewsDate = newsVM.NewsDate;
                news.SourceLink = newsVM.SourceLink;
                news.IsEnable = newsVM.IsEnable;
                news.IsActive = newsVM.IsActive;

                uow.GenericRepository<EF.News>().Update(news);
                uow.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }


            return true;
        }

        public List<AllNewsVM> GetAllNews()
        {
            return (from news in uow.Context.News
                    join cat in uow.Context.NewsCategories
                    on news.NewsCategoryID equals cat.NewsCategoryID
                    where news.IsEnable == true
                    select new AllNewsVM
                    {
                        NewsID = news.NewsID,
                        NewsCategoryID = news.NewsCategoryID,
                        Heading = news.Heading,
                        Headline = news.Headline,
                        NewsCategory = cat.Description,
                        Thumbnail = news.Heading,
                        HeadlineImage = news.Heading,
                        SourceLink = news.Heading,
                        IsActive = news.IsActive,
                        IsEnable = news.IsEnable,
                        NewsDate = news.NewsDate,
                        CreatedDate = news.CreatedDate
                    }).ToList();
        }





        public PMS.EF.News GetNewsById(int id)
        {
            return uow.GenericRepository<EF.News>().Table.Where(x => x.NewsID == id && x.IsEnable == true).FirstOrDefault();
        }


        public List<DTO.ViewModels.NewsViewModels.News> GetAllNews(int newsCategoryID, int count, int newsId)
        {
            var newsList = new List<DTO.ViewModels.NewsViewModels.News>();
            var newsRepo = uow.GenericRepository<EF.News>();
            var newsDetailRepo = uow.GenericRepository<EF.NewsDetail>();
            var news = new List<EF.News>();

            var query = newsRepo.Table.Where(x => x.IsEnable == true && x.IsActive == true).OrderByDescending(x => x.NewsDate) as IQueryable<EF.News>;

            if (newsCategoryID > 0)
                query = query.Where(x => x.NewsCategoryID == newsCategoryID);

            if (newsId > 0)
                query = query.Where(x => x.NewsID == newsId);

            if (count > 0)
                query = query.Take(count);

            news = query.ToList();

            foreach (var newsItem in news)
            {
                DTO.ViewModels.NewsViewModels.News n = new DTO.ViewModels.NewsViewModels.News();
                n.NewsId = newsItem.NewsID;
                n.NewsCategoryID = newsItem.NewsCategoryID;
                n.Heading = newsItem.Heading;

                if (newsItem.Ar_Heading != null)
                    n.Ar_Heading = newsItem.Ar_Heading;
                else
                    n.Ar_Heading = newsItem.Heading;

                if (newsItem.Headline.Length > 120)
                    n.Headline = newsItem.Headline.Substring(0, 119);
                else
                    n.Headline = newsItem.Headline;
                try
                {
                    if (newsItem.Ar_Headline != null) {
                        if (newsItem.Ar_Headline.Length > 120)
                            n.Ar_Headline = newsItem.Ar_Headline.Substring(0, 119);
                        else
                            n.Ar_Headline = newsItem.Ar_Headline;
                    }
                    else
                    {
                        if (newsItem.Headline.Length > 120)
                            n.Ar_Headline = newsItem.Headline.Substring(0, 119);
                        else
                            n.Ar_Headline = newsItem.Headline;

                    }
                       



                }
                catch (Exception)
                {

                }


                n.Headline = n.Headline + "...";


                n.Ar_Headline = n.Ar_Headline + "...";

                n.NewsDate = newsItem.NewsDate;
                n.ThumbnailUrl = newsItem.Thumbnail;

                if (newsItem.Ar_Thumbnail != null)
                    n.Ar_ThumbnailUrl = newsItem.Ar_Thumbnail;
                else
                    n.Ar_ThumbnailUrl = newsItem.Thumbnail;

                

                n.HeadlineImageUrl = newsItem.HeadlineImage;

                if (newsItem.Ar_Thumbnail != null)
                    n.Ar_HeadlineImageUrl = newsItem.Ar_HeadlineImage;
                else
                    n.Ar_HeadlineImageUrl = newsItem.Ar_HeadlineImage;

                n.SourceLink = newsItem.SourceLink;

                string baseUrl = "https://themyriad.com/news/detail/";
                if (Globals.BaseUrl.Contains("8020"))
                    baseUrl = "http://themyriad.com:8081/news/detail/";
                n.NewsUrl = baseUrl + newsItem.NewsID + "?layout=false";

                newsList.Add(n);
            }


            return newsList;
        }

        public DTO.ViewModels.NewsViewModels.News GetApiNewsById(int newsId)
        {
            var news = GetAllNews(0, 1, newsId).FirstOrDefault();
            if (news != null)
            {
                List<DTO.ViewModels.NewsViewModels.NewsDetail> newsDetailList = new List<DTO.ViewModels.NewsViewModels.NewsDetail>();
                var detail = uow.GenericRepository<EF.NewsDetail>().Table.Where(x => x.NewsID == news.NewsId).ToList();
                foreach (var detailItem in detail)
                {
                    DTO.ViewModels.NewsViewModels.NewsDetail d = new DTO.ViewModels.NewsViewModels.NewsDetail();
                    d.ContentType = detailItem.ContentType;
                    d.ContentValue = detailItem.ContentValue;

                    if (detailItem.Ar_ContentValue != null)
                        d.Ar_ContentValue = detailItem.Ar_ContentValue;
                    else
                        d.Ar_ContentValue = detailItem.ContentValue;

                    newsDetailList.Add(d);
                }

                news.NewsDetail = newsDetailList;
            }

            return news;
        }



        public List<string> GetContentType()
        {
            return uow.GenericRepository<EF.NewsDetail>().Table.Select(x => x.ContentType).Distinct().ToList();
        }

        public List<EF.NewsDetail> GetNewsDetails(int id)
        {
            return uow.GenericRepository<EF.NewsDetail>().Table.Where(x => x.NewsID == id).ToList();
        }

        public EF.NewsDetail GetNewsDetailById(int newsDetailId)
        {
            return uow.GenericRepository<EF.NewsDetail>().Table.Where(x => x.NewsDetailID == newsDetailId).FirstOrDefault();
        }

        public bool AddNewsDetail(NewsDetailVM model)
        {
            bool ret = false;
            EF.NewsDetail detail = new EF.NewsDetail
            {
                ContentType = model.SelectedContentType,
                NewsID = model.NewsId,
                ContentValue = model.ContentValue,
                Ar_ContentValue = model.Ar_ContentValue
            };


            try
            {
                if (model.SelectedContentType.ToLower() == "image")
                {
                    ImageResult result = new ImageResult();

                    Common.ImageUpload upload = new Common.ImageUpload()
                    {
                        Quality = 80
                    };
                    result = upload.RenameUploadFile(model.ImageSource);

                    if (!result.Success)
                        return false;

                    detail.ContentValue = result.ImageName;
                }

                //uow.GenericRepository<NewsDetail>().Insert(detail);
                //uow.SaveChanges();

                //ret = true;


                if (model.SelectedContentType.ToLower() == "image")
                {
                    ImageResult result = new ImageResult();

                    Common.ImageUpload upload = new Common.ImageUpload()
                    {
                        Quality = 80
                    };
                    result = upload.RenameUploadFile(model.Ar_ImageSource);

                    //if (!result.Success)
                    //    return false;

                    detail.Ar_ContentValue = result.ImageName;
                }

                uow.GenericRepository<EF.NewsDetail>().Insert(detail);
                uow.SaveChanges();

                ret = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ret;
        }

        public bool UpdateNewsDetail(NewsDetailVM model)
        {
            bool ret = false;

            EF.NewsDetail detail = GetNewsDetailById(model.NewsDetailId);

            detail.ContentType = model.SelectedContentType;
            detail.NewsID = model.NewsId;
            detail.ContentValue = model.ContentValue;
            detail.Ar_ContentValue = model.Ar_ContentValue;

            try
            {
                if (model.SelectedContentType.ToLower() == "image")
                {
                    ImageResult result = new ImageResult();

                    Common.ImageUpload upload = new Common.ImageUpload()
                    {
                        Quality = 80
                    };
                    result = upload.RenameUploadFile(model.ImageSource);

                    if (!result.Success)
                        return false;


                    detail.ContentValue = result.ImageName;
                    detail.Ar_ContentValue = result.ImageName;

                    if (model.Ar_ImageSource != null)
                    {
                        result = upload.RenameUploadFile(model.Ar_ImageSource);

                        if (result.Success)
                            detail.Ar_ContentValue = result.ImageName;
                    }

                }

                uow.GenericRepository<EF.NewsDetail>().Update(detail);
                uow.SaveChanges();

                ret = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ret;
        }

        public bool DeleteNewsDetail(int newsDetailId)
        {
            bool ret = false;
            try
            {
                EF.NewsDetail detail = GetNewsDetailById(newsDetailId);
                uow.GenericRepository<EF.NewsDetail>().Delete(detail);
                uow.SaveChanges();

                ret = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ret;
        }
    }
}
