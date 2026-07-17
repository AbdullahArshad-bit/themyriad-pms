using Newtonsoft.Json;
using PMS.Common.Filters;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.DTO.ViewModels.BedSpacePlacementViewModels
{
    public class PlacementsListVM
    {
        public int BedSpacePlacementID { get; set; }
        public int BookingID { get; set; }
        public int PersonID { get; set; }

        public string Title { get; set; }
        public string FullName { get; set; }
        public string ReferralCode { get; set; }
        public string Gender { get; set; }
        [Display(Name = "Move In")]
        public DateTime MoveIn { get; set; }
        [Display(Name = "Move Out")]
        public DateTime MoveOut { get; set; }
        public Nullable<DateTime> CheckIn { get; set; }
        public Nullable<DateTime> CheckOut { get; set; }
        public string Commitment { get; set; }
        public string RoomType { get; set; }

        public string BedSpace { get; set; }
        public string Room { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }

        public string Requests { get; set; }
        public bool BedspacePlacementIsEnable { get; set; }
        public DateTime Createddate { get; set; }

        public int? GuestCount { get; set; }
        public string Description { get; set; }
        public List<GuestDetailVm> Guests { get; set; } = new List<GuestDetailVm>();

    }

    public class AddBedSpacePlacementVM
    {
        public int BedSpacePlacementID { get; set; }

        [Display(Name = "Booking")]
        [Required]
        public int BookingID { get; set; }

        [Display(Name = "Bed Space")]
        [Required]
        public int BedSpaceID { get; set; }
        public List<SelectListVM> BedSpacesList { get; set; }
        public List<SelectListVM> AllBedSpacesList { get; set; }

        [Display(Name = "Move In")]
        public DateTime MoveIn { get; set; }
        [Display(Name = "Move Out")]
        public DateTime MoveOut { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }

        public string Requests { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string BookingNumber { get; set; }
        public string BedName { get; set; }
        public int Duration { get; set; }
    }
    public class InspectionShow
    {
        public string InspectionName { get; set; }
        public int InspectionID { get; set; }
    }

    public class GenetateInspectionVM
    {
        [Required(ErrorMessage = "Inspection is required.")]

        public int InspectionID { get; set; }
        public int BedSpaceID { get; set; }
        public string Remarks { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public int Staff_Status { get; set; }
        public int Student_Status { get; set; }

        public int Maintenance_Status { get; set; }

        public bool IsEnable { get; set; }

    }

    public class BedSpacePlacementMigrationVM
    {
        public int PlacementId { get; set; }
        public int BedSpaceId { get; set; }
        public string Remarks { get; set; }
        public string EncoderNumber { get; set; }
        public int LocationId { get; set; }
        public bool IsCheckedIn { get; set; }
        public List<PlacementHistoryVM> PlacementHistory { get; set; }
    }

    public class PlacementHistoryVM
    {
        public string OldBedSpace { get; set; }
        public string NewBedSpace { get; set; }
        public string Remarks { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreatedDate { get; set; }

    }

    public class PlacementsBinding
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? personId { get; set; }
        public int? id { get; set; }
        public string query { get; set; }
        public string start { get; set; }
        public string length { get; set; }
        public string draw { get; set; }
        public Search search { get; set; }
        public string QueryBy { get; set; }
        public string orderBy { get; set; }
        public string orderDir { get; set; }
        public int termID { get; set; }
        public List<string> SelectedColumns { get; set; }

    }

    public class PlacementsResponse
    {
        public List<V_PlacementList> PlacementList { get; set; }
        public int TotalRecords { get; set; }
        public int RecordsFiltered { get; set; }
    }
    public class Search
    {
        public string value { get; set; }
        public string regex { get; set; }
        public string column { get; set; }
    }

    public class GuestCountVm
    {
        public string RoomType { get; set; }
        public int PersonId { get; set; }
        public int BedSpacePlacementID { get; set; }
        public int BookingID { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public DateTime MoveIn { get; set; }
        public DateTime MoveOut { get; set; }
        public string BedSpace { get; set; }
        public string Room { get; set; }

        public List<GuestDetailVm> Guests { get; set; } = new List<GuestDetailVm>();
    }

    public class GuestDetailVm
    {
        public int ID { get; set; }
        public int BedSpacePlacementID { get; set; }
        public int GuestCount { get; set; }
        public string Description { get; set; }
        public string IDNumber { get; set; }
        public string GuestName { get; set; }
        public DateTime CurrentDateTime { get; set; }
        public string VisitorCardNumber { get; set; }
        public string ImageUrl { get; set; }

        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg, svg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase ImageSource { get; set; }
    }

    public class GuestCountListVM
    {
        public string Title { get; set; }
        public string Code { get; set; }
        public string FullName { get; set; }
        public string BedRoom { get; set; }
        public int GuestCount { get; set; }
        public int PersonGuestID { get; set; }
        public int BedSpacePlacementID { get; set; }
        public string Description { get; set; }
        public string IDNumber { get; set; }
        public string GuestName { get; set; }
        public string VisitorCardNumber { get; set; }
        public string CreatedBy { get; set; }
        public string LocationName { get; set; }
    }
    public class ICCardResponse
    {
        public List<ICCardItem> list { get; set; }
        [JsonProperty("cardId")]
        public int CardId { get; set; }
    }
    public class ICCardItem
    {
        public int cardId { get; set; }
        public int lockId { get; set; }
        public string cardNumber { get; set; }
        public string cardName { get; set; }
        public long startDate { get; set; }
        public long endDate { get; set; }
        public long createDate { get; set; }
        public string senderUsername { get; set; }
        public int cardType { get; set; }
    }
    public class ICCardDetail
    {
        public int CardId { get; set; }
        public int LockId { get; set; }
        public string CardNumber { get; set; }
        public string CardName { get; set; }
        public long StartDate { get; set; }
        public long EndDate { get; set; }
        public long CreateDate { get; set; }
        public string SenderUsername { get; set; }
        public int CardType { get; set; }
    }
    public class DeleteCardResponse
    {
        public int ErrCode { get; set; }
        public string ErrMsg { get; set; }
    }
    public class PlacementDataOptimized
    {
        public int PlacementId { get; set; }
        public int BookingId { get; set; }
        public int PersonId { get; set; }
        public string PersonCode { get; set; }
        public string PersonFullName { get; set; }
        public string PersonEmail { get; set; }
        public string PersonPhone { get; set; }
        public string PersonNationality { get; set; }
        public string University { get; set; }
        public string UniversityName { get; set; }
        public string BookingNumber { get; set; }
        public string RoomTypeName { get; set; }
        public string BuildingName { get; set; }
        public string FloorName { get; set; }
        public string RoomName { get; set; }
        public string BedName { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int LocationId { get; set; }
        public string EmergencyContactFullName { get; set; }
        public string EmergencyContactRelation { get; set; }
        public string EmergencyContactEmail { get; set; }
        public string EmergencyContactPhone { get; set; }
    }
}
