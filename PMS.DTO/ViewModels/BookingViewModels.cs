using PMS.Common.Filters;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.DTO.ViewModels.BookingViewModels
{
    public class BookingListVM
    {
        public int BookingID { get; set; }
        public int PersonID { get; set; }

        public string Title { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }

        public string Commitment { get; set; }
        public string RoomType { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool? IsCancel { get; set; }
        public string Status { get; set; }
        public string HearFrom { get; set; }
        public string Channel { get; set; }
        public string Source { get; set; }
        public string PaymentType { get; set; }
        public string BookingNumber { get; set; }
        public  string AccessibilityRequest { get; set; }

        public DateTime BookingDate { get; set; }
        public string LocationName { get; set; }
        public string University { get; set; }
        public string UniReferenceNo { get; set; }
        public int? UniversityId { get; set; }
        public string Nationality { get; set; }
        public string TenantPassportNumber { get; set; }
        public string GuardianFullName { get; set; }
        public string GuardianPhone { get; set; }
        public string GuardianEmail { get; set; }
        public string GuardianRelation { get; set; }
        public string PrefereableView { get; set; }
        public string PrefereableFloor { get; set; }
        public string Religions { get; set; }
        public string Nationalities { get; set; }
        public string Universities { get; set; }
        public string AgeRange { get; set; }
        public PriceConfig PriceConfig { get; set; }

    }
    public class BookingsBinding
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? personId { get; set; }
        public int? id { get; set; }
        public string query { get; set; }
        public string orderBy { get; set; }
        public string orderDir { get; set; }
        public string start { get; set; }
        public string length { get; set; }
        public string draw { get; set; }
        public Search search { get; set; }
        public string QueryBy { get; set; }
        public List<string> SelectedColumns { get; set; }
    }
    public class BookingsResponse
    {
        public List<BookingListVM> BookingList { get; set; }

        public int TotalRecords { get; set; }
        public int RecordsFiltered { get; set; }
    }
    public class Search
    {
        public string value { get; set; }
        public string regex { get; set; }
        public string column { get; set; }
    }
    public class AddBookingVM
    {
        public int BookingID { get; set; }


        [Display(Name = "Commitment")]
        [Required]
        public int PriceConfigID { get; set; }
        public List<SelectListVM> PriceConfigList { get; set; }

        [Display(Name = "Person")]
        [Required]
        public int PersonID { get; set; }

        public string PersonCode { get; set; }

        [Display(Name = "Special Request")]

        public string Requests { get; set; }

        [Display(Name = "CheckIn Date")]
        [Required]
        public DateTime CheckInDate { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }

        public DateTime? CheckOut { get; set; }
        public int? MinDuration { get; set; }
        public int? FrequencyId { get; set; }
        public string MercuryID { get; set; }
        public DateTime? TermEndDate { get; set; }
        public string TermName { get; set; }
        public string RoomTypeName { get; set; }
        public decimal? ImportPrice { get; set; }

    }
    public class UploadReceiptVM
    {
        public int BookingId { get; set; }
        public int PersonId { get; set; }
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase ThumbnailImage { get; set; }
        public string ThumbnailImageUrl { get; set; }
        public string ImageUrl { get; set; }

    }

    public class CommitmentDetailVM
    {
        public string Term { get; set; }
        public string TermDescription { get; set; }
        public string RoomType { get; set; }
        public string RoomTypeDescription { get; set; }
        public decimal Price { get; set; }
        public decimal? CleaningCharge { get; set; }
        public string Currency { get; set; }
    }

}
