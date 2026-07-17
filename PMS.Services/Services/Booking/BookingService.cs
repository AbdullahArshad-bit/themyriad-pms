using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.DTO.ViewModels;
using PMS.Services.Services.Person;
using PMS.Services.Services.Email;
using PMS.Services.Services.Correspondence;
using PMS.Services.Services.AuditLogs;
using PMS.DTO.ViewModels.BookingViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Common.Classes;
using PMS.DTO;
using System.Net;
using PMS.Services.Services.Notifications;
using System.Data.Entity.Core.Objects;
using System.Web;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.DTO.ViewModels.ApiViewModels;
using static System.Data.Entity.Infrastructure.Design.Executor;
using Ninject.Activation;
using System.Diagnostics.Eventing.Reader;
using PMS.DTO.ViewModels.FeedbackViewModels;
using PMS.Common;
using PMS.DTO.ViewModels.SetupViewModels;
using System.IO;
using PMS.DTO.ViewModels.PersonManageViewModels;
using PMS.Services.Services.LocationContext;

namespace PMS.Services.Services.Booking
{
    public class BookingService : IBookingService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IPersonService personService;
        private readonly ICorrespondenceService correspondenceService;
        private readonly IAuditLogsService auditLogsService;
        private readonly IEmailService emailService;
        private readonly INotificationService notificationService;
        private readonly ILocationContextService locationContextService;

        public BookingService(UnitOfWork<PMSEntities> _uow, IPersonService _personService, ICorrespondenceService _correspondenceService, IEmailService _emailService, IAuditLogsService _auditLogsService, INotificationService _notificationService
            , ILocationContextService _locationContextService)
        {
            uow = _uow;
            personService = _personService;
            correspondenceService = _correspondenceService;
            auditLogsService = _auditLogsService;
            emailService = _emailService;
            notificationService = _notificationService;
            locationContextService = _locationContextService;
        }
        public List<BookingListVM> GetBookings(int personId = 0)
        {

            uow.Context.Database.ExecuteSqlCommand("EXEC BookingExpired");
            var db = uow.Context;

            var data = (from booking in db.Bookings

                        join p in db.People
                        on booking.PersonID equals p.PersonID
                        into per
                        from person in per.DefaultIfEmpty()
                        where (person.PersonID == personId || personId == 0)

                        join university in db.Universities
                        on person.UniversityId equals university.Id
                        into uni
                        from u in uni.DefaultIfEmpty()

                        join pr in db.PriceConfigs
                        on booking.PriceConfigID equals pr.PriceConfigID
                        into pri
                        from priceConfig in pri.DefaultIfEmpty()

                        join t in db.Terms
                        on priceConfig.TermID equals t.TermID
                        into tr
                        from term in tr.DefaultIfEmpty()

                        join rm in db.RoomTypes
                        on priceConfig.RoomTypeID equals rm.RoomTypeID
                        into rt
                        from roomType in rt.DefaultIfEmpty()

                        where booking.IsEnable == true

                        select new BookingListVM
                        {
                            BookingID = booking.BookingID,
                            LocationName = booking.Location.LocationName ?? "",
                            University = u.UniversityName,
                            PersonID = booking.PersonID,
                            Commitment = term.TermName,
                            RoomType = roomType.RoomCode + " - " + roomType.RoomName,
                            CheckInDate = booking.CheckInDate,
                            CheckOutDate = booking.CheckOutDate,
                            Title = person.Title,
                            FullName = person.FullName,
                            Email = person.Email,
                            Gender = person.Gender,
                            Phone = person.Phone,
                            IsCancel = booking.IsCancel,
                            Channel = booking.Channel,
                            HearFrom = booking.HearFrom,
                            PaymentType = booking.PaymentType,
                            BookingNumber = booking.BookingNumber,
                            AccessibilityRequest = booking.AccessibilityRequest,
                            BookingDate = booking.CreatedDate,
                            Status = (db.BedSpacePlacements.Any(x => x.IsEnable == true && x.BookingID == booking.BookingID) ? "Allocated" : (booking.Status == true) ? "Expired" : "Pending"),
                            Nationality = person.Nationality,
                            PriceConfig = booking.PriceConfig

                        });

            if (personId > 0)
                data = data.Where(x => x.PersonID == personId);

            return data.ToList();
        }

        public BookingsResponse GetPMSBookings(BookingsBinding request)
        {
            //uow.Context.Database.ExecuteSqlCommand("EXEC BookingExpired");
            var db = uow.Context;

            var _returnModel = new List<BookingListVM>();
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();
            List<string> selectedColumns = request.SelectedColumns ?? new List<string>();
            List<string> allColumns = new List<string>
{
    "BookingID", "LocationName", "University", "PersonID", "MyriadID", "Commitment",
    "RoomType", "CheckInDate", "CheckOutDate", "Title", "FullName", "Email",
    "Gender", "Phone", "IsCancel", "Channel","Source","UniReferenceNo", "HearFrom", "PaymentType",
    "BookingNumber", "AccessibilityRequest", "BookingDate", "Status", "Nationality",
    "TenantPassportNumber", "GuardianFullName", "GuardianPhone","GuardianEmail",
    "GuardianRelation","PrefereableFloor","PrefereableView","Religions","Nationalities","Universities","AgeRange"
};

            // Exclude the selected columns from all columns
            List<string> unselectedColumns = allColumns.Except(selectedColumns).ToList();
            IQueryable<BookingListVM> bookingQuery = from booking in db.Bookings
                                                     join person in db.People on booking.PersonID equals person.PersonID
                                                     join university in db.Universities on person.UniversityId equals university.Id into uni
                                                     from u in uni.DefaultIfEmpty()
                                                     join priceConfig in db.PriceConfigs on booking.PriceConfigID equals priceConfig.PriceConfigID into pri
                                                     from priceConfig in pri.DefaultIfEmpty()
                                                     join term in db.Terms on priceConfig.TermID equals term.TermID into tr
                                                     from term in tr.DefaultIfEmpty()
                                                     join roomType in db.RoomTypes on priceConfig.RoomTypeID equals roomType.RoomTypeID into rt
                                                     from roomType in rt.DefaultIfEmpty()
                                                     join emergencyContact in db.EmergencyContacts on person.PersonID equals emergencyContact.PersonID into ec
                                                     from emergencyContact in ec.DefaultIfEmpty()
                                                     join specialRequest in db.SpecialRequests on person.PersonID equals specialRequest.PersonID into sr
                                                     from specialRequest in sr.DefaultIfEmpty()
                                                     where booking.IsEnable == true && assignedLocationIds.Contains((int)booking.LocationID)
                                                     &&
                                                     (request.FromDate == null || EntityFunctions.TruncateTime(booking.CheckInDate) >= EntityFunctions.TruncateTime(request.FromDate))
                                                     && (request.ToDate == null || EntityFunctions.TruncateTime(booking.CheckInDate) <= EntityFunctions.TruncateTime(request.ToDate))

                                                     select new BookingListVM
                                                     {
                                                         BookingID = unselectedColumns.Contains("BookingID") ? booking.BookingID : default,
                                                         LocationName = unselectedColumns.Contains("LocationName") ? booking.Location.LocationName ?? "" : default,
                                                         University = unselectedColumns.Contains("University") ? u.UniversityName : default,
                                                         PersonID = unselectedColumns.Contains("PersonID") ? booking.PersonID : default,
                                                         Commitment = unselectedColumns.Contains("Commitment") ? term.TermName : default,
                                                         RoomType = unselectedColumns.Contains("RoomType") ? roomType.RoomCode + " - " + roomType.RoomName : default,
                                                         CheckInDate = unselectedColumns.Contains("CheckInDate") ? booking.CheckInDate : default,
                                                         CheckOutDate = unselectedColumns.Contains("CheckOutDate") ? booking.CheckOutDate : default,
                                                         Title = unselectedColumns.Contains("Title") ? person.Title : default,
                                                         MyriadID = unselectedColumns.Contains("MyriadID") ? person.Code : default,
                                                         FullName = unselectedColumns.Contains("FullName") ? person.FullName : default,
                                                         Email = unselectedColumns.Contains("Email") ? person.Email : default,
                                                         Gender = unselectedColumns.Contains("Gender") ? person.Gender : default,
                                                         Phone = unselectedColumns.Contains("Phone") ? person.Phone : default,
                                                         IsCancel = unselectedColumns.Contains("IsCancel") ? booking.IsCancel : default,
                                                         Channel = unselectedColumns.Contains("Channel") ? booking.Channel : default,
                                                         Source = unselectedColumns.Contains("Source") ? booking.Source : default,
                                                         UniReferenceNo = unselectedColumns.Contains("UniReferenceNo") ? booking.UniReferenceNo : default,
                                                         HearFrom = unselectedColumns.Contains("HearFrom") ? booking.HearFrom : default,
                                                         PaymentType = unselectedColumns.Contains("PaymentType") ? booking.PaymentType : default,
                                                         BookingNumber = unselectedColumns.Contains("BookingNumber") ? booking.BookingNumber : default,
                                                         AccessibilityRequest = unselectedColumns.Contains("AccessibilityRequest") ? booking.AccessibilityRequest : default,
                                                         BookingDate = unselectedColumns.Contains("BookingDate") ? booking.CreatedDate : default,
                                                         Status = unselectedColumns.Contains("Status") ? (db.BedSpacePlacements.Any(x => x.IsEnable == true && x.BookingID == booking.BookingID) ? "Allocated" : (booking.Status == true) ? "Expired" : "Pending") : default,
                                                         Nationality = unselectedColumns.Contains("Nationality") ? person.Nationality : default,
                                                         TenantPassportNumber = unselectedColumns.Contains("TenantPassportNumber") ? emergencyContact.PassportNumber : default,
                                                         GuardianFullName = unselectedColumns.Contains("GuardianFullName") ? emergencyContact.FullName : default,
                                                         GuardianPhone = unselectedColumns.Contains("GuardianPhone") ? emergencyContact.Phone : default,
                                                         GuardianEmail = unselectedColumns.Contains("GuardianEmail") ? emergencyContact.Email : default,
                                                         GuardianRelation = unselectedColumns.Contains("GuardianRelation") ? emergencyContact.Relation : default,
                                                         PrefereableFloor = unselectedColumns.Contains("PrefereableFloor") ? specialRequest.PreferableFloor : default,
                                                         PrefereableView = unselectedColumns.Contains("PrefereableView") ? specialRequest.PreferableView : default,
                                                         Religions = unselectedColumns.Contains("Religions") ? specialRequest.Religions : default,
                                                         Nationalities = unselectedColumns.Contains("Nationalities") ? specialRequest.Nationalities : default,
                                                         Universities = unselectedColumns.Contains("Universities") ? specialRequest.Universities : default,
                                                         AgeRange = unselectedColumns.Contains("AgeRange") ? specialRequest.Agerange : default,


                                                     };
            // For server Side SOrting.
            if (!string.IsNullOrEmpty(request.orderBy))
            {
                switch (request.orderBy.ToLower())
                {
                    case "bookingid":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.BookingID) : bookingQuery.OrderByDescending(x => x.BookingID);
                        break;
                    case "personid":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.PersonID) : bookingQuery.OrderByDescending(x => x.PersonID);
                        break;
                    case "title":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.Title) : bookingQuery.OrderByDescending(x => x.Title);
                        break;
                    case "myriadid":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.MyriadID) : bookingQuery.OrderByDescending(x => x.MyriadID);
                        break;
                    case "fullname":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.FullName) : bookingQuery.OrderByDescending(x => x.FullName);
                        break;
                    case "gender":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.Gender) : bookingQuery.OrderByDescending(x => x.Gender);
                        break;
                    case "checkindate":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.CheckInDate) : bookingQuery.OrderByDescending(x => x.CheckInDate);
                        break;
                    case "checkoutdate":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.CheckOutDate) : bookingQuery.OrderByDescending(x => x.CheckOutDate);
                        break;
                    case "commitment":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.Commitment) : bookingQuery.OrderByDescending(x => x.Commitment);
                        break;
                    case "roomtype":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.RoomType) : bookingQuery.OrderByDescending(x => x.RoomType);
                        break;
                    case "email":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.Email) : bookingQuery.OrderByDescending(x => x.Email);
                        break;
                    case "phone":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.Phone) : bookingQuery.OrderByDescending(x => x.Phone);
                        break;
                    case "iscancel":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.IsCancel) : bookingQuery.OrderByDescending(x => x.IsCancel);
                        break;
                    case "status":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.Status) : bookingQuery.OrderByDescending(x => x.Status);
                        break;
                    case "hearfrom":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.HearFrom) : bookingQuery.OrderByDescending(x => x.HearFrom);
                        break;
                    case "channel":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.Channel) : bookingQuery.OrderByDescending(x => x.Channel);
                        break;
                    case "source":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.Source) : bookingQuery.OrderByDescending(x => x.Source);
                        break;
                    case "unireferenceno":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.UniReferenceNo) : bookingQuery.OrderByDescending(x => x.UniReferenceNo);
                        break;
                    case "paymenttype":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.PaymentType) : bookingQuery.OrderByDescending(x => x.PaymentType);
                        break;
                    case "bookingnumber":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.BookingNumber) : bookingQuery.OrderByDescending(x => x.BookingNumber);
                        break;
                    case "accessibilityrequest":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.AccessibilityRequest) : bookingQuery.OrderByDescending(x => x.AccessibilityRequest);
                        break;
                    case "bookingdate":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.BookingDate) : bookingQuery.OrderByDescending(x => x.BookingDate);
                        break;
                    case "locationname":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.LocationName) : bookingQuery.OrderByDescending(x => x.LocationName);
                        break;
                    case "university":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.University) : bookingQuery.OrderByDescending(x => x.University);
                        break;
                    case "nationality":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.Nationality) : bookingQuery.OrderByDescending(x => x.Nationality);
                        break;
                    case "tenantpassportnumber":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.TenantPassportNumber) : bookingQuery.OrderByDescending(x => x.TenantPassportNumber);
                        break;
                    case "guardianfullname":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.GuardianFullName) : bookingQuery.OrderByDescending(x => x.GuardianFullName);
                        break;
                    case "guardianphone":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.GuardianPhone) : bookingQuery.OrderByDescending(x => x.GuardianPhone);
                        break;
                    case "guardianemail":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.GuardianEmail) : bookingQuery.OrderByDescending(x => x.GuardianEmail);
                        break;
                    case "guardianrelation":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.GuardianRelation) : bookingQuery.OrderByDescending(x => x.GuardianRelation);
                        break;
                    case "prefereablefloor":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.PrefereableFloor) : bookingQuery.OrderByDescending(x => x.PrefereableFloor);
                        break;
                    case "prefereableview":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.PrefereableView) : bookingQuery.OrderByDescending(x => x.PrefereableView);
                        break;
                    case "religions":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.Religions) : bookingQuery.OrderByDescending(x => x.Religions);
                        break;
                    case "nationalities":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.Nationalities) : bookingQuery.OrderByDescending(x => x.Nationalities);
                        break;
                    case "universities":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.Universities) : bookingQuery.OrderByDescending(x => x.Universities);
                        break;
                    case "agerange":
                        bookingQuery = request.orderDir == "asc" ? bookingQuery.OrderBy(x => x.AgeRange) : bookingQuery.OrderByDescending(x => x.AgeRange);
                        break;
                    default:
                        bookingQuery = bookingQuery.OrderByDescending(x => x.BookingDate);
                        break;
                }
            }

            else
            {
                // Default sorting if orderBy is null or empty
                bookingQuery = bookingQuery.OrderByDescending(x => x.BookingDate);
            }


            var Result = new BookingsResponse();
            var filteredQuery = bookingQuery.Where(x => x.Status.ToLower().Contains("allocated") ||
                    x.Status.ToLower().Contains("pending") && x.IsCancel != true && !x.Status.ToLower().Contains("expired")).ToList();

            if (!string.IsNullOrEmpty(request.search.value) && !string.IsNullOrEmpty(request.search.column) && request.query == null)
            {
                int TotalRecords = filteredQuery.Count();

                request.search.value = request.search.value.ToLower();
                switch (request.search.column.ToLower())
                {
                    case "locationname":
                        bookingQuery = bookingQuery.Where(x => x.LocationName.ToLower().Contains(request.search.value));
                        break;
                    case "bookingnumber":
                        bookingQuery = bookingQuery.Where(x => x.BookingNumber.ToLower().Contains(request.search.value));
                        break;
                    case "title":
                        bookingQuery = bookingQuery.Where(x => x.Title.ToLower().Contains(request.search.value));
                        break;
                    case "myriadid":
                        bookingQuery = bookingQuery.Where(x => x.MyriadID.ToLower().Contains(request.search.value));
                        break;
                    case "fullname":
                        bookingQuery = bookingQuery.Where(x => x.FullName.ToLower().Contains(request.search.value));
                        break;
                    case "gender":
                        bookingQuery = bookingQuery.Where(x => x.Gender.ToLower().Contains(request.search.value));
                        break;
                    case "commitment":
                        bookingQuery = bookingQuery.Where(x => x.Commitment.ToLower().Contains(request.search.value));
                        break;
                    case "roomtype":
                        bookingQuery = bookingQuery.Where(x => x.RoomType.ToLower().Contains(request.search.value));
                        break;
                    case "status":
                        bookingQuery = bookingQuery.Where(x => x.Status.ToLower().Contains(request.search.value));
                        break;
                    case "email":
                        bookingQuery = bookingQuery.Where(x => x.Email.ToLower().Contains(request.search.value));
                        break;
                    case "university":
                        bookingQuery = bookingQuery.Where(x => x.University.ToLower().Contains(request.search.value));
                        break;
                    case "phone":
                        bookingQuery = bookingQuery.Where(x => x.Phone.ToLower().Contains(request.search.value));
                        break;
                    case "channel":
                        bookingQuery = bookingQuery.Where(x => x.Channel.ToLower().Contains(request.search.value));
                        break;
                    case "source":
                        bookingQuery = bookingQuery.Where(x => x.Source.ToLower().Contains(request.search.value));
                        break;
                    case "unireferenceno":
                        bookingQuery = bookingQuery.Where(x => x.UniReferenceNo.ToLower().Contains(request.search.value));
                        break;
                    case "paymenttype":
                        bookingQuery = bookingQuery.Where(x => x.PaymentType.ToLower().Contains(request.search.value));
                        break;
                    case "accessibilityrequest":
                        bookingQuery = bookingQuery.Where(x => x.AccessibilityRequest.ToLower().Contains(request.search.value));
                        break;
                    case "bookingdate":
                        // Assuming BookingDate is a DateTime property
                        bookingQuery = bookingQuery.Where(x => x.BookingDate.ToString().Contains(request.search.value));
                        break;
                    case "hearfrom":
                        bookingQuery = bookingQuery.Where(x => x.HearFrom.ToLower().Contains(request.search.value));
                        break;
                    case "nationality":
                        bookingQuery = bookingQuery.Where(x => x.Nationality.ToLower().Contains(request.search.value));
                        break;

                    case "tenantpassportnumber":
                        bookingQuery = bookingQuery.Where(x => x.TenantPassportNumber.ToLower().Contains(request.search.value));
                        break;
                    case "guardianfullname":
                        bookingQuery = bookingQuery.Where(x => x.GuardianFullName.ToLower().Contains(request.search.value));
                        break;
                    case "guardianphone":
                        bookingQuery = bookingQuery.Where(x => x.GuardianPhone.ToLower().Contains(request.search.value));
                        break;
                    case "guardianemail":
                        bookingQuery = bookingQuery.Where(x => x.GuardianEmail.ToLower().Contains(request.search.value));
                        break;
                    case "guardianrelation":
                        bookingQuery = bookingQuery.Where(x => x.GuardianRelation.ToLower().Contains(request.search.value));
                        break;
                    case "prefereablefloor":
                        bookingQuery = bookingQuery.Where(x => x.PrefereableFloor.ToLower().Contains(request.search.value));
                        break;
                    case "prefereableview":
                        bookingQuery = bookingQuery.Where(x => x.PrefereableView.ToLower().Contains(request.search.value));
                        break;
                    case "religions":
                        bookingQuery = bookingQuery.Where(x => x.Religions.ToLower().Contains(request.search.value));
                        break;
                    case "nationalities":
                        bookingQuery = bookingQuery.Where(x => x.Nationalities.ToLower().Contains(request.search.value));
                        break;
                    case "universities":
                        bookingQuery = bookingQuery.Where(x => x.Nationalities.ToLower().Contains(request.search.value));
                        break;
                    case "agerange":
                        bookingQuery = bookingQuery.Where(x => x.AgeRange.ToLower().Contains(request.search.value));
                        break;

                    // Add more cases for other columns if needed
                    default:
                        bookingQuery = bookingQuery.Where(x =>
                            x.FullName.ToLower().Contains(request.search.value) ||
                            x.Email.ToLower().Contains(request.search.value) ||
                            x.Phone.ToLower().Contains(request.search.value) ||
                            x.LocationName.ToLower().Contains(request.search.value) ||
                            x.BookingNumber.ToLower().Contains(request.search.value) ||
                            x.Title.ToLower().Contains(request.search.value) ||
                            x.MyriadID.ToLower().Contains(request.search.value) ||
                            x.Gender.ToLower().Contains(request.search.value) ||
                            x.Commitment.ToLower().Contains(request.search.value) ||
                            x.RoomType.ToLower().Contains(request.search.value) ||
                            x.Status.ToLower().Contains(request.search.value) ||
                            x.University.ToLower().Contains(request.search.value) ||
                            x.Channel.ToLower().Contains(request.search.value) ||
                            x.Source.ToLower().Contains(request.search.value) ||
                            x.UniReferenceNo.ToLower().Contains(request.search.value) ||
                            x.PaymentType.ToLower().Contains(request.search.value) ||
                            x.AccessibilityRequest.ToLower().Contains(request.search.value) ||
                            x.HearFrom.ToLower().Contains(request.search.value) ||
                            x.Nationality.ToLower().Contains(request.search.value) ||
                            x.TenantPassportNumber.ToLower().Contains(request.search.value) ||
                            x.GuardianFullName.ToLower().Contains(request.search.value) ||
                            x.GuardianPhone.ToLower().Contains(request.search.value) ||
                            x.GuardianEmail.ToLower().Contains(request.search.value) ||
                            x.GuardianRelation.ToLower().Contains(request.search.value) ||
                            x.PrefereableFloor.ToLower().Contains(request.search.value) ||
                            x.PrefereableView.ToLower().Contains(request.search.value) ||
                            x.Religions.ToLower().Contains(request.search.value) ||
                            x.Nationalities.ToLower().Contains(request.search.value) ||
                            x.Universities.ToLower().Contains(request.search.value) ||
                            x.AgeRange.ToLower().Contains(request.search.value)


                        );
                        break;

                }


                int RecordsFiltered = bookingQuery.Count();
                var List = bookingQuery.Where(x => x.Status.ToLower().Contains("allocated") ||
                    x.Status.ToLower().Contains("pending") && x.IsCancel != true && !x.Status.ToLower().Contains("expired"))
                    .OrderByDescending(x => x.BookingDate).Skip(Int32.Parse(request.start)).Take(Int32.Parse(request.length)).ToList();
                Result.BookingList = List;
                Result.TotalRecords = TotalRecords;
                Result.RecordsFiltered = RecordsFiltered;
            }
            else if (!string.IsNullOrEmpty(request.search.value) && request.query == "Expired")
            {
                request.search.value = request.search.value.ToLower();
                bookingQuery = bookingQuery.Where(x =>
                    x.Commitment.ToLower().Contains(request.search.value) ||
                    x.RoomType.ToLower().Contains(request.search.value) ||
                    x.MyriadID.ToLower().Contains(request.search.value) ||
                    x.FullName.ToLower().Contains(request.search.value) ||
                    x.Email.ToLower().Contains(request.search.value) ||
                    x.Phone.ToLower().Contains(request.search.value) ||
                    x.Channel.ToLower().Contains(request.search.value) ||
                    x.Source.ToLower().Contains(request.search.value) ||
                    x.UniReferenceNo.ToLower().Contains(request.search.value) ||
                    x.PaymentType.ToLower().Contains(request.search.value) ||
                    x.BookingNumber.ToLower().Contains(request.search.value) ||
                    x.HearFrom.ToLower().Contains(request.search.value) ||
                    x.AccessibilityRequest.ToLower().Contains(request.search.value) ||
                    x.HearFrom.ToLower().Contains(request.search.value) ||
                    x.Nationality.ToLower().Contains(request.search.value) ||
                    x.TenantPassportNumber.ToLower().Contains(request.search.value) ||
                    x.GuardianFullName.ToLower().Contains(request.search.value) ||
                    x.GuardianPhone.ToLower().Contains(request.search.value) ||
                    x.GuardianEmail.ToLower().Contains(request.search.value) ||
                    x.GuardianRelation.ToLower().Contains(request.search.value) ||
                    x.PrefereableFloor.ToLower().Contains(request.search.value) ||
                    x.PrefereableView.ToLower().Contains(request.search.value) ||
                    x.Religions.ToLower().Contains(request.search.value) ||
                    x.Nationalities.ToLower().Contains(request.search.value) ||
                    x.Universities.ToLower().Contains(request.search.value) ||
                    x.AgeRange.ToLower().Contains(request.search.value)
                );


                var List = bookingQuery.Where(x => x.Status.ToLower().Contains("expired") && x.IsCancel == false).ToList();

                Result.BookingList = List;
                Result.RecordsFiltered = List.Count();
                Result.TotalRecords = List.Count();
            }
            else if (!string.IsNullOrEmpty(request.search.value) && request.query == "Cancelled")
            {
                int TotalRecords = bookingQuery.Count();

                request.search.value = request.search.value.ToLower();
                bookingQuery = bookingQuery.Where(x =>
                    x.Commitment.ToLower().Contains(request.search.value) ||
                    x.RoomType.ToLower().Contains(request.search.value) ||
                    x.MyriadID.ToLower().Contains(request.search.value) ||
                    x.FullName.ToLower().Contains(request.search.value) ||
                    x.Email.ToLower().Contains(request.search.value) ||
                    x.Phone.ToLower().Contains(request.search.value) ||
                    x.Channel.ToLower().Contains(request.search.value) ||
                    x.Source.ToLower().Contains(request.search.value) ||
                    x.UniReferenceNo.ToLower().Contains(request.search.value) ||
                    x.PaymentType.ToLower().Contains(request.search.value) ||
                    x.BookingNumber.ToLower().Contains(request.search.value) ||
                    x.HearFrom.ToLower().Contains(request.search.value) ||
                    x.AccessibilityRequest.ToLower().Contains(request.search.value) ||
                    x.HearFrom.ToLower().Contains(request.search.value) ||
                    x.Nationality.ToLower().Contains(request.search.value) ||
                    x.TenantPassportNumber.ToLower().Contains(request.search.value) ||
                    x.GuardianFullName.ToLower().Contains(request.search.value) ||
                    x.GuardianPhone.ToLower().Contains(request.search.value) ||
                    x.GuardianEmail.ToLower().Contains(request.search.value) ||
                    x.GuardianRelation.ToLower().Contains(request.search.value) ||
                    x.PrefereableFloor.ToLower().Contains(request.search.value) ||
                    x.PrefereableView.ToLower().Contains(request.search.value) ||
                    x.Religions.ToLower().Contains(request.search.value) ||
                    x.Nationalities.ToLower().Contains(request.search.value) ||
                    x.Universities.ToLower().Contains(request.search.value) ||
                    x.AgeRange.ToLower().Contains(request.search.value)
                );

                var List = bookingQuery.Where(x => x.IsCancel == true).ToList();

                Result.BookingList = List;
                Result.RecordsFiltered = List.Count();
                Result.TotalRecords = List.Count();
            }
            else if (!string.IsNullOrEmpty(request.search.value))
            {
                int TotalRecords = bookingQuery.Count();

                request.search.value = request.search.value.ToLower();
                bookingQuery = bookingQuery.Where(x =>
                    x.Commitment.ToLower().Contains(request.search.value) ||
                    x.RoomType.ToLower().Contains(request.search.value) ||
                    x.MyriadID.ToLower().Contains(request.search.value) ||
                    x.FullName.ToLower().Contains(request.search.value) ||
                    x.Email.ToLower().Contains(request.search.value) ||
                    x.Phone.ToLower().Contains(request.search.value) ||
                    x.Channel.ToLower().Contains(request.search.value) ||
                    x.Source.ToLower().Contains(request.search.value) ||
                    x.UniReferenceNo.ToLower().Contains(request.search.value) ||
                    x.PaymentType.ToLower().Contains(request.search.value) ||
                    x.BookingNumber.ToLower().Contains(request.search.value) ||
                    x.HearFrom.ToLower().Contains(request.search.value) ||
                    x.AccessibilityRequest.ToLower().Contains(request.search.value) ||
                    x.HearFrom.ToLower().Contains(request.search.value) ||
                    x.Nationality.ToLower().Contains(request.search.value) ||
                    x.TenantPassportNumber.ToLower().Contains(request.search.value) ||
                    x.GuardianFullName.ToLower().Contains(request.search.value) ||
                    x.GuardianPhone.ToLower().Contains(request.search.value) ||
                    x.GuardianEmail.ToLower().Contains(request.search.value) ||
                    x.GuardianRelation.ToLower().Contains(request.search.value) ||
                    x.PrefereableFloor.ToLower().Contains(request.search.value) ||
                    x.PrefereableView.ToLower().Contains(request.search.value) ||
                    x.Religions.ToLower().Contains(request.search.value) ||
                    x.Nationalities.ToLower().Contains(request.search.value) ||
                    x.Universities.ToLower().Contains(request.search.value) ||
                    x.AgeRange.ToLower().Contains(request.search.value)
                );

                var List = bookingQuery.ToList();

                Result.BookingList = List;
                Result.RecordsFiltered = List.Count();
                Result.TotalRecords = List.Count();
            }

            else if (request.query != null && request.query == "Expired" && string.IsNullOrEmpty(request.search.value))
            {
                var List = bookingQuery.Where(x => x.Status.ToLower().Contains("expired") && x.IsCancel == false).ToList();

                Result.BookingList = List.Skip(int.Parse(request.start ?? "0")).Take(int.Parse(request.length ?? "0")).ToList();
                Result.RecordsFiltered = List.Count();
                Result.TotalRecords = List.Count();
            }

            else if (request.query != null && request.query == "Cancelled" && string.IsNullOrEmpty(request.search.value))
            {
                var List = bookingQuery.Where(x => x.IsCancel == true).ToList();

                Result.BookingList = List.Skip(int.Parse(request.start ?? "0")).Take(int.Parse(request.length ?? "0")).ToList();
                Result.RecordsFiltered = List.Count();
                Result.TotalRecords = List.Count();
            }
            else if (request.query != null && request.query == "PlacementNotAssigned")
            {
                var List = bookingQuery.Where(x => x.Status.ToLower().Contains("pending") && x.IsCancel != true).ToList();

                Result.BookingList = List.Skip(int.Parse(request.start ?? "0")).Take(int.Parse(request.length ?? "0")).ToList();
                Result.RecordsFiltered = List.Count();
                Result.TotalRecords = List.Count();
            }

            else if (request.id > 0)
            {
                var List = bookingQuery.Where(x => x.BookingID == request.id).ToList();

                Result.BookingList = List.Skip(int.Parse(request.start ?? "0")).Take(int.Parse(request.length ?? "0")).ToList();
                Result.RecordsFiltered = List.Count();
                Result.TotalRecords = List.Count();
            }

            else if (request.personId > 0)
            {
                var List = bookingQuery.Where(x => x.PersonID == request.personId).ToList();

                Result.BookingList = List.Skip(int.Parse(request.start ?? "0")).Take(int.Parse(request.length ?? "0")).ToList();
                Result.RecordsFiltered = List.Count();
                Result.TotalRecords = List.Count();
            }

            else
            {
                var List = filteredQuery
                    .Skip(Int32.Parse(request.start))
                    .Take(Int32.Parse(request.length))
                    .ToList();
                int TotalRecords = filteredQuery.Count();

                Result.BookingList = List;
                Result.RecordsFiltered = filteredQuery.Count();
                Result.TotalRecords = filteredQuery.Count();
            }

            return Result;
        }

        public BookingsResponse GetPMSExportBookings(string QueryBy, string query = null, DateTime? FromDate = null, DateTime? ToDate = null)
        {
            uow.Context.Database.ExecuteSqlCommand("EXEC BookingExpired");
            var db = uow.Context;

            var _returnModel = new List<BookingListVM>();
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            IQueryable<BookingListVM> bookingQuery = from booking in db.Bookings
                                                     join person in db.People on booking.PersonID equals person.PersonID
                                                     join university in db.Universities on person.UniversityId equals university.Id into uni
                                                     from u in uni.DefaultIfEmpty()
                                                     join priceConfig in db.PriceConfigs on booking.PriceConfigID equals priceConfig.PriceConfigID into pri
                                                     from priceConfig in pri.DefaultIfEmpty()
                                                     join term in db.Terms on priceConfig.TermID equals term.TermID into tr
                                                     from term in tr.DefaultIfEmpty()
                                                     join roomType in db.RoomTypes on priceConfig.RoomTypeID equals roomType.RoomTypeID into rt
                                                     from roomType in rt.DefaultIfEmpty()
                                                     join emergencyContact in db.EmergencyContacts on person.PersonID equals emergencyContact.PersonID into ec
                                                     from emergencyContact in ec.DefaultIfEmpty()
                                                     join specialRequest in db.SpecialRequests on person.PersonID equals specialRequest.PersonID into sr
                                                     from specialRequest in sr.DefaultIfEmpty()
                                                     where booking.IsEnable == true && assignedLocationIds.Contains((int)booking.LocationID)
                                                     && (EntityFunctions.TruncateTime(booking.CheckInDate) >= EntityFunctions.TruncateTime(FromDate) || FromDate == null)
                                                     && (EntityFunctions.TruncateTime(booking.CheckInDate) <= EntityFunctions.TruncateTime(ToDate) || ToDate == null)

                                                     select new BookingListVM
                                                     {
                                                         BookingID = booking.BookingID,
                                                         LocationName = booking.Location.LocationName ?? "",
                                                         University = u.UniversityName,
                                                         PersonID = booking.PersonID,
                                                         Commitment = term.TermName,
                                                         RoomType = roomType.RoomCode + " - " + roomType.RoomName,
                                                         CheckInDate = booking.CheckInDate,
                                                         CheckOutDate = booking.CheckOutDate,
                                                         Title = person.Title,
                                                         MyriadID = person.Code,
                                                         FullName = person.FullName,
                                                         Email = person.Email,
                                                         Gender = person.Gender,
                                                         Phone = person.Phone,
                                                         IsCancel = booking.IsCancel,
                                                         Channel = booking.Channel,
                                                         Source = booking.Source,
                                                         UniReferenceNo = booking.UniReferenceNo,
                                                         HearFrom = booking.HearFrom,
                                                         PaymentType = booking.PaymentType,
                                                         BookingNumber = booking.BookingNumber,
                                                         AccessibilityRequest = booking.AccessibilityRequest,
                                                         BookingDate = booking.CreatedDate,
                                                         Status = (db.BedSpacePlacements.Any(x => x.IsEnable == true && x.BookingID == booking.BookingID) ? "Allocated" : (booking.Status == true) ? "Expired" : "Pending"),
                                                         Nationality = person.Nationality,
                                                         TenantPassportNumber = emergencyContact.PassportNumber,
                                                         GuardianFullName = emergencyContact.FullName,
                                                         GuardianPhone = emergencyContact.Phone,
                                                         GuardianEmail = emergencyContact.Email,
                                                         GuardianRelation = emergencyContact.Relation,
                                                         PrefereableView = specialRequest.PreferableView,
                                                         PrefereableFloor = specialRequest.PreferableFloor,
                                                         Religions = specialRequest.Religions,
                                                         Nationalities = specialRequest.Nationalities,
                                                         Universities = specialRequest.Universities,
                                                         AgeRange = specialRequest.Agerange
                                                     };

            var Result = new BookingsResponse();
            var filteredQuery = bookingQuery.Where(x => x.Status.ToLower().Contains("allocated") ||
                    x.Status.ToLower().Contains("pending") && x.IsCancel != true && !x.Status.ToLower().Contains("expired")).ToList();

            if (QueryBy != null && QueryBy != "" && (query == "null" || query == ""))
            {
                Result.BookingList = filteredQuery.OrderByDescending(x => x.BookingDate).ToList();
            }
            else if (query == "Expired")
            {
                Result.BookingList = bookingQuery.Where(x => x.Status.ToLower().Contains("expired") && x.IsCancel == false).ToList();
            }
            else if (query == "Cancelled")
            {
                Result.BookingList = bookingQuery.Where(x => x.IsCancel == true).ToList();
            }

            return Result;
        }

        public List<SelectListVM> GetPriceConfigurations()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var db = uow.Context;

            var data = (from priceConfig in db.PriceConfigs
                        join t in db.Terms
                        on priceConfig.TermID equals t.TermID
                        into tr
                        from term in tr.DefaultIfEmpty()
                        join rm in db.RoomTypes
                        on priceConfig.RoomTypeID equals rm.RoomTypeID
                        into rt
                        from roomType in rt.DefaultIfEmpty()
                        where priceConfig.IsEnable == true && priceConfig.IsAvailable == true && assignedLocationIds.Contains((int)priceConfig.LocationId)
                        select new SelectListVM
                        {
                            Text = term.TermName + " - " + roomType.RoomName + (term.University.Prefix == null ? "" : " - " + term.University.Prefix),
                            Value = priceConfig.PriceConfigID.ToString(),
                            MinDuration = term.Min_Duration,
                            TermEndDate = term.TermEndDate,
                            FrequencyId = term.FrequencyId

                        }).OrderBy(x => x.Text).ToList();

            return data;
        }

        public List<SelectListVM> GetWebsitePriceConfigurations()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var db = uow.Context;

            var data = (from priceConfig in db.PriceConfigs
                        join t in db.Terms
                        on priceConfig.TermID equals t.TermID
                        into tr
                        from term in tr.DefaultIfEmpty()
                        join rm in db.RoomTypes
                        on priceConfig.RoomTypeID equals rm.RoomTypeID
                        into rt
                        from roomType in rt.DefaultIfEmpty()
                        where priceConfig.IsEnable == true && priceConfig.IsAvailable == true /*&& term.IsPublished == true*/ && assignedLocationIds.Contains((int)priceConfig.LocationId)
                        select new SelectListVM
                        {
                            Text = term.TermName + " - " + roomType.RoomName + (term.University.Prefix == null ? "" : " - " + term.University.Prefix),
                            Value = priceConfig.PriceConfigID.ToString(),
                            MinDuration = term.Min_Duration,
                            TermEndDate = term.TermEndDate,
                            FrequencyId = term.FrequencyId

                        }).OrderBy(x => x.Text).ToList();

            return data;
        }

        public CommitmentDetailVM GetPriceConfigDetailByID(int id)
        {
            var db = uow.Context;

            var data = (from priceConfig in db.PriceConfigs
                        join t in db.Terms
                        on priceConfig.TermID equals t.TermID
                        into tr
                        from term in tr.DefaultIfEmpty()
                        join rm in db.RoomTypes
                        on priceConfig.RoomTypeID equals rm.RoomTypeID
                        into rt
                        from roomType in rt.DefaultIfEmpty()
                        where priceConfig.PriceConfigID == id
                        select new CommitmentDetailVM
                        {
                            Term = term.TermName,
                            TermDescription = term.TermDescription,
                            RoomType = roomType.RoomName,
                            RoomTypeDescription = roomType.RoomDescription,
                            Currency = priceConfig.Currency,
                            Price = priceConfig.Price,
                            CleaningCharge = priceConfig.CleaningCharge
                        }).ToList().FirstOrDefault();

            return data;
        }

        public GuestCountVm GetPlacementDetailID(int id)
        {
            var db = uow.Context;

            var data = (from bp in db.BedSpacePlacements
                        join bs in db.BedSpaces on bp.BedSpaceID equals bs.BedSpaceID
                        join booking in db.Bookings on bp.BookingID equals booking.BookingID
                        join r in db.Rooms on bs.RoomID equals r.RoomID
                        join rt in db.RoomTypes on r.RoomTypeID equals rt.RoomTypeID
                        where bp.IsEnable == true && booking.IsEnable == true
                              && booking.PersonID == id
                              && bp.CheckIn != null && bp.CheckOut == null
                        select new GuestCountVm
                        {
                            BedSpace = bs.BedName,
                            PersonId = booking.PersonID,
                            BedSpacePlacementID = bp.BedSpacePlacementID,
                            BookingID = bp.BookingID,
                            CheckIn = bp.CheckIn,
                            CheckOut = bp.CheckOut,
                            MoveIn = bp.MoveIn,
                            MoveOut = bp.MoveOut,
                            Room = r.RoomName,
                            RoomType = rt.RoomName
                        }).FirstOrDefault();

            return data;
        }


        public EF.Booking GetBookingByID(int id)
        {
            return uow.GenericRepository<EF.Booking>().GetById(id);
        }

        public EF.Booking AddBooking(AddBookingVM bookingVM)
        {
            var person = personService.GetPersonById(bookingVM.PersonID);
            EF.Booking booking = new EF.Booking();
            booking.PersonID = bookingVM.PersonID;
            booking.PriceConfigID = bookingVM.PriceConfigID;
            booking.CheckInDate = bookingVM.CheckInDate;
            booking.CheckOutDate = bookingVM.CheckOut;
            booking.IsCancel = false;
            booking.IsEnable = true;
            booking.CreatedDate = bookingVM.CreatedDate;
            booking.CreatedBy = bookingVM.CreatedBy;
            booking.AccessibilityRequest = bookingVM.Requests;
            booking.BookingNumber = Common.Globals.GetBookingNumber((int)person.LocationId);
            booking.LocationID = (int)person.LocationId;
            booking.Status = false;
            uow.GenericRepository<EF.Booking>().Insert(booking);
            uow.SaveChanges();


            var Oldbooking = new EF.Booking();
            //Insert Audit Log
            {
                var difference = Common.Classes.Common.DetailedCompare<EF.Booking>(Oldbooking, booking);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.CreateBooking,
                    PK = booking.BookingID.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "Booking",
                    Reference = booking.BookingNumber,
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = booking.PersonID,
                    AuditLogDetails = difference
                };
                auditLogsService.AddAuditLog(auditLog);
            }

            var NotifyEmail = correspondenceService.GetEmailMessagesByActionId(((int)Enumeration.CorrespondenceAction.CreateBooking), booking.LocationID ?? 0);
            if (NotifyEmail != null)
            {
                var body = NotifyEmail.EmailMessageBody;
                body = body.Replace("[[PersonID]]", person.Code);
                body = body.Replace("[[PersonFull_Name]]", person.FullName);
                body = body.Replace("[[BookingNumber]]", booking.BookingNumber);
                emailService.SendEmailAsync(Convert.ToString(NotifyEmail.EmailMessageSubject), body, true, person.Email, NotifyEmail.EmailMessageSenderID);
            }

            // Send Notification

            var Description = "Your Booking has been created against Booking Number: " + booking.BookingNumber;
            notificationService.SendNotification(null, booking.PersonID, "Student", "New Booking", Description, "/Student/Booking/BookingList", PMS.Common.Globals.User.Email);

            //END notification

            return booking;
        }


        public EF.Booking AddImportBooking(AddBookingVM bookingVM)
        {
            var priceConfigExists = uow.GenericRepository<EF.PriceConfig>()
    .Table.Any(x => x.PriceConfigID == bookingVM.PriceConfigID);

            if (!priceConfigExists)
            {
                throw new Exception($"PriceConfigID {bookingVM.PriceConfigID} does not exist in PriceConfig table.");
            }
            var person = personService.GetPersonById(bookingVM.PersonID);
            EF.Booking booking = new EF.Booking();
            booking.PersonID = bookingVM.PersonID;
            booking.PriceConfigID = bookingVM.PriceConfigID;
            booking.CheckInDate = bookingVM.CheckInDate;
            booking.CheckOutDate = bookingVM.CheckOut;
            booking.IsCancel = false;
            booking.IsEnable = true;
            booking.CreatedDate = bookingVM.CreatedDate;
            booking.CreatedBy = bookingVM.CreatedBy;
            booking.AccessibilityRequest = bookingVM.Requests;
            booking.BookingNumber = Common.Globals.GetBookingNumber((int)person.LocationId);
            booking.LocationID = (int)person.LocationId;
            booking.Status = false;
            uow.GenericRepository<EF.Booking>().Insert(booking);
            uow.SaveChanges();

            return booking;
        }


        public EF.Booking UpdateBooking(AddBookingVM bookingVM)
        {
            EF.Booking Oldbooking = uow.GenericRepository<EF.Booking>().GetByIdAsNoTracking(x => x.BookingID == bookingVM.BookingID);
            EF.Booking booking = GetBookingByID(bookingVM.BookingID);

            if (booking != null)
            {
                booking.PersonID = bookingVM.PersonID;
                booking.PriceConfigID = bookingVM.PriceConfigID;
                booking.CheckInDate = bookingVM.CheckInDate;
                booking.CheckOutDate = bookingVM.CheckOut;
                booking.IsEnable = true;
                booking.UpdatedDate = bookingVM.UpdatedDate;
                booking.UpdatedBy = bookingVM.UpdatedBy;
                booking.AccessibilityRequest = bookingVM.Requests;

                uow.GenericRepository<EF.Booking>().Update(booking);
                uow.SaveChanges();


                //eMAIL NOTIFICATION
                var NotifyEmail = correspondenceService.GetEmailMessagesByActionId(((int)Enumeration.CorrespondenceAction.UpdateBooking), booking.LocationID ?? 0);
                if (NotifyEmail != null)
                {
                    var body = NotifyEmail.EmailMessageBody;
                    body = body.Replace("[[PersonID]]", booking.Person.Code);
                    body = body.Replace("[[PersonFull_Name]]", booking.Person.FullName);
                    body = body.Replace("[[BookingNumber]]", booking.BookingNumber);
                    emailService.SendEmailAsync(Convert.ToString(NotifyEmail.EmailMessageSubject), body, true, booking.Person.Email, NotifyEmail.EmailMessageSenderID);
                }


                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Booking>(Oldbooking, booking);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateBooking,
                        PK = booking.BookingID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Booking",
                        Reference = booking.BookingNumber,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = booking.PersonID,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }


                return booking;
            }
            else
                throw new Exception("Booking not found to update.");
        }

        public bool DeleteBooking(int id)
        {
            EF.Booking Oldbooking = uow.GenericRepository<EF.Booking>().GetByIdAsNoTracking(x => x.BookingID == id);

            EF.Booking booking = GetBookingByID(id);
            var placement = uow.GenericRepository<BedSpacePlacement>().Table.Where(x => x.BookingID == id && x.IsEnable == true).FirstOrDefault();
            if (booking != null && placement == null)
            {
                booking.IsEnable = false;

                uow.GenericRepository<EF.Booking>().Update(booking);
                uow.SaveChanges();

                //email notification
                //var NotifyEmail = correspondenceService.GetEmailMessagesByActionId(((int)Enumeration.CorrespondenceAction.DeleteBooking), booking.LocationID ?? 0);
                //if (NotifyEmail != null)
                //{
                //    var body = NotifyEmail.EmailMessageBody;
                //    body = body.Replace("[[PersonID]]", booking.Person.Code);
                //    body = body.Replace("[[PersonFull_Name]]", booking.Person.FullName);
                //    body = body.Replace("[[BookingNumber]]", booking.BookingNumber);
                //    emailService.SendEmailAsync(Convert.ToString(NotifyEmail.EmailMessageSubject), body, true, booking.Person.Email, NotifyEmail.EmailMessageSenderID);
                //}

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Booking>(Oldbooking, booking);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteBooking,
                        PK = booking.BookingID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Booking",
                        Reference = booking.BookingNumber,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = booking.PersonID,

                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }


                return true;
            }
            else
                throw new Exception("Unable to delete booking. placement is available; please delete placement first.");
        }

        public bool CancelBooking(int id)
        {
            EF.Booking Oldbooking = uow.GenericRepository<EF.Booking>().GetByIdAsNoTracking(x => x.BookingID == id);

            EF.Booking booking = GetBookingByID(id);

            if (booking != null)
            {
                booking.IsCancel = (booking.IsCancel == null || booking.IsCancel == false) ? true : false;

                uow.GenericRepository<EF.Booking>().Update(booking);
                uow.SaveChanges();

                if (booking.IsCancel == true)
                {
                    var NotifyEmail = correspondenceService.GetEmailMessagesByActionId(((int)Enumeration.CorrespondenceAction.CancelBooking), booking.LocationID ?? 0);
                    if (NotifyEmail != null)
                    {
                        var body = NotifyEmail.EmailMessageBody;
                        body = body.Replace("[[PersonID]]", booking.Person.Code);
                        body = body.Replace("[[PersonFull_Name]]", booking.Person.FullName);
                        body = body.Replace("[[BookingNumber]]", booking.BookingNumber);
                        emailService.SendEmailAsync(Convert.ToString(NotifyEmail.EmailMessageSubject), body, true, booking.Person.Email, NotifyEmail.EmailMessageSenderID);
                    }
                }
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Booking>(Oldbooking, booking);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.CancelBooking,
                        PK = booking.BookingID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Booking",
                        Reference = booking.BookingNumber,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = booking.PersonID,

                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }



                return true;
            }
            else
                throw new Exception("Booking not found to change cancel status.");
        }
        public bool SavePersonGuest(GuestCountVm personGuest)
        {
            try
            {
                foreach (var guest in personGuest.Guests)
                {
                    if (guest.ImageSource != null)
                    {
                        var file = guest.ImageSource;
                        Common.ImageUpload upload = new Common.ImageUpload();

                        var result = Common.ImageUpload.SaveFile(file, "GuestDocuments");

                        guest.ImageUrl = "/Upload/Files/GuestDocuments/" + result;
                    }

                    var newGuest = new EF.PersonGuest
                    {
                        BedSpacePlacementID = personGuest.BedSpacePlacementID,
                        GuestCount = guest.GuestCount,
                        Description = guest.Description,
                        IDNumber = guest.IDNumber,
                        GuestName = guest.GuestName,
                        CurrentDateTime = guest.CurrentDateTime,
                        VisitorCardNumber = guest.VisitorCardNumber,
                        ImageUrl = guest.ImageUrl,
                        CreatedDate = DateTime.Now,
                        CreatedBy = PMS.Common.Globals.User.Email
                    };

                    uow.GenericRepository<EF.PersonGuest>().Insert(newGuest);
                }
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool CheckOutGuest(GuestDetailVm headCountVM)
        {
            try
            {
                var res = uow.GenericRepository<EF.PersonGuest>().GetAll()
                             .FirstOrDefault(x => x.ID == headCountVM.ID);

                if (res != null)
                {
                    res.CheckOutGuest = DateTime.Now;

                    uow.GenericRepository<EF.PersonGuest>().Update(res);
                    uow.SaveChanges();
                }
                return true;
            }
            catch (Exception ex)
            {
                // Log exception
                return false;
            }
        }
        //api services
        public ApiResponse<List<BookingListVM>> GetBooking(int Id)
        {
            var response = new ApiResponse<List<BookingListVM>>();
            try
            {
                var studentId = uow.GenericRepository<UserMaster>().Table.Where(x => x.ID == Id).Select(x => x.PersonID).FirstOrDefault();
                //var data = GetBookings(0).Where(x => x.PersonID == studentId).ToList();
                response.Code = (int)HttpStatusCode.OK;
                response.Success = true;
                response.Message = "success";
                //response.Data = data;
                return response;
            }
            catch (Exception ex)
            {
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Success = false;
                response.Message = ex.Message;
                response.Data = null;
                return response;
            }
        }
        public UploadReceiptVM GetBookingData(int personid, int bookingid)
        {
            var res = uow.GenericRepository<EF.Booking>().Table.Where(x => x.PersonID == personid && x.BookingID == bookingid).Select(x => new UploadReceiptVM
            {
                ImageUrl = x.ImageUrl,
                PersonId = personid,
                BookingId = bookingid


            }).FirstOrDefault();
            return res;
        }

        public UploadReceiptVM GetReceipt(int bookingid)
        {
            var receipt = uow.GenericRepository<EF.Booking>().Table.Where(x => x.BookingID == bookingid).Select(x => new UploadReceiptVM
            {
                BookingId = x.BookingID,
                ThumbnailImageUrl = x.ImageUrl,

            }).FirstOrDefault();
            return receipt;
        }
        public bool AddReceipt(UploadReceiptVM uploadReceiptVM)
        {
            try
            {
                var bookingDetail = uow.GenericRepository<EF.Booking>().GetById(uploadReceiptVM.BookingId);
                if (uploadReceiptVM.ThumbnailImage != null)
                {
                    ImageResult result = new ImageResult();
                    Common.ImageUpload upload = new Common.ImageUpload()
                    {
                        Width = 2250,
                        Height = 508,
                        Quality = 80
                    };
                    result = upload.RenameUploadFile(uploadReceiptVM.ThumbnailImage);

                    if (!result.Success)
                        return false;
                    bookingDetail.ImageUrl = result.ImageName;
                }
                uow.GenericRepository<EF.Booking>().Update(bookingDetail);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool Checkupload(int Personid, int bookingid)
        {
            var res = uow.GenericRepository<EF.Booking>().Table
                 .FirstOrDefault(x => x.PersonID == Personid && x.BookingID == bookingid && x.ImageUrl != null);
            if (res != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public IQueryable<EF.Booking> GetBookingQueryable()
        {
            return uow.GenericRepository<EF.Booking>().Table;
        }
    }
}
