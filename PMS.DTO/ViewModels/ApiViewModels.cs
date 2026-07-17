using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.ApiViewModels
{

    #region Home Page
    public class HomePageApiResponse
    {
        public List<Banner> Banner { get; set; }
        public List<Features> Features { get; set; }
        public List<Facilities> Facilities { get; set; }
        public List<News> News { get; set; }


        public Video Video { get; set; }
        public List<NearByPlaces> NearByPlaces { get; set; }
        public List<RoomFeatures> RoomFeatures { get; set; }
        public List<RoomTypes> RoomTypes { get; set; }
        public List<EF.RoomTypeDetail> RoomPrices { get; set; }
        //public List<ImageURLs> GalleryImages { get; set; }
        public List<News> DubaiNews { get; set; }
    }
    public class Banner
    {
        public string ImageUrl { get; set; }
    }
    public class Features
    {
        public string ImageUrl { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    public class Facilities
    {
        public string ImageUrl { get; set; }
        public string Name { get; set; }
    }
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



    #region Location
    public class LocationPageApiResponse
    {
        public Video Video { get; set; }
        public List<NearByPlaces> NearByPlaces { get; set; }
        public List<EF.RoomTypeFeature> RoomFeatures { get; set; }
        public List<EF.RoomType> RoomTypes { get; set; }
        //public List<v_RommTypeDetail> RoomPrices { get; set; }
        //public List<ImageURLs> GalleryImages { get; set; }
        public List<News> News { get; set; }
    }
    public class NearByPlaces
    {
        public string ImageUrl { get; set; }
        public string Name { get; set; }
    }
    public class RoomFeatures
    {
        public string ImageUrl { get; set; }
        public string Name { get; set; }
    }
    public class Video
    {
        public string VideoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Description { get; set; }
    }
    public class RoomTypes
    {
        public int RoomTypeID { get; set; }
        public string ImageUrl { get; set; }
    }


    #endregion


    #region Booking

    public class BookingApiResponse
    {
        public List<BookingLocation> Locations { get; set; }
        //public List<DTO.BookingCheckIn> CheckIn { get; set; }
        public List<BookingRoomType> RoomTypes { get; set; }
        public List<BookingDuration> Duration { get; set; }
    }
    public class BookingRoomTypeApiResponse
    {
        public List<RoomsDetail> RoomsDetail { get; set; }
        //public List<DTO.ViewModels.ImageURLs> RoomFeatures { get; set; }
        //public List<DTO.ViewModels.ImageURLs> RoomImages { get; set; }
    }

    public class RoomsDetail
    {
        public int RoomTypePriceID { get; set; }
        public int RoomTypeID { get; set; }
        public int DurationId { get; set; }
        public string RoomName { get; set; }
        public string RoomDescription { get; set; }
        public string PayDescription { get; set; }
        public int DurationMonths { get; set; }
        public decimal PricePerMonth { get; set; }
        public string Duration { get; set; }
        public decimal RefundableDeposit { get; set; }
        public decimal Deposit { get; set; }
        public int Frequency { get; set; }

    }
    public class RoomPrices
    {

    }
    public class PayMethod
    {
        public int PaymentMethodId { get; set; }
        public string Name { get; set; }
    }
    public class BookingCheckIn
    {
        public int CheckInId { get; set; }
        public string CheckInName { get; set; }
    }
    public class BookingLocation
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; }
    }

    public class BookingRoomType
    {
        public int RoomTypeId { get; set; }
        public string RoomName { get; set; }
        public string Description { get; set; }
    }
    public class BookingDuration
    {
        public int DurationId { get; set; }
        public string Duration { get; set; }
        public int DurationMonths { get; set; }
    }
    public class SharedPreference
    {
        public List<Preference> Preference { get; set; }
    }
    public class Preference
    {
        public List<string> PreferenceValues { get; set; }
        public string Description { get; set; }

    }

    public class IsAlreadyBookedResponse
    {
        public bool Status { get; set; }
        public string BookingNumber { get; set; }
    }
    public class BookNowVM
    {
        public string currentCulture { get; set; }
        [Required(ErrorMessage = "Please select location")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Please select room type")]
        public string RoomType { get; set; }

        [Required(ErrorMessage = "Please select duration of stay")]
        public string Duration { get; set; }


        public string SelectedLocation { get; set; }
        public string SelectedRoomType { get; set; }
        public string SelectedDuration { get; set; }
    }

    public class BookingSearchVM
    {
        public BookingSearchVM()
        {
            SearchResult = new SearchResultVM();
            OtherOffers = new List<OtherOffersVM>();
        }
        public SearchResultVM SearchResult { get; set; }
        public List<OtherOffersVM> OtherOffers { get; set; }
    }
    public class SearchResultVM
    {
        public string Room { get; set; }
        public string Ar_Room { get; set; }
        public string RoomTitle { get; set; }
        public string RoomDescription { get; set; }
        public string Ar_RoomDescription { get; set; }
        public string RoomInstruction { get; set; }
        public bool IsClubRoom { get; set; }
        public int Actual_Price { get; set; }
        public string Currency { get; set; }
        public string Bank { get; set; }
        public string Branch { get; set; }
        public string BranchTitle { get; set; }
        public string Account { get; set; }
        public string SwiftCode { get; set; }
        public List<Commitment> Commintments { get; set; }
        public List<ImageURLs> RoomTypeImages { get; set; }
        public List<ImageURLs> RoomTypeFeatureIcons { get; set; }
        public string Prefix { get; set; }
        public string Email { get; set; }
    }
    public class OtherOffersVM
    {
        public string RoomTitle { get; set; }
        public string RoomDescription { get; set; }
        public string Location { get; set; }
        public string Duration { get; set; }
        public string RoomType { get; set; }
        public string Price { get; set; }
        public string PriceText { get; set; }
        public string ThumbnailImageUrl { get; set; }
        public string CommitmentLabel { get; set; }
        public string Ar_ThumbnailImageUrl { get; set; }
    }
    public class Commitment
    {
        public int RoomTypePriceID { get; set; }
        public int DurationOfStayId { get; set; }
        public string Price { get; set; }
        public string PriceText { get; set; }
        public string CommitmentText { get; set; }
        public string CommitmentDescription { get; set; }
        public int? Min_Duration { get; set; }
        public string Room_Occupancy { get; set; }
        public string Ar_Room_Occupancy { get; set; }

        public string RoomStandard { get; set; }

        public string Ar_RoomStandard { get; set; }
        public string CommitmentLabel { get; set; }
        public string CommitmentDeposit { get; set; }
        public decimal RefundableDeposit { get; set; }
        public string RefundableDepositText { get; set; }
        public int DurationMonths { get; set; }
        public int Frequency { get; set; }
        public bool IsSelected { get; set; }
        public string TotalRent { get; set; }
        public string Currency { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime? TimeStartDate { get; set; }
        public DateTime? TimeEndDate { get; set; }

    }
    public class ImageURLs
    {
        public string ImageUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Description { get; set; }

        public string ar_Description { get; set; }
        public bool IsVideo { get; set; }
        public string VideoUrl { get; set; }

        public int RoomTypeId { get; set; }
    }


    public class BookingVM
    {
        public string BookingNumber { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }
        public string Channel { get; set; }
        public string PaymentType { get; set; }


        [Required]
        public DateTime DOB { get; set; }

        [Required]
        public string Nationality { get; set; }
        public string University { get; set; }
        public int UniversityId { get; set; }


        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }

        public int RoomTypeID { get; set; }
        public decimal price { get; set; }
        public decimal InitialDeposit { get; set; }

        public string Currency { get; set; }

        public string AccessibilityRequest { get; set; }
     
        
        public string EmergencyFullName { get; set; }

        
        public string EmergencyPhone { get; set; }

        [EmailAddress]
        public string EmergencyEmail { get; set; }

        
        public string Emergencyrelation { get; set; }

        public string PassportNumber { get; set; }

        public string EmergencyOther { get; set; }
        public string FloorPreference { get; set; }
        public string ViewPreference { get; set; }
        public string Religion { get; set; }
        public string SNationality { get; set; }
        public string SUniversity { get; set; }
        public string SAgeRange { get; set; }


        public string SharingPreference { get; set; }
        public string PreferenceName { get; set; }
        public string PreferenceEmail { get; set; }
        public string Preference1 { get; set; }
        public string Preference2 { get; set; }
        public string Preference3 { get; set; }
        public string Preference4 { get; set; }
        public string Preference5 { get; set; }

        public Nullable<int> LocationID { get; set; }
        public Nullable<int> CheckInID { get; set; }

        [Required]
        public int RoomTypePriceID { get; set; }

        [Required]
        public int PaymentMethodID { get; set; }
        public string HearFrom { get; set; }
        public string HearFromCode { get; set; }
        public string HearFromName { get; set; }
        public string HearFromOther { get; set; }
        public string HearFromPhone { get; set; }

        [Required]
        public decimal Amount { get; set; }
        public string CardLastDigits { get; set; }
        public string TranRef { get; set; }
        public string Message { get; set; }
        public bool IsTest { get; set; }

        public bool IsMdx { get; set; }

        public savedCard SavedCard { get; set; }
        public BillingAddressVM BillingAddress { get; set; }

        public int SelectedCommitment { get; set; }

        public string CommitmentRoom { get; set; }
        public string Prefix { get; set; }
        public string UniReferenceNo { get; set; }

    }
    public class savedCard
    {
        public string maskedPan { get; set; }
        public string expiry { get; set; }
        public string cardholderName { get; set; }
        public string scheme { get; set; }
        public string cardToken { get; set; }
        public bool recaptureCsc { get; set; }
    }
    public class BillingAddressVM
    {
        public string PaymentID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    public class PersonVM
    {

        public string Title { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Gender { get; set; }
        public System.DateTime DOB { get; set; }
        public string Nationality { get; set; }
        public bool IsEnable { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<int> LocationId { get; set; }
        public string Code { get; set; }

    }

    public class UserPaymentVM
    {
        public int PaymentID { get; set; }
        public int PaymentMethodID { get; set; }
        public Nullable<int> BookingID { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string CardLastDigits { get; set; }
        public string TranRef { get; set; }
        public string Message { get; set; }
        public bool IsEnable { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
    }

    public class UniversitiesVM
    {
        public int Id { get; set; }

        public string UniversityName { get; set; }
        public string Prefix { get; set; }

    }
    public class RefundRequestVM
    {
        public string BankAccount { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public string Signature { get; set; }
        public string Code { get; set; }
        public string Email { get; set; }
        public string SendEmail { get; set; }
        public int PersonID { get; set; }
    }

    //Booking API Get All LocationSettingsApiVM
    public class LocationSettingsApiVM
    {
        public int LocationId { get; set; }
        public decimal RegistrationFee { get; set; }
        public string CodeOfConduct { get; set; }
        public string TermsAndCondition { get; set; }
    }

    #endregion

    #region userMaster
    public class UserMasterViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Nationality { get; set; }
        public string Phone { get; set; }
        public Nullable<System.DateTime> DOB { get; set; }
        public string University { get; set; }
        public string ImageUrl { get; set; }

    }

    #endregion
}
