using PMS.Common.Filters;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.DTO.ViewModels.PersonManageViewModels
{
    public class AddPersonVM
    {
        public int PersonID { get; set; }

        [Required]
        public int LocationId { get; set; }

        [Required]
        public string Code { get; set; }
        public string ReferralCode { get; set; }

        [Required]
        public string Title { get; set; }

        [Display(Name = "Full Name")]
        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        //[RegularExpression(@"^[A-Za-z0-9](([_\.\-]?[a-zA-Z0-9]+)*)@([A-Za-z0-9]+)(([\.\-‌]?[a-zA-Z0-9]+)*)\.([A-Za-z]{2,})$", ErrorMessage = "Email is not valid")]
        public string Email { get; set; }

        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        public string SecondaryEmail { get; set; }

        public string CampusEmail { get; set; }
        public string GuardianEmail { get; set; }
        public string Religion { get; set; }
        [Required]
        public string PassportNumber { get; set; }


        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^\+?(?:[0-9]●?){6,14}[0-9]$", ErrorMessage = "Not a valid phone number")]
        [Required]
        public string Phone { get; set; }


        [Required]
        public string Gender { get; set; }

        [Display(Name = "Date of Birth")]
        [Required]
        public DateTime DOB { get; set; }

        [Required]
        public string Nationality { get; set; }

        public string LocationName { get; set; }
        public string University { get; set; }
        [Required]
        public int UniversityId { get; set; }

        public HttpPostedFileBase ProfilePhoto { get; set; }
        public string ImageUrl { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<DateTime> Checkin { get; set; }
        public string ResidentWhatsappNumber { get; set; }
        public BookingViewModels.AddBookingVM Booking { get; set; }
        public bool IsActive { get; set; }
        public int? BookingId { get; set; }
        public int? Type { get; set; }

        [RegularExpression(@"^\d{7,}$", ErrorMessage = "ResidentID Number must be numeric and at least 7 characters long")]
        public string ResidentID { get; set; }

        [Display(Name = "Vehicle Plate Number")]
        public string VehicleNumber { get; set; }

        [Required]
        public string GuardianFullName { get; set; }
        public string GuardianPassportNumber { get; set; }
        [Required]
        public string GuardianPhone { get; set; }
        [Required]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Guardian Other Email")]
        public string GuardianOtherEmail { get; set; }
        [Required]
        [Display(Name = "Guardian Relation")]
        public string GuardianRelation { get; set; }
        public string UniversityName { get; set; }
        public string MercuryID { get; set; }
        public string ProfileNotes { get; set; }
        public string UniversityStudentID { get; set; }
        public string ImportError { get; set; }

    }
    public class ImportFromExcelVM
    {
        [Required(ErrorMessage = "Please select file.")]
        public HttpPostedFileBase fileBase { get; set; }
    }
    public class ImportPersonsWithBookingResultVM
    {
        public List<AddPersonVM> SavedPersons { get; set; } = new List<AddPersonVM>();
        public List<AddPersonVM> NotSavedPersons { get; set; } = new List<AddPersonVM>();
        public List<ImportErrorSummaryItem> ErrorSummary { get; set; } = new List<ImportErrorSummaryItem>();

        public int TotalRows => SavedPersons.Count + NotSavedPersons.Count;

        public void BuildErrorSummary()
        {
            ErrorSummary = NotSavedPersons
                .Where(x => !string.IsNullOrWhiteSpace(x.ImportError))
                .GroupBy(x => x.ImportError)
                .Select(g => new ImportErrorSummaryItem { Reason = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
        }
    }

    public class ImportErrorSummaryItem
    {
        public string Reason { get; set; }
        public int Count { get; set; }
    }
    //PersonDocument (Booking --> Profile)
    public class PersonDocumentsVM
    {
        public string ImageUrl { get; set; }

        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg, svg, and pdf files are allowed.", Extensions = "png,jpg,jpeg,svg,pdf")]
        public HttpPostedFileBase ImageSource { get; set; }

        public int PersonDocumentID { get; set; }
        public int PersonID { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }

    }


    public class PersonViewModel
    {
        public string LocationName { get; set; }
        public string Code { get; set; }
        public string ResidentID { get; set; }
        public string ReferralCode { get; set; }
        public string Title { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string UniversityName { get; set; }
        public string Phone { get; set; }
        public string EditLink { get; set; } // URL for editing the person
    }

    public class PersonBinding
    {
        public int? id { get; set; }
        public string query { get; set; }
        public string orderBy { get; set; }
        public string orderDir {  get; set; }
        public string start { get; set; }
        public string length { get; set; }
        public string draw { get; set; }
        public Search search { get; set; }
        public List<string> SelectedColumns { get; set; }


    }
    public class PersonResponse
    {
        public List<PersonViewModels> person { get; set; }
        public int TotalRecords { get; set; }
        public int RecordsFiltered { get; set; }
    }
    public class Search
    {
        public string value { get; set; }
        public string regex { get; set; }
        public string column { get; set; }
    }
    public class PersonViewModels
    {
        public int PersonID { get; set; }
        public string Location { get; set; }
        public string MyriadID { get; set; }
        public string ResidentID { get; set; }
        public string ReferralCode { get; set; }
        public string Title { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string University { get; set; }
        public string Phone { get; set; }
        public DateTime? DOB { get; set; }
        public string VehicleNumber { get; set; }
        public string MercuryID { get; set; }
        public string PassportNumber { get; set; }
        public string ProfileNotes { get; set; }
        public string UniversityStudentID { get; set; }

    }

    public class InHousePortalCredentialsResultVM
    {
        public int TotalInHouseResidents { get; set; }
        public int Created { get; set; }
        public int Resent { get; set; }
        public int Failed { get; set; }
        public List<InHousePortalCredentialsDetailVM> Details { get; set; }
    }

    public class InHousePortalCredentialsDetailVM
    {
        public int PersonID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
}
