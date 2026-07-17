using PMS.DTO.ViewModels.NewsViewModels;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.News
{
    public interface INewsService
    {
        List<NewsCategory> GetNewsCatogries();
        List<AllNewsVM> GetAllNews();

        List<DTO.ViewModels.NewsViewModels.News> GetAllNews(int newsCategoryID, int count, int newsId);

        DTO.ViewModels.NewsViewModels.News GetApiNewsById(int newsId);


        EF.News GetNewsById(int id);
        bool AddNew(NewsVM newsVM);
        bool UpdateNews(NewsVM newsVM);

        List<string> GetContentType();

        List<EF.NewsDetail> GetNewsDetails(int id);
        EF.NewsDetail GetNewsDetailById(int newsDetailId);

        bool AddNewsDetail(NewsDetailVM model);
        bool UpdateNewsDetail(NewsDetailVM model);

        bool DeleteNewsDetail(int newsDetailId);
    }
}
