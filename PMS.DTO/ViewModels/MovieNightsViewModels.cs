using PMS.Common.Filters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.DTO.ViewModels.MovieNightsViewModels
{
    public class MoviesVM
    {
        public int MovieID { get; set; }

        [Required(ErrorMessage = "Please enter movie title.")]
        public string MovieTitle { get; set; }


        [Required(ErrorMessage = "Please enter movie details.")]
        public string MovieDetails { get; set; }

        [Required(ErrorMessage = "Please select file.")]
        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase BannerImage { get; set; }
        public string BannerImageUrl { get; set; }

        [Required(ErrorMessage = "Please select file.")]
        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase MonthImage { get; set; }
        public string MonthImageUrl { get; set; }

        [Required(ErrorMessage = "Please select file.")]
        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase VoteImage { get; set; }
        public string VoteImageUrl { get; set; }
    }


    public class MovieShowsVM
    {
        [Required(ErrorMessage = "Please select Movie")]
        public int MovieID { get; set; }
        public List<EF.Movie> MoviesList { get; set; }
        public string MovieTitle { get; set; }
        public int MovieShowID { get; set; }

        [Required]
        public DateTime ShowDate { get; set; }

        [Required]
        public string ShowTime { get; set; }

        [Required(ErrorMessage = "Please enter Hall #")]
        [MaxLength(15)]
        public string HallNo { get; set; }

        public string ShowDetails { get; set; }
    }

    public class MovieVoteCampaignVM
    {
        public int MovieVoteCampaignID { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public DateTime ShowDate { get; set; }

        [Required]
        public string ShowTime { get; set; }

        [Required]
        public string HallNo { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public bool IsActive { get; set; }
    }

    public class MovieVoteCampaignDetailVM
    {
        [Required(ErrorMessage = "Please select Movie")]
        public int MovieID { get; set; }
        public List<EF.Movie> MoviesList { get; set; }
        public string MovieTitle { get; set; }
        public int MovieVoteCampaignID { get; set; }
        public int MovieVoteCampaignDetailID { get; set; }
        public int Likes { get; set; }
        public int DisLikes { get; set; }
    }
}
