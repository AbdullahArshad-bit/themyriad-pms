using PMS.Common.Filters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.DTO.ViewModels.SetupViewModels
{
    public class GetTableVM
    {
        public string View { get; set; }
        public int Id { get; set; }
        public int LinkId { get; set; }
    }
    public class AddLocationVM
    {
        public int LocationID { get; set; }
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(255)]
        public string LocationName { get; set; }
        public string Ar_LocationName { get; set; }
        public string LocationDescription { get; set; }
        public string Prefix { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
    public class GetLocation
    {
        public int LocationID { get; set; }
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(255)]
        public string LocationName { get; set; }
        public string Ar_LocationName { get; set; }
        public string Prefix { get; set; }
    }
    public class AddProjectVM
    {
        public int ProjectID { get; set; }
        [Required(ErrorMessage = "Location is required.")]
        public int LocationID { get; set; }
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(255)]
        public string ProjectName { get; set; }
        public string ProjectCity { get; set; }
        public string ProjectState { get; set; }
        //[Range(1, int.MaxValue, ErrorMessage = "Field must be number")]
        public string ProjectZip { get; set; }
        public string ProjectAddress { get; set; }
        public string ProjectDescription { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
    public class AddBuildingVM
    {
        public int BuildingID { get; set; }

        [Required(ErrorMessage = "Project is required.")]
        public int ProjectID { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(255)]
        public string BuildingName { get; set; }
        public string BuildingDescription { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
    public class AddFloorVM
    {
        public int FloorID { get; set; }
        [Required(ErrorMessage = "Building is required.")]
        public int BuildingID { get; set; }
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(255)]
        public string FloorName { get; set; }
        public string FloorDescription { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
    public class AddRoomVM
    {
        public int RoomID { get; set; }
        [Required(ErrorMessage = "Floor is required.")]
        public int FloorID { get; set; }
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(255)]
        public string RoomName { get; set; }


        [Required(ErrorMessage = "Room Type is required.")]
        public int RoomTypeID { get; set; }

        public List<EF.RoomType> RoomTypeList { get; set; }


        [Required(ErrorMessage = "Room size is required.")]
        public string RoomSize { get; set; }
        public int RoomLockId { get; set; }

        [Required(ErrorMessage = "Room gender is required.")]
        public string RoomGender { get; set; }
        public string RoomDescription { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
    public class AddBedSpaceVM
    {
        public int BedSpaceID { get; set; }

        [Required(ErrorMessage = "Room is required.")]
        public int RoomID { get; set; }


        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(255)]
        public string BedSpaceName { get; set; }

        public string BedSpaceGender { get; set; }
        public string BedSpaceAddress { get; set; }
        public string BedSpaceDescription { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public bool Status { get; set; }
        public string RoomName { get; set; }
    }

    public class AddAllRoomFeatureVM
    {
        public int AllRoomFeatureID { get; set; }


        [Display(Name = "Feature Name")]
        [Required]
        public string FeatureName { get; set; }

        [Display(Name = "Arabic Feature Name")]
        [Required]
        public string Ar_FeatureName { get; set; }

        public string ImageUrl { get; set; }
        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg, svg image files are allowed.", Extensions = "png,jpg,jpeg,svg")]
        public HttpPostedFileBase ImageSource { get; set; }

    }


    public class RoomTypeDetailsVM
    {
        public string ImageUrl { get; set; }

        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg, svg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase ImageSource { get; set; }

        public int ID { get; set; }
        public int RoomTypeID { get; set; }
        public string Description { get; set; }
        public string Ar_Description { get; set; }
        public string ThumbnailUrl { get; set; }

    }



    public class AddRoomTypeVM
    {
        public int RoomTypeID { get; set; }

        [Required(ErrorMessage = "Code is required.")]
        [MaxLength(255)]
        public string RoomCode { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(255)]
        public string RoomName { get; set; }

        public string RoomDescription { get; set; }

        [Required(ErrorMessage = "Area is required.")]
        [MaxLength(255)]
        public string RoomArea { get; set; }

        public List<int> SelectedFeatures { get; set; }

        public List<EF.AllRoomFeature> AllRoomFeaturesList { get; set; }
        public List<EF.AllRoomFeature> RoomTypeFeatures  { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public int? LocationId { get; set; }
        [Required(ErrorMessage = "Arabic Name is required.")]
        public string Ar_RoomName { get; set; }
        public string Ar_RoomDescription { get; set; }
        public string BedSpace { get; set; }

        [Display(Name = "Actual Price")]
        [Required(ErrorMessage = "Price is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a value greater than 0")]
        public decimal? Actual_Price { get; set; }
        public string RoomInstruction { get; set; }
        public string Ar_RoomInstruction { get; set; }

        public string Ar_thumbnail { get; set; }
        public string thumbnail { get; set; }


    }
    public class ImportTermFromExcelVM
    {
        [Required(ErrorMessage = "Please select file.")]
        public HttpPostedFileBase fileBase { get; set; }
    }
    public class ImportRoomFromExcelVM
    {
        [Required(ErrorMessage = "Please select file.")]
        public HttpPostedFileBase fileBase { get; set; }
    }
    public class ImportBedFromExcelVM
    {
        [Required(ErrorMessage = "Please select file.")]
        public HttpPostedFileBase fileBase { get; set; }
    }

    public class ImportPriceConfigFromExcelVM
    {
        [Required(ErrorMessage = "Please select file.")]
        public HttpPostedFileBase fileBase { get; set; }
    }

    public class AddTermVM
    {
        public int TermID { get; set; }
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(255)]
        public string TermName { get; set; }
        [Required(ErrorMessage = "Arabic Name is required.")]
        [MaxLength(255)]
        public string Ar_TermName { get; set; }

        [Display(Name = "Min Duration")]
        [Required(ErrorMessage = "Duration is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Please enter a value greater than 0")]
        public int? Min_Duration { get; set; }

        //[Display(Name = "Start Date")]
        //[Required]
     [Display(Name = "Term Start Date")]
        public DateTime? TermStartDate { get; set; }

        //[Display(Name = "End Date")]
        //[Required]
     [Display(Name = "Term End Date")]
        public DateTime? TermEndDate { get; set; }
        public int? LocationId { get; set; }
        public string TermDescription { get; set; }
        
        public string Room_Occupancy { get; set; }
        public string Ar_Room_Occupancy { get; set; }
        
        public string Room_Standared { get; set; }
        public string Ar_Room_Standared { get; set; }
        public string Ar_TermDescription { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string DurationType { get; set; }
        public bool IsPublished { get; set; }
        public int FrequencyId { get; set; }
        public int? UniversityId { get; set; }
        public string RateInfo { get; set; }
    }


    public class TermsDropdown
    {
        public int TermID { get; set; }
        public string TermDescription { get; set; }
        public string TermName { get; set; }
        }
    public class PriceConfigVM
    {
        public int PriceConfigID { get; set; }
        public string LocationName { get; set; }
        public int? LocationId { get; set; }
        public int TermID { get; set; }
        public int RoomTypeID { get; set; }
        public string TermName { get; set; }
        public string UniversityName { get; set; }
        public string TermDescription { get; set; }
        public string RoomTypeName { get; set; }
        public string RoomTypeDescription { get; set; }
        public decimal Price { get; set; }
        public decimal Deposit { get; set; }
        public decimal? CleaningCharge { get; set; }
        public string Currency { get; set; }
        public object RoomType { get; set; }
        public object RoomTypeDetails { get; set; }
        public bool IsAvailable { get; set; }
        public int OrderBy { get; set; }

    }

    public class AddPriceConfigVM
    {
        public int? LocationId { get; set; }
        public List<EF.Term> TermsList { get; set; }
        public List<EF.RoomType> RoomTypesList { get; set; }

        public int PriceConfigID { get; set; }
        [Required(ErrorMessage = "Term is required.")]
        public int TermID { get; set; }

        [Required(ErrorMessage = "Room type is required.")]
        public int RoomTypeID { get; set; }
        public string RoomName { get; set; }

        [Required(ErrorMessage = "Currency is required.")]
        public string Currency { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Deposit is required.")]
        public decimal Deposit { get; set; }

        public decimal? CleaningCharge { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }

        public bool IsAvailable { get; set; }
        [Required(ErrorMessage = "OrderBy is required.")]
        public int OrderBy { get; set; }

        [Required(ErrorMessage = "Please select file.")]
        public HttpPostedFileBase fileBase { get; set; }

        public string TermName { get; set; }
        public string RateInfo { get; set; }
        public string ErrorMessage { get; set; } 


    }
    public class TermsAndRoomsVM
    {
      public  List<AddRoomTypeVM> addRooms { get; set; }

      public  List<AddTermVM> addTerms { get; set; }

    }


    public class dummy
    {
        public string BuildingName { get; set; }

        //[Display(Name = "Start Date")]
        //[Required]
        [DisplayFormat(DataFormatString = "{0:dd/M/yyyy}",
           ApplyFormatInEditMode = true)]
        public DateTime TermStartDate { get; set; }

        //[Display(Name = "End Date")]
        //[Required]
        [DisplayFormat(DataFormatString = "{0:dd/M/yyyy}",
           ApplyFormatInEditMode = true)]
        public DateTime TermEndDate { get; set; }


    }


    public class LocationSettingsVM
    {
        public int id { get; set; }
        public int LocationId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a value greater than or equal to {1}")]
        [Display(Name = "Registration Fee")]
        public decimal RegistrationFee { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        [Display(Name = "Code of Conduct for English")]
        public string CodeOfConduct_EN { get; set; }
        [Display(Name = "Code of Conduct for Arabic")]
        public string CodeOfConduct_AR { get; set; }
        [Display(Name = "Terms And Condition in English")]
        public string TermsAndCondition_EN { get; set; }
        [Display(Name = "Terms And Condition in Arabic")]
        public string TermsAndCondition_AR { get; set; }
        public string CompanyName { get; set; }
        public string VATNo { get; set; }
        public string PaymentGateWay { get; set; }
        public string ReferralProgram { get; set; }
        public bool ReferralIsActive { get; set; }
        public string PreCheckinDocumention { get; set; }
        public bool PreCheckinDocumentationIsActive { get; set; }
        public string Branch { get; set; }
        public string Bank { get; set; }
        public string Title { get; set; }
        public string Account { get; set; }
        public string Currency { get; set; }
        public string SwiftCode { get; set; }
        public string IBAN { get; set; }
        public int? Def_Acc_Rec { get; set; }
        public int? Def_Acc_Pay { get; set; }
        public int? Def_Acc_Discount { get; set; }
        public int? Def_Acc_Adv_Pay { get; set; }

        // Transaction password (encrypted at rest, decrypted for edit)
        [Display(Name = "Transaction Password")]
        [RegularExpression(@"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{8,}$", ErrorMessage = "Minimum eight characters, at least one uppercase letter, one lowercase letter, one number and one special character is required")]
        public string TransactionPassword { get; set; }

    }
    public class CurrencyVM
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }
    public class FrequencyVm
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Status { get; set; }
    }
    public class UniversityVM
    {
        public int Id { get; set; }
        public string UniversityName { get; set; }
        public int? LocationId { get; set; }
        public bool IsActive { get; set; }
        public bool IsEnable { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UniversityArabicName { get; set; }
        public string Prefix { get; set; }
        public string EmailPrefix { get; set; }
        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase ThumbnailImage { get; set; }
        public string ThumbnailImageUrl { get; set; }
        public string ImageUrl { get; set; }
        public string UniDescription { get; set; }
        public string Ar_UniDescription { get; set; }
        public string EmailCC { get; set; }
    }
}
