using PMS.Common.Filters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.DTO.ViewModels.NewsViewModels
{
    public class AllNewsVM
    {
        public int NewsID { get; set; }
        public int NewsCategoryID { get; set; }
        public string NewsCategory { get; set; }
        public string Heading { get; set; }
        public string Headline { get; set; }
        public System.DateTime NewsDate { get; set; }
        public string Thumbnail { get; set; }
        public string HeadlineImage { get; set; }
        public string SourceLink { get; set; }
        public bool IsEnable { get; set; }
        public bool IsActive { get; set; }
        public System.DateTime CreatedDate { get; set; }
    }

    public class NewsVM
    {
        public int NewsId { get; set; }

        [Required(ErrorMessage = "Please select category")]
        public int SelectedCategory { get; set; }
        public List<PMS.EF.NewsCategory> NewsCategories { get; set; }

        [Required]
        public string Heading { get; set; }


        public string Ar_Heading { get; set; }

        [Required]
        public string Headline { get; set; }


        public string Ar_Headline { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime NewsDate { get; set; }

        public string SourceLink { get; set; }

        [Required]
        public bool IsEnable { get; set; }
        [Required]
        public bool IsActive { get; set; }


        [Required(ErrorMessage = "Please select file.")]
        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase ThumbnailImage { get; set; }


        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase Ar_ThumbnailImage { get; set; }


        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase HeadlineImage { get; set; }

        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase Ar_HeadlineImage { get; set; }

        public string ThumbnailImageUrl { get; set; }

        public string Ar_ThumbnailImageUrl { get; set; }

        public string HeadlineImageUrl { get; set; }

        public string Ar_HeadlineImageUrl { get; set; }
    }
    public class NewsDetailVM
    {
        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase ImageSource { get; set; }


        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase Ar_ImageSource { get; set; }

        public string ImageUrl { get; set; }

        public string Ar_ImageUrl { get; set; }
        public string ContentValue { get; set; }
        public string Ar_ContentValue { get; set; }

        [Required(ErrorMessage = "This field is required.")]
        public string SelectedContentType { get; set; }
        public List<string> ContentTypes { get; set; }

        public int NewsId { get; set; }
        public int NewsDetailId { get; set; }

    }


    #region Home Page

    public class News
    {
        public int NewsId { get; set; }
        public int NewsCategoryID { get; set; }
        public string Heading { get; set; }

        public string Ar_Heading { get; set; }

        public string Headline { get; set; }

        public string Ar_Headline { get; set; }
        public DateTime NewsDate { get; set; }
        public string ThumbnailUrl { get; set; }

        public string Ar_ThumbnailUrl { get; set; }
        public string HeadlineImageUrl { get; set; }

        public string Ar_HeadlineImageUrl { get; set; }
        public string NewsUrl { get; set; }
        public string SourceLink { get; set; }


        public List<NewsDetail> NewsDetail { get; set; }
    }
    public class NewsDetail
    {
        public string ContentType { get; set; }
        public string ContentValue { get; set; }

        public string Ar_ContentValue { get; set; }
    }
    #endregion


}
