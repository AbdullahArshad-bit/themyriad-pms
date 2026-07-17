using Newtonsoft.Json;
using PMS.Common.Classes;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.DTO.ViewModels.NetIntViewModel;
using PMS.DTO.ViewModels.PersonManageViewModels;
using PMS.DTO.ViewModels.TTLockViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.AuditLogs;
using PMS.Services.Services.Booking;
using PMS.Services.Services.Correspondence;
using PMS.Services.Services.Email;
using PMS.Services.Services.LocationContext;
using PMS.Services.Services.Notifications;
using PMS.Services.Services.Person;
using PMS.Services.Services.Setup;
using PMS.Services.Services.TTLockIntegration;
using PMS.Services.Services.UserManage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using System.Web;
using static PMS.Common.Classes.Enumeration;
using static System.Net.WebRequestMethods;

namespace PMS.Services.Services.BedSpacePlace
{
    public class BedSpacePlacementService : IBedSpacePlacementService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ICorrespondenceService correspondenceService;
        private readonly IEmailService emailService;
        private readonly IPersonService personService;
        private readonly IAuditLogsService auditLogsService;
        private readonly IBookingService bookingService;
        private readonly IUserManageService UserManageService;
        private readonly INotificationService notificationService;
        private readonly ISetupService setupService;
        private readonly ITTLockAuth iTTLockAuth;
        private readonly ILocationContextService locationContextService;


        public BedSpacePlacementService(UnitOfWork<PMSEntities> _uow, ICorrespondenceService _correspondenceService, IEmailService _emailService,
            IPersonService _personService, IBookingService _bookingService, IAuditLogsService _auditLogsService, IUserManageService _UserManageService,
            INotificationService _notificationService, ISetupService _setupService, ITTLockAuth _iTTLockAuth, ILocationContextService _locationContextService)
        {
            uow = _uow;
            correspondenceService = _correspondenceService;
            emailService = _emailService;
            personService = _personService;
            auditLogsService = _auditLogsService;
            bookingService = _bookingService;
            UserManageService = _UserManageService;
            notificationService = _notificationService;
            setupService = _setupService;
            iTTLockAuth = _iTTLockAuth;
            locationContextService = _locationContextService;
        }

        #region Get
        public PlacementsResponse GetPlacements(PlacementsBinding request, string QueryBY, string searchValue, string start, string lenght, string query = null, DateTime? FromDate = null, DateTime? ToDate = null, int personId = 0, int id = 0, string orderBy = null, string orderDir = "asc")
        {
            var db = uow.Context;
            try
            {

                var assignedLocationIds = locationContextService.GetAssignedLocationIds();

                IQueryable<V_PlacementList> data = uow.GenericRepository<V_PlacementList>().Table
                         .Where(x => assignedLocationIds.Contains((int)x.LocationID));

                if (personId > 0)
                {
                    data = data.Where(x => x.PersonID == personId && x.BedSpacePlacementIsEnable == true);
                }
                else if (id > 0)
                {
                    data = data.Where(x => x.BedSpacePlacementID == id && x.BedSpacePlacementIsEnable == true);
                }
                else
                {

                    switch (query)
                    {
                        case "TodayCheck":
                            data = data.Where(x => x.CheckIn != null && EntityFunctions.TruncateTime(x.CheckIn) == EntityFunctions.TruncateTime(DateTime.Today));
                            break;
                        case "TodayCheckOut":
                            data = data.Where(x => x.CheckOut != null && EntityFunctions.TruncateTime(x.CheckOut) == EntityFunctions.TruncateTime(DateTime.Today));
                            break;
                        case "TodayCheckinpending":
                            data = data.Where(x => x.CheckIn == null && x.MoveIn == DateTime.Today);
                            break;
                        case "TodayCheckoutpending":
                            data = data.Where(x => x.CheckOut == null && x.MoveOut == DateTime.Today);
                            break;
                        case "NotCheckedIn":
                            data = data.Where(x => x.CheckIn == null && x.CheckOut == null);
                            break;
                        case "NotCheckedOut":
                            data = data.Where(x => x.CheckOut == null);
                            break;
                        case "NoContract":
                            data = data.Where(x => !(from con in db.StudentContracts select con.PlacementId).Contains(x.BedSpacePlacementID));
                            break;

                        case "inHouse":
                            data = data.Where(x => (EntityFunctions.TruncateTime(x.BedSpacePlacementCreatedDate) >= EntityFunctions.TruncateTime(FromDate) || FromDate == null)
                                                && (EntityFunctions.TruncateTime(x.BedSpacePlacementCreatedDate) <= EntityFunctions.TruncateTime(ToDate) || ToDate == null)
                                                && (request.termID == 0 || x.PriceConfigID == request.termID) && (query == null || (x.CheckIn != null && x.CheckOut == null)));
                            break;

                        default:
                            data = data.Where(x => (EntityFunctions.TruncateTime(x.BedSpacePlacementCreatedDate) >= EntityFunctions.TruncateTime(FromDate) || FromDate == null)
                                                && (EntityFunctions.TruncateTime(x.BedSpacePlacementCreatedDate) <= EntityFunctions.TruncateTime(ToDate) || ToDate == null)
                                                && (request.termID == 0 || x.PriceConfigID == request.termID));
                            break;
                    }
                }

                var Result = new PlacementsResponse();

                if (QueryBY != null && QueryBY != "")
                {
                    Result.PlacementList = data.ToList();

                }
                else
                {
                    int TotalRecords = data.Count();
                    if (!string.IsNullOrEmpty(request.search.value) && !string.IsNullOrEmpty(request.search.column))
                    {
                        request.search.value = request.search.value.ToLower();
                        switch (request.search.column.ToLower())
                        {
                            case "locationname":
                                data = data.Where(x => x.LocationName.ToLower().Contains(request.search.value));
                                break;
                            case "title":
                                data = data.Where(x => x.Title.ToLower().Contains(request.search.value));
                                break;
                            case "myriadid":
                                data = data.Where(x => x.MyriadID.ToLower().Contains(request.search.value));
                                break;
                            case "fullname":
                                data = data.Where(x => x.FullName.ToLower().Contains(request.search.value));
                                break;
                            case "gender":
                                data = data.Where(x => x.Gender.ToLower().Contains(request.search.value));
                                break;
                            case "block":
                                data = data.Where(x => x.Block.ToLower().Contains(request.search.value));
                                break;
                            case "bedspace":
                                data = data.Where(x => x.BedSpace.ToLower().Contains(request.search.value));
                                break;
                            case "room":
                                data = data.Where(x => x.Room.ToLower().Contains(request.search.value));
                                break;
                            case "roomtype":
                                data = data.Where(x => x.RoomType.ToLower().Contains(request.search.value));
                                break;
                            case "commitment":
                                data = data.Where(x => x.Commitment.ToLower().Contains(request.search.value));
                                break;
                            case "price":
                                data = data.Where(x => x.Price.ToString().Contains(request.search.value));
                                break;
                            case "email":
                                data = data.Where(x => x.Email.ToLower().Contains(request.search.value));
                                break;
                            case "phone":
                                data = data.Where(x => x.Phone.ToLower().Contains(request.search.value));
                                break;
                            case "university":
                                data = data.Where(x => x.University.ToLower().Contains(request.search.value));
                                break;
                            case "requests":
                                data = data.Where(x => x.Requests.ToLower().Contains(request.search.value));
                                break;
                            case "billedupto":
                                data = data.Where(x => x.BilledUpto != null && x.CheckOut == null &&
                                    (SqlFunctions.DatePart("day", x.BilledUpto).ToString() + "/" +
                                     SqlFunctions.DatePart("month", x.BilledUpto).ToString() + "/" +
                                     SqlFunctions.DatePart("year", x.BilledUpto).ToString()).Contains(request.search.value));
                                break;

                                //case "billedupto":
                                //    // Get the data first, then search on client side
                                //    var tempData = data.Where(x => x.BilledUpto != null && x.CheckOut == null).AsEnumerable();

                                //    data = tempData.Where(x =>
                                //    {
                                //        var dateStr = x.BilledUpto.Value.Day + "/" + x.BilledUpto.Value.Month + "/" + x.BilledUpto.Value.Year;
                                //        return dateStr.Contains(request.search.value);
                                //    }).AsQueryable();
                                //    break;

                                //case "billedupto":
                                //    // Pre-filter to reduce dataset, then apply search
                                //    data = data.Where(x => x.BilledUpto.HasValue)
                                //               .Where(x => SqlFunctions.StringConvert((double)SqlFunctions.DatePart("day", x.BilledUpto)).Trim() + "/" +
                                //                          SqlFunctions.StringConvert((double)SqlFunctions.DatePart("month", x.BilledUpto)).Trim() + "/" +
                                //                          SqlFunctions.StringConvert((double)SqlFunctions.DatePart("year", x.BilledUpto)).Trim()
                                //                          == searchValue ||
                                //                          (SqlFunctions.StringConvert((double)SqlFunctions.DatePart("day", x.BilledUpto)).Trim() + "/" +
                                //                           SqlFunctions.StringConvert((double)SqlFunctions.DatePart("month", x.BilledUpto)).Trim()).Contains(searchValue));
                                //    break;
                        }
                    }
                    else if (!string.IsNullOrEmpty(searchValue))
                    {
                        data = data.Where(x =>
                        x.LocationName.Contains(searchValue) ||
                        x.Title.Contains(searchValue) ||
                        x.MyriadID.Contains(searchValue) ||
                        x.FullName.Contains(searchValue) ||
                        x.Gender.Contains(searchValue) ||
                        x.Block.Contains(searchValue) ||
                        x.BedSpace.Contains(searchValue) ||
                        x.Room.Contains(searchValue) ||
                        x.RoomType.Contains(searchValue) ||
                        x.Commitment.Contains(searchValue) ||
                        x.Price.ToString().Contains(searchValue) ||
                        x.Email.Contains(searchValue) ||
                        x.Phone.Contains(searchValue) ||
                        x.University.Contains(searchValue) ||
                        x.Requests.Contains(searchValue));
                    }
                    else
                    {
                        data = data.OrderByDescending(x => x.BedSpacePlacementCreatedDate);
                    }

                    int RecordsFiltered = data.Count();
                    List<string> selectedColumn = request.SelectedColumns ?? new List<string>();

                    List<string> allColumns = new List<string>
{
    "LocationName", "Title", "MyriadID", "FullName", "Gender","Block", "BedSpace", "Room", "RoomType",
    "Commitment","Price","BilledUpto", "MoveIn", "MoveOut", "CheckIn", "CheckOut", "Email", "Phone","University", "DOB",
    "Requests", "BedSpacePlacementID"
};
                    List<string> unselectedColumns = allColumns.Except(selectedColumn).ToList();
                    if (!string.IsNullOrEmpty(orderBy))
                    {
                        switch (orderBy)
                        {
                            case "LocationName":
                                data = orderDir == "asc" ? data.OrderBy(x => x.LocationName) : data.OrderByDescending(x => x.LocationName);
                                break;
                            case "Title":
                                data = orderDir == "asc" ? data.OrderBy(x => x.Title) : data.OrderByDescending(x => x.Title);
                                break;
                            case "MyriadID":
                                data = orderDir == "asc" ? data.OrderBy(x => x.MyriadID) : data.OrderByDescending(x => x.MyriadID);
                                break;
                            case "FullName":
                                data = orderDir == "asc" ? data.OrderBy(x => x.MyriadID) : data.OrderByDescending(x => x.MyriadID);
                                break;
                            case "Gender":
                                data = orderDir == "asc" ? data.OrderBy(x => x.Gender) : data.OrderByDescending(x => x.Gender);
                                break;
                            case "Block":
                                data = orderDir == "asc" ? data.OrderBy(x => x.Block) : data.OrderByDescending(x => x.Block);
                                break;
                            case "BedSpace":
                                data = orderDir == "asc" ? data.OrderBy(x => x.BedSpace) : data.OrderByDescending(x => x.BedSpace);
                                break;
                            case "Room":
                                data = orderDir == "asc" ? data.OrderBy(x => x.Room) : data.OrderByDescending(x => x.Room);
                                break;
                            case "RoomType":
                                data = orderDir == "asc" ? data.OrderBy(x => x.RoomType) : data.OrderByDescending(x => x.RoomType);
                                break;
                            case "Commitment":
                                data = orderDir == "asc" ? data.OrderBy(x => x.Commitment) : data.OrderByDescending(x => x.Commitment);
                                break;
                            case "Price":
                                data = orderDir == "asc" ? data.OrderBy(x => x.Price) : data.OrderByDescending(x => x.Commitment);
                                break;
                            case "BilledUpto":
                                data = orderDir == "asc" ? data.OrderBy(x => x.BilledUpto) : data.OrderByDescending(x => x.BilledUpto);
                                break;
                            case "MoveIn":
                                data = orderDir == "asc" ? data.OrderBy(x => x.MoveIn) : data.OrderByDescending(x => x.MoveIn);
                                break;
                            case "MoveOut":
                                data = orderDir == "asc" ? data.OrderBy(x => x.MoveOut) : data.OrderByDescending(x => x.MoveOut);
                                break;
                            case "CheckIn":
                                data = orderDir == "asc" ? data.OrderBy(x => x.CheckIn) : data.OrderByDescending(x => x.CheckIn);
                                break;
                            case "CheckOut":
                                data = orderDir == "asc" ? data.OrderBy(x => x.CheckOut) : data.OrderByDescending(x => x.CheckOut);
                                break;
                            case "Email":
                                data = orderDir == "asc" ? data.OrderBy(x => x.Email) : data.OrderByDescending(x => x.Email);
                                break;
                            case "Phone":
                                data = orderDir == "asc" ? data.OrderBy(x => x.Phone) : data.OrderByDescending(x => x.Phone);
                                break;
                            case "University":
                                data = orderDir == "asc" ? data.OrderBy(x => x.University) : data.OrderByDescending(x => x.University);
                                break;
                            case "DOB":
                                data = orderDir == "asc" ? data.OrderBy(x => x.DOB) : data.OrderByDescending(x => x.DOB);
                                break;
                            case "Requests":
                                data = orderDir == "asc" ? data.OrderBy(x => x.Requests) : data.OrderByDescending(x => x.Requests);
                                break;
                            case "BedSpacePlacementID":
                                data = orderDir == "asc" ? data.OrderBy(x => x.BedSpacePlacementID) : data.OrderByDescending(x => x.BedSpacePlacementID);
                                break;
                            default:
                                data = data.OrderByDescending(x => x.BedSpacePlacementCreatedDate);
                                break;
                        }
                    }
                    else
                    {
                        // Default sorting if orderBy is null or empty
                        data = data.OrderByDescending(x => x.BedSpacePlacementCreatedDate);
                    }
                    var placementListData = data.Skip(Int32.Parse(start)).Take(Int32.Parse(lenght)).Select(placement => new
                    {
                        LocationName = unselectedColumns.Contains("LocationName") ? placement.LocationName : default,
                        Title = unselectedColumns.Contains("Title") ? placement.Title : default,
                        MyriadID = unselectedColumns.Contains("MyriadID") ? placement.MyriadID : default,
                        FullName = unselectedColumns.Contains("FullName") ? placement.FullName : default,
                        Gender = unselectedColumns.Contains("Gender") ? placement.Gender : default,
                        Block = unselectedColumns.Contains("Block") ? placement.Block : default,
                        BedSpace = unselectedColumns.Contains("BedSpace") ? placement.BedSpace : default,
                        Room = unselectedColumns.Contains("Room") ? placement.Room : default,
                        RoomType = unselectedColumns.Contains("RoomType") ? placement.RoomType : default,
                        Commitment = unselectedColumns.Contains("Commitment") ? placement.Commitment : default,
                        Price = unselectedColumns.Contains("Price") ? placement.Price : default,
                        BilledUpto = unselectedColumns.Contains("BilledUpto") ? placement.BilledUpto : default,
                        MoveIn = unselectedColumns.Contains("MoveIn") ? placement.MoveIn : default,
                        MoveOut = unselectedColumns.Contains("MoveOut") ? placement.MoveOut : default,
                        CheckIn = unselectedColumns.Contains("CheckIn") ? placement.CheckIn : default,
                        CheckOut = unselectedColumns.Contains("CheckOut") ? placement.CheckOut : default,
                        Email = unselectedColumns.Contains("Email") ? placement.Email : default,
                        Phone = unselectedColumns.Contains("Phone") ? placement.Phone : default,
                        DOB = unselectedColumns.Contains("DOB") ? placement.DOB : default,
                        University = unselectedColumns.Contains("University") ? placement.University : default,
                        Requests = unselectedColumns.Contains("Requests") ? placement.Requests : default,
                        BedSpacePlacementCreatedDate = placement.BedSpacePlacementCreatedDate,
                        BedSpacePlacementID = placement.BedSpacePlacementID,
                        BookingId = placement.BookingID,
                        PersonID = placement.PersonID,
                        LocationID = placement.LocationID
                    }).OrderByDescending(x => x.BedSpacePlacementCreatedDate).ToList();

                    Result.PlacementList = placementListData.Select(x => new V_PlacementList
                    {
                        LocationName = x.LocationName,
                        Title = x.Title,
                        MyriadID = x.MyriadID,
                        FullName = x.FullName,
                        Gender = x.Gender,
                        Block = x.Block,
                        BedSpace = x.BedSpace,
                        Room = x.Room,
                        RoomType = x.RoomType,
                        Commitment = x.Commitment,
                        Price = x.Price,
                        BilledUpto = x.BilledUpto,
                        MoveIn = x.MoveIn,
                        MoveOut = x.MoveOut,
                        CheckIn = x.CheckIn,
                        CheckOut = x.CheckOut,
                        Email = x.Email,
                        Phone = x.Phone,
                        DOB = x.DOB,
                        University = x.University,
                        Requests = x.Requests,
                        BedSpacePlacementCreatedDate = x.BedSpacePlacementCreatedDate,
                        BedSpacePlacementID = x.BedSpacePlacementID,
                        BookingID = x.BookingId,
                        PersonID = x.PersonID,
                        LocationID = x.LocationID,


                    }).OrderByDescending(x => x.BedSpacePlacementCreatedDate).ToList();
                    Result.TotalRecords = TotalRecords;
                    Result.RecordsFiltered = RecordsFiltered;
                }


                return Result;
            }

            catch (Exception ex)
            {

            }
            return null;
        }

        public BedSpacePlacement GetBedSpacePlacementById(int id)
        {
            return uow.GenericRepository<BedSpacePlacement>().GetById(id);
        }

        public async Task<PlacementsResponse> GetPlacementsExportAsync(string QueryBY, string query = null, DateTime? FromDate = null, DateTime? ToDate = null)
        {
            try
            {
                IQueryable<V_PlacementList> data = uow.GenericRepository<V_PlacementList>().Table;

                // Sorting at the database level
                data = data.OrderByDescending(x => x.BedSpacePlacementCreatedDate);

                var result = new PlacementsResponse
                {
                    PlacementList = await data.ToListAsync()
                };

                return result;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error fetching placements data", ex);
            }
        }

        public List<PlacementsListVM> GetNoContractPlacements(int personId = 0)
        {
            var db = uow.Context;

            try
            {
                var data = (from placemnet in db.BedSpacePlacements

                            join b in db.Bookings
                            on placemnet.BookingID equals b.BookingID
                            into bk
                            from booking in bk.DefaultIfEmpty()

                            join p in db.People
                            on booking.PersonID equals p.PersonID
                            into pr
                            from person in pr.DefaultIfEmpty()

                            join bs in db.BedSpaces
                            on placemnet.BedSpaceID equals bs.BedSpaceID
                            into bsp
                            from bedSpace in bsp.DefaultIfEmpty()

                            join r in db.Rooms
                            on bedSpace.RoomID equals r.RoomID
                            into rm
                            from room in rm.DefaultIfEmpty()

                            where placemnet.IsEnable == true &&
                            !(from con in db.StudentContracts
                              select con.PlacementId).Contains(placemnet.BedSpacePlacementID)

                            select new PlacementsListVM
                            {
                                BedSpacePlacementID = placemnet.BedSpacePlacementID,
                                BookingID = placemnet.BookingID,
                                PersonID = person.PersonID,
                                ReferralCode = person.ReferralCode,
                                MoveIn = placemnet.MoveIn,
                                MoveOut = placemnet.MoveOut,
                                CheckIn = placemnet.CheckIn,
                                CheckOut = placemnet.CheckOut,
                                Title = person.Title,
                                FullName = person.FullName,
                                Gender = person.Gender,
                                Phone = person.Phone,
                                Email = person.Email,
                                BedSpace = bedSpace.BedName,
                                Room = room.RoomName,
                                RoomType = room.RoomType.RoomName,
                                Requests = placemnet.AccessibilityRequest,
                                Commitment = booking.PriceConfig.Term.TermName,
                                BedspacePlacementIsEnable = placemnet.IsEnable
                            });

                if (personId > 0)
                    data = data.Where(x => x.PersonID == personId);

                return data.ToList();
            }

            catch (Exception ex)
            {

            }

            return null;

        }

        public List<PlacementsListVM> GetCheckin(int personId = 0)
        {
            var db = uow.Context;
            try
            {
                var data = (from placemnet in db.BedSpacePlacements

                            join b in db.Bookings
                            on placemnet.BookingID equals b
                            .BookingID
                            into bk
                            from booking in bk.DefaultIfEmpty()

                            join p in db.People
                            on booking.PersonID equals p.PersonID
                            into pr
                            from person in pr.DefaultIfEmpty()

                            join bs in db.BedSpaces
                            on placemnet.BedSpaceID equals bs.BedSpaceID
                            into bsp
                            from bedSpace in bsp.DefaultIfEmpty()

                            join r in db.Rooms
                            on bedSpace.RoomID equals r.RoomID
                            into rm
                            from room in rm.DefaultIfEmpty()

                            where placemnet.IsEnable == true

                            select new PlacementsListVM
                            {
                                BedSpacePlacementID = placemnet.BedSpacePlacementID,
                                BookingID = placemnet.BookingID,
                                PersonID = person.PersonID,
                                ReferralCode = person.ReferralCode,
                                MoveIn = placemnet.MoveIn,
                                MoveOut = placemnet.MoveOut,
                                CheckIn = placemnet.CheckIn,
                                CheckOut = placemnet.CheckOut,
                                Title = person.Title,
                                FullName = person.FullName,
                                Gender = person.Gender,
                                Phone = person.Phone,
                                Email = person.Email,
                                BedSpace = bedSpace.BedName,
                                Room = room.RoomName,
                                RoomType = room.RoomType.RoomName,
                                Requests = placemnet.AccessibilityRequest,
                                Commitment = booking.PriceConfig.Term.TermName

                            });

                if (personId > 0)
                    data = data.Where(x => x.PersonID == personId);

                return data.Where(a => a.CheckIn == null && a.CheckOut == null).ToList();
            }

            catch (Exception ex)
            {

            }

            return null;

        }

        public List<SelectListVM> GetAvailableBedSpaces()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();


            var db = uow.Context;

            var data = (from bed in db.BedSpaces

                        join b in db.BedSpacePlacements
                        on bed.BedSpaceID equals b.BedSpaceID
                        into bsp
                        from bedSpacePlacement in bsp.DefaultIfEmpty()

                        join r in db.Rooms
                        on bed.RoomID equals r.RoomID

                        into rm

                        from room in rm.DefaultIfEmpty().OrderBy(x => x.RoomID)
                        where (bedSpacePlacement.BedSpacePlacementID == null
                        || (0 == db.BedSpacePlacements.Count(x => x.IsEnable == true && x.BedSpaceID == bed.BedSpaceID && x.CheckOut == null)))
                        && bed.IsEnable == true && bed.Status == true

                        select new
                        {
                            bed.BedName,
                            bed.BedSpaceID,
                            room.RoomName,
                            bed.Room.Floor.Building.BuildingName,
                            bed.Room.Floor.Building.Project.LocationID,
                            bed.Room.Floor.FloorName,
                            RoomType = room.RoomType.RoomName,
                            room.RoomID

                        }).OrderByDescending(x => x.RoomID).GroupBy(x => x.BedSpaceID).Select(y => y.FirstOrDefault()).Select(z => new SelectListVM

                        {
                            Text = z.BuildingName + "-" + z.FloorName + "-" + z.RoomName + "-" + z.BedName + "-" + z.RoomType.Substring(0, 3).ToUpper(),
                            Value = z.BedSpaceID.ToString(),
                            OrderBy = z.RoomID,
                            LocationId = z.LocationID,
                            RoomTypeName = z.RoomType,
                            BuildingName = z.BuildingName,
                            FloorName = z.FloorName,
                            RoomName = z.RoomName,
                            BedName = z.BedName,
                            BedSpaceID = z.BedSpaceID,
                        });

            var dat = data.ToList();
            dat = dat.Where(x => assignedLocationIds.Contains((int)x.LocationId)).OrderBy(x => x.OrderBy).ToList();
            return dat;
        }

        public List<SelectListVM> GetAvailableBedSpacesForRoomType(string roomTypeName)
        {
            //var availableBedSpaces = GetAvailableBedSpaces().Where(a => a.RoomTypeName.ToLower() == roomTypeName.ToLower()).ToList();

            var availableBedSpaces = GetAvailableBedSpaces()
      .Where(a => a.RoomTypeName.Equals(roomTypeName, StringComparison.OrdinalIgnoreCase)) // Filter by room type
      .ToList();

            // Group available bedspaces by room
            var groupedBedSpaces = availableBedSpaces
                .GroupBy(b => new { b.BuildingName, b.FloorName, b.RoomName })
                .Select(group => new
                {
                    Room = group.Key,
                    TotalBeds = group.Count(),
                    AvailableBeds = group.Count(b => b.BedSpaceID != 0)
                })
                .OrderBy(group => group.AvailableBeds)
                .ThenByDescending(group => group.TotalBeds);

            var selectedBedSpaces = new List<SelectListVM>();
            foreach (var roomGroup in groupedBedSpaces)
            {
                var roomBedSpaces = availableBedSpaces
                    .Where(b => b.BuildingName == roomGroup.Room.BuildingName
                             && b.FloorName == roomGroup.Room.FloorName
                             && b.RoomName == roomGroup.Room.RoomName)
                    .ToList();

                selectedBedSpaces.AddRange(roomBedSpaces);
            }
            return selectedBedSpaces;
        }

        public SelectListVM GetBedSpaceByID(int id)
        {
            SelectListVM model = new SelectListVM();

            var bs = uow.Context.BedSpaces.Include("Room").Where(x => x.BedSpaceID == id).FirstOrDefault();
            if (bs != null)
            {
                model.Text = "Bed : " + bs.BedName + Environment.NewLine + "Room : " + bs.Room.RoomName;
                model.Value = bs.BedSpaceID.ToString();
            }

            return model;
        }

        public List<SelectListVM> GetAllBedSpaces()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();


            var db = uow.Context;

            var data = (from bspm in db.BedSpacePlacementMigrations
                        join bsp in db.BedSpacePlacements on bspm.BedSpaceplacementId equals bsp.BedSpacePlacementID
                        join bed in db.BedSpaces on bsp.BedSpaceID equals bed.BedSpaceID
                        join room in db.Rooms on bed.RoomID equals room.RoomID
                        where bsp.IsEnable == true && bed.IsEnable == true
                        select new
                        {
                            bed.BedName,
                            bed.BedSpaceID,
                            room.RoomName,
                            bed.Room.Floor.Building.BuildingName,
                            bed.Room.Floor.Building.Project.LocationID,
                            bed.Room.Floor.FloorName,
                            RoomType = room.RoomType.RoomName,
                            room.RoomID
                        })
                        .OrderByDescending(x => x.RoomID)
                        .GroupBy(x => x.BedSpaceID)
                        .Select(y => y.FirstOrDefault())
                        .Select(z => new SelectListVM
                        {
                            Text = z.BuildingName + "-" + z.FloorName + "-" + z.RoomName + "-" + z.BedName + "-" + z.RoomType.Substring(0, 3).ToUpper(),
                            Value = z.BedSpaceID.ToString(),
                            OrderBy = z.RoomID,
                            LocationId = z.LocationID
                        });

            var dat = data.ToList();
            dat = dat.Where(x => assignedLocationIds.Contains((int)x.LocationId)).OrderBy(x => x.OrderBy).ToList();
            return dat;
        }

        public string GetMaxPersonReferalCode(int personid)
        {
            string code = "0";

            if (uow.GenericRepository<EF.Person>().Table.Where(x => x.ReferralCode != null && x.PersonID == personid).Count() != 0)
            {
                var nowithGRn = Convert.ToString(uow.GenericRepository<EF.Person>().Table.Where(x => x.ReferralCode != null && x.PersonID == personid).AsEnumerable()
                    .Select(x => new { Number = Convert.ToString(x.ReferralCode.Split('-').Last()) }).Max(x => x.Number)) + 1;
                code = (string)nowithGRn;
            }

            else
            {
                code = "1";
            }

            var data = personService.GetPersonById(personid);
            var maxcode = code;
            string value = String.Format("{0:D4}", maxcode);
            var Code = data.FullName.Substring(0, 4).Trim().ToUpper() + Guid.NewGuid().ToString().Split('-')[0].Substring(0, 4).Trim().ToUpper();
            return Code;
        }

        public string GetCode(int LocationId)
        {
            var prevcode = uow.GenericRepository<StudentCreditNote>().Table.Where(x => x.LocationId == LocationId).OrderByDescending(x => x.Code).Select(x => x.Code).FirstOrDefault();
            var randomNo = new Random().Next(0, 9999);
            var code = "CRD-0001-" + randomNo;

            if (prevcode != null)
            {
                var maxCode = String.Format("{0:D4}", (Convert.ToInt32(prevcode.Split('-')[1]) + 1));
                code = "CRD-" + maxCode + "-" + randomNo;
            }

            return code;
        }

        public List<PlacementHistoryVM> GetMigrationHistoryByPlacementId(int PlacementId)
        {
            var data = (from migrationhistory in uow.Context.BedSpacePlacementMigrations

                        join bs in uow.Context.BedSpaces
                        on migrationhistory.OldBedSpaceId equals bs.BedSpaceID
                        into obsp
                        from bedSpace in obsp.DefaultIfEmpty()

                        join bs in uow.Context.BedSpaces
                        on migrationhistory.NewBedSpaceId equals bs.BedSpaceID
                        into nbsp
                        from BedSpace in nbsp.DefaultIfEmpty()

                        join r in uow.Context.Rooms
                        on bedSpace.RoomID equals r.RoomID
                        into rm
                        from room in rm.DefaultIfEmpty()

                        join u in uow.Context.UserMasters
                        on migrationhistory.CreatedBy equals u.ID
                        into ur
                        from UserMaster in ur.DefaultIfEmpty()

                        where migrationhistory.BedSpaceplacementId == PlacementId

                        select new PlacementHistoryVM
                        {
                            OldBedSpace = obsp.FirstOrDefault().Room.Floor.FloorName + "-" + obsp.FirstOrDefault().Room.RoomName + "-" + obsp.FirstOrDefault().BedName + "-" + obsp.FirstOrDefault().Room.RoomType.RoomName.Substring(0, 3).ToUpper(),
                            NewBedSpace = nbsp.FirstOrDefault().Room.Floor.FloorName + "-" + nbsp.FirstOrDefault().Room.RoomName + "-" + nbsp.FirstOrDefault().BedName + "-" + nbsp.FirstOrDefault().Room.RoomType.RoomName.Substring(0, 3).ToUpper(),
                            Remarks = migrationhistory.Remarks,
                            CreatedDate = migrationhistory.CreatedDate,
                            CreateBy = ur.FirstOrDefault().FullName
                        });

            var dat = data.ToList();
            return dat;

        }

        public List<GuestCountListVM> GetGuestCounts()
        {
            var db = uow.Context;
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();


            var data = (from guest in db.PersonGuests
                        join bsp in db.BedSpacePlacements on guest.BedSpacePlacementID equals bsp.BedSpacePlacementID
                        join bedspace in db.BedSpaces on bsp.BedSpaceID equals bedspace.BedSpaceID
                        join room in db.Rooms on bedspace.RoomID equals room.RoomID
                        join roomType in db.RoomTypes on room.RoomTypeID equals roomType.RoomTypeID
                        join booking in db.Bookings on bsp.BookingID equals booking.BookingID
                        join person in db.People on booking.PersonID equals person.PersonID
                        join location in db.Locations on person.LocationId equals location.LocationID
                        where assignedLocationIds.Contains((int)booking.LocationID) && guest.CheckOutGuest == null && bsp.CheckIn != null && bsp.CheckOut == null
                        //&& (EntityFunctions.TruncateTime(guest.CurrentDateTime) >= EntityFunctions.TruncateTime(FromDate) || FromDate == null)
                        //&& (EntityFunctions.TruncateTime(guest.CurrentDateTime) <= EntityFunctions.TruncateTime(ToDate) || ToDate == null)
                        select new GuestCountListVM
                        {
                            PersonGuestID = guest.ID,
                            LocationName = location.LocationName,
                            Title = person.Title,
                            Code = person.Code,
                            FullName = person.FullName,
                            BedSpacePlacementID = bsp.BedSpacePlacementID,
                            BedRoom = bedspace.BedName + " - " + room.RoomName + " - " + roomType.RoomName,
                            GuestCount = guest.GuestCount ?? 0,
                            Description = guest.Description,
                            IDNumber = guest.IDNumber,
                            GuestName = guest.GuestName,
                            VisitorCardNumber = guest.VisitorCardNumber,
                            CreatedBy = guest.CreatedBy
                        });

            return data.ToList();
        }

        #endregion

        #region POST
        public BedSpacePlacement AddBedSpacePlacement(AddBedSpacePlacementVM model)
        {
            // Check for overlapping placements with the same move-in/move-out dates
            var overlappingPlacement = CheckForOverlappingPlacements(model.BedSpaceID, model.MoveIn, model.MoveOut);

            if (overlappingPlacement != null)
            {
                throw new Exception($"Cannot create placement: The selected bedspace is already occupied from {overlappingPlacement.MoveIn:dd/MM/yyyy} to {overlappingPlacement.MoveOut:dd/MM/yyyy}. Please change the move-out date of the existing placement before creating a new one.");
            }

            // Also check if bedspace is currently occupied (for immediate occupancy)
            bool ocupy = CheckIfBedSpaceOcupied(model.BedSpaceID);

            if (ocupy)
            {
                throw new Exception("Bed space already occupied.");
            }

            var bookings = uow.GenericRepository<EF.Booking>().GetById(model.BookingID);
            if (bookings == null)
            {
                throw new InvalidOperationException("Booking not found.");
            }

            int personID = bookings.PersonID;

            //bool depositPaid = uow.GenericRepository<EF.Invoicing>()
            //    .GetAll()
            //    .Any(i => i.StudentId == personID && i.InvoiceTypeId == 2 && i.IsPaid == true);

            //if (!depositPaid)
            //{
            //    throw new InvalidOperationException("Please submit the deposit before creating a placement.");
            //}

            var personBookings = uow.GenericRepository<EF.Booking>()
                            .GetAll()
                            .Where(b => b.PersonID == personID)
                            .Select(b => b.BookingID)
                            .ToList();

            var existingPlacementOld = uow.GenericRepository<BedSpacePlacement>()
                .GetAll()
                .Any(p => personBookings.Contains(p.BookingID) && p.IsEnable && p.CheckOut == null);

            if (existingPlacementOld)
            {
                throw new InvalidOperationException("An active placement already exists for this person.");
            }

            BedSpacePlacement placement = new BedSpacePlacement
            {
                BookingID = model.BookingID,
                BedSpaceID = model.BedSpaceID,
                MoveIn = model.MoveIn,
                MoveOut = model.MoveOut,
                IsEnable = true,
                AccessibilityRequest = model.Requests,
                CreatedDate = model.CreatedDate,
                CreatedBy = model.CreatedBy
            };

            uow.GenericRepository<BedSpacePlacement>().Insert(placement);
            uow.SaveChanges();


            // update Dates from bookings
            EF.Booking booking = uow.GenericRepository<EF.Booking>().GetById(model.BookingID);
            if (booking != null)
            {
                booking.CheckInDate = placement.MoveIn;
                booking.CheckOutDate = placement.MoveOut;
            }

            uow.GenericRepository<EF.Booking>().Update(booking);
            uow.SaveChanges();

#if !DEBUG

            var NotifyEmail = correspondenceService.GetEmailMessagesByActionId(((int)Enumeration.CorrespondenceAction.CreatePlacement), booking.LocationID ?? 0);

            if (NotifyEmail != null)
            {
                var Booking = bookingService.GetBookingByID(placement.BookingID);
                var Placement1 = GetBedSpacePlacementById(placement.BedSpacePlacementID);
                var body = NotifyEmail.EmailMessageBody;

                emailService.SendEmailAsync(Convert.ToString(NotifyEmail.EmailMessageSubject), body, true, Booking.Person.Email, NotifyEmail.EmailMessageSenderID);
            }

            //Insert Audit Log
            var Oldplacement = new BedSpacePlacement();
            {
                var difference = Common.Classes.Common.DetailedCompare<BedSpacePlacement>(Oldplacement, placement);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.CreatePlacement,
                    PK = placement.BedSpacePlacementID.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "BedSpacePlacement",
                    Reference = booking.BookingNumber,
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = booking.PersonID,
                    AuditLogDetails = difference
                };

                auditLogsService.AddAuditLog(auditLog);
            }

            var user = personService.CheckUserMaster(booking.PersonID);

            if (user != null && !user.IsActive)
            {
                personService.ResendEmail(user.UserID);
            }
            // Send Notification

            var Description = "Your Placement has been created against Booking Number: " + booking.BookingNumber;
            notificationService.SendNotification(null, booking.PersonID, "Student", "New Placement", Description, "/Student/Notification", PMS.Common.Globals.User.Email);
#endif

            //END notification
            return placement;
        }

        public BedSpacePlacement UpdateBedSpacePlacement(AddBedSpacePlacementVM model)
        {
            var Oldplacement = uow.GenericRepository<BedSpacePlacement>().GetByIdAsNoTracking(x => x.BedSpacePlacementID == model.BedSpacePlacementID);
            var placement = GetBedSpacePlacementById(model.BedSpacePlacementID);

            // Check for overlapping placements with the same move-in/move-out dates (excluding current placement)
            var overlappingPlacement = CheckForOverlappingPlacements(model.BedSpaceID, model.MoveIn, model.MoveOut, model.BedSpacePlacementID);

            if (overlappingPlacement != null)
            {
                throw new Exception($"Cannot update placement: The selected bedspace is already occupied from {overlappingPlacement.MoveIn:dd/MM/yyyy} to {overlappingPlacement.MoveOut:dd/MM/yyyy}. Please change the move-out date of the existing placement before updating this one.");
            }

            bool ocupy = CheckIfBedSpaceOcupied(model.BedSpaceID, model.BedSpacePlacementID);

            if (ocupy)
            {
                throw new Exception("Bed space already occupied.");
            }

            if (placement != null)
            {
                placement.BookingID = model.BookingID;
                placement.BedSpaceID = model.BedSpaceID;
                placement.MoveIn = model.MoveIn;
                placement.MoveOut = model.MoveOut;
                placement.UpdatedDate = model.UpdatedDate;
                placement.UpdatedBy = model.UpdatedBy;
                placement.AccessibilityRequest = model.Requests;

                EF.Booking booking = uow.GenericRepository<EF.Booking>().GetById(model.BookingID);
                if (booking != null)
                {
                    booking.CheckInDate = placement.MoveIn;
                    booking.CheckOutDate = placement.MoveOut;
                }
                uow.GenericRepository<EF.Booking>().Update(booking);
                uow.GenericRepository<BedSpacePlacement>().Update(placement);
                uow.SaveChanges();

                //send Confirmation Email
#if !DEBUG

                var NotifyEmail = correspondenceService.GetEmailMessagesByActionId(((int)Enumeration.CorrespondenceAction.UpdatePlacement), booking.LocationID ?? 0);
                if (NotifyEmail != null)
                {
                    var Booking = bookingService.GetBookingByID(placement.BookingID);

                    var body = NotifyEmail.EmailMessageBody;
                    body = body.Replace("[[PersonID]]", Booking.Person.Code);
                    body = body.Replace("[[PersonFull_Name]]", Booking.Person.FullName);
                    body = body.Replace("[[BookingNumber]]", Booking.BookingNumber);
                    body = body.Replace("[[BookingRoomManagementRoomType]]", Booking.PriceConfig.RoomType.RoomName.ToString());
                    body = body.Replace("[[BookingRoomManagementBuilding]]", placement.BedSpace.Room.Floor.Building.BuildingName);
                    body = body.Replace("[[BookingRoomManagementFloor]]", placement.BedSpace.Room.Floor.FloorName);
                    body = body.Replace("[[BookingRoomManagementRoomNo]]", placement.BedSpace.Room.RoomName);
                    body = body.Replace("[[BookingRoomManagementBedSpace]]", placement.BedSpace.BedName);
                    body = body.Replace("[[BookingRoomManagementMoveInDate]]", placement.MoveIn.ToString("dd/M/yyyy"));
                    body = body.Replace("[[BookingRoomManagementNoOFMonths]]", (placement.MoveOut - placement.MoveIn).TotalDays.ToString());
                    body = body.Replace("[[BookingRoomManagementMoveOutDate]]", placement.MoveOut.ToString("dd/M/yyyy"));
                    emailService.SendEmailAsync(Convert.ToString(NotifyEmail.EmailMessageSubject), body, true, Booking.Person.Email, NotifyEmail.EmailMessageSenderID);
                }

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<BedSpacePlacement>(Oldplacement, placement);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdatePlacement,
                        PK = placement.BedSpacePlacementID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "BedSpacePlacement",
                        Reference = booking.BookingNumber,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = booking.PersonID,
                        AuditLogDetails = difference
                    };

                    auditLogsService.AddAuditLog(auditLog);
                }
#endif
                return placement;
            }

            else
            {
                throw new Exception("Placement not found to update.");
            }
        }

        public BedSpacePlacement UpdateBedSpacePlacementDate(AddBedSpacePlacementVM model)
        {
            var Oldplacement = uow.GenericRepository<BedSpacePlacement>().GetByIdAsNoTracking(x => x.BedSpacePlacementID == model.BedSpacePlacementID);
            var placement = GetBedSpacePlacementById(model.BedSpacePlacementID);

            if (placement != null)
            {
                // Check for overlapping placements with the same move-in/move-out dates (excluding current placement)
                var overlappingPlacement = CheckForOverlappingPlacements(placement.BedSpaceID, model.MoveIn, model.MoveOut, model.BedSpacePlacementID);

                if (overlappingPlacement != null)
                {
                    throw new Exception($"Cannot update placement dates: The selected bedspace is already occupied from {overlappingPlacement.MoveIn:dd/MM/yyyy} to {overlappingPlacement.MoveOut:dd/MM/yyyy}. Please change the move-out date of the existing placement before updating this one.");
                }
                placement.BookingID = model.BookingID;
                placement.MoveIn = model.MoveIn;
                placement.MoveOut = model.MoveOut;
                placement.CheckIn = model.CheckIn == null || model.CheckIn == DateTime.MinValue ? (DateTime?)null : model.CheckIn;
                placement.CheckOut = model.CheckOut == null || model.CheckOut == DateTime.MinValue ? (DateTime?)null : model.CheckOut;
                placement.UpdatedDate = model.UpdatedDate;
                placement.UpdatedBy = model.UpdatedBy;

                EF.Booking booking = uow.GenericRepository<EF.Booking>().GetById(model.BookingID);
                if (booking != null)
                {
                    booking.CheckInDate = placement.MoveIn;
                    booking.CheckOutDate = placement.MoveOut;
                }
                uow.GenericRepository<EF.Booking>().Update(booking);
                uow.GenericRepository<BedSpacePlacement>().Update(placement);
                uow.SaveChanges();

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<BedSpacePlacement>(Oldplacement, placement);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdatePlacement,
                        PK = placement.BedSpacePlacementID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "BedSpacePlacement",
                        Reference = booking.BookingNumber,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = booking.PersonID,
                        AuditLogDetails = difference
                    };

                    auditLogsService.AddAuditLog(auditLog);
                }

                return placement;
            }

            else
            {
                throw new Exception("Placement not found to update.");
            }
        }

        public async Task<bool> CheckInPlacement(int id, DateTime checkIntime, string cardNumber, string encoderNumber)
        {
            try
            {
                var assignedLocationIds = locationContextService.GetAssignedLocationIds();


                var Oldplacement = uow.GenericRepository<BedSpacePlacement>().GetByIdAsNoTracking(x => x.BedSpacePlacementID == id);
                var placement = GetBedSpacePlacementById(id);
                var booking = uow.GenericRepository<EF.Booking>().Table.Where(x => x.BookingID == placement.BookingID)
                    .Select(x => new { x.BookingNumber, x.PersonID, x.Person.FullName, x.Person.Email, x.CheckOutDate, x.LocationID }).FirstOrDefault();

                if (placement == null)
                {
                    throw new Exception("Placement not found to update check-in.");
                }

                var clientIntegrations = uow.GenericRepository<ClientIntegration>().Table.ToList();
#if !DEBUG // Skip Messerschmitt API and TTLock block in debug mode (localhost)

                // Messerschmitt Integration - Dubai
                if (booking.LocationID == (int)LocationEnum.Dubai)
                {
                    var clientIntegrationMesserschmitt = clientIntegrations.FirstOrDefault(x => x.Client_Name == "Messerschmitt");

                    if (clientIntegrationMesserschmitt == null)
                        throw new Exception("Messerschmitt integration information not found.");

                    var baseUrl = CacheHelper.GetOrAdd("Messerschmitt_BaseUrl", () => "http://tmds1.fortiddns.com:7331/mst/enc", 30);
                    var accessToken = CacheHelper.GetOrAdd("Messerschmitt_AccessToken", () =>
                    {
                        return clientIntegrationMesserschmitt.Access_Token;
                    }, 30);

                    if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(accessToken))
                        throw new Exception("Missing base URL or access token for Messerschmitt.");

                    string placementMoveOut = placement.MoveOut.ToString("MM/dd/yyyy");

                    // Determine the identifier to pass based on room type
                    var roomType = placement.BedSpace.Room.RoomType.RoomName;
                    string identifierForCheckout = roomType == "Double Room"
                       ? placement.BedSpace.BedName
                        : placement.BedSpace.Room.RoomName;

                    //var checkInResponse = await iTTLockAuth.CheckInGuest(placement.BedSpace.BedName, checkIntime.ToString("MM/dd/yyyy"), placementMoveOut, encoderNumber);
                    var checkInResponse = await iTTLockAuth.CheckInGuest(identifierForCheckout, checkIntime.ToString("MM/dd/yyyy"), placementMoveOut, encoderNumber);

                    if (checkInResponse.Result == 0)
                    {
                        string TID = checkInResponse.Data.Tid;

                        // If TID is not null, call the second API
                        if (!string.IsNullOrEmpty(TID))
                        {

                            //var ciemsResponse = await iTTLockAuth.EMSCheckin(placement.BedSpace.BedName, accessToken);


                            //if (ciemsResponse?.Result != 0)
                            //{
                            //    throw new Exception($"CIEMS Check-in failed. Room: {placement.BedSpace.Room.RoomName}, Message: {ciemsResponse?.Msg}");
                            //}
                        }
                        placement.TID = TID;
                    }
                    else
                    {
                        throw new Exception($"Guest Check-in failed. Room: {placement.BedSpace.Room.RoomName}, Message: {checkInResponse?.Msg}");
                    }

                }

                // Sciener app Integration - Muscat
                //else
                //{
                //    var lockId = placement.BedSpace.Room.RoomLockId;

                //    var clientIntegrationScienerApp = clientIntegrations.FirstOrDefault(x => x.Client_Name == "ScienerApp");

                //    if (clientIntegrationScienerApp != null)
                //    {
                //        if (DateTime.Parse(clientIntegrationScienerApp.Access_Token_Expiry) <= DateTime.Now)
                //        {
                //            await iTTLockAuth.RefreshScienerAccessTokenAsync(clientIntegrationScienerApp);
                //        }

                //        var clientId = clientIntegrationScienerApp.Client_ID;
                //        var accessToken = clientIntegrationScienerApp.Access_Token;

                //        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(accessToken) || lockId == null)
                //        {
                //            throw new Exception("Missing client ID, access token, or lock ID.");
                //        }

                //        long date = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                //        long startDate = await iTTLockAuth.GetStartDate(clientId, accessToken, lockId.Value, date);

                //        long endDate = startDate + (long)TimeSpan.FromDays(2).TotalMilliseconds;
                //        int addType = 2;
                //        await Task.Delay(2000);

                //        var cardId = await iTTLockAuth.AddReversedCardNumber(clientId, accessToken, lockId.Value, cardNumber, startDate, endDate, addType, date);

                //        if (cardId <= 0)
                //        {
                //            throw new Exception("Invalid CardId received from AddReversedCardNumber.");
                //        }
                //        placement.CardID = cardId;

                //    }
                //    else
                //    {
                //        throw new Exception("TT Lock integration information not found.");
                //    }
                //}
#endif
                placement.CheckIn = checkIntime;

                uow.GenericRepository<BedSpacePlacement>().Update(placement);
                uow.SaveChanges();

#if !DEBUG
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<BedSpacePlacement>(Oldplacement, placement);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.CheckinPlacement,
                        PK = placement.BedSpacePlacementID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "BedSpacePlacement",
                        Reference = booking.BookingNumber,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = booking.PersonID,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                // Send Notification
                var Description = "You have been checked in against Booking Number: " + booking.BookingNumber;
                notificationService.SendNotification(null, booking.PersonID, "Student", "New CheckIn", Description, "/Student/Notification", PMS.Common.Globals.User.Email);
                //END notification

                var NotifyEmail = correspondenceService.GetEmailMessagesByActionId((int)Enumeration.CorrespondenceAction.Feedback, booking.LocationID ?? 0);
                if (NotifyEmail != null)
                {
                    var encryptUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(booking.PersonID + "," + placement.BedSpacePlacementID));
                    var Request = HttpContext.Current.Request;

                    var body = NotifyEmail.EmailMessageBody;
                    body = body.Replace("[[Description]]", booking.FullName);
                    body = body.Replace("{{ConfirmationLink}}", Request.Url.GetLeftPart(UriPartial.Authority) + Request.ApplicationPath + "Feedback/FeedbackSetting/AddFeedback?encIds=" + encryptUrl);
                    emailService.SendEmail(Convert.ToString(NotifyEmail.EmailMessageSubject), body, true, booking.Email, NotifyEmail.EmailMessageSenderID);
                }
#endif

                //var referralCode = GetMaxPersonReferalCode(booking.PersonID);
                //var refCheck = uow.GenericRepository<EF.Person>().Table.Where(x => x.PersonID == booking.PersonID).Select(x => new { x.ReferralCode, x.LocationId }).FirstOrDefault();
                //if (refCheck == null)
                //{
                //    var res = uow.GenericRepository<EF.Person>().Table.Where(x => x.PersonID == booking.PersonID).FirstOrDefault();
                //    res.ReferralCode = referralCode;
                //    uow.GenericRepository<EF.Person>().Update(res);
                //    uow.SaveChanges();
                //    var NotifyEmail1 = correspondenceService.GetEmailMessagesByActionId((int)Enumeration.CorrespondenceAction.ReferralEmail, refCheck.LocationId ?? 0);

                //    if (NotifyEmail1 != null)
                //    {
                //        var body = NotifyEmail1.EmailMessageBody;
                //        body = body.Replace("[[Description]]", booking.FullName);
                //        body = body.Replace("[[ReferralCode]]", referralCode);
                //        emailService.SendEmail(Convert.ToString(NotifyEmail1.EmailMessageSubject), body, true, booking.Email, NotifyEmail1.EmailMessageSenderID);
                //    }
                //}

                //var referralcode1 = uow.GenericRepository<EF.Booking>().Table.Where(x => x.BookingID == placement.BookingID).Select(x => new { x.HearFromCode, x.LocationID, x.BookingNumber }).FirstOrDefault();

                //if (referralcode1.HearFromCode != null)
                //{
                //    var refferalmatch = uow.GenericRepository<EF.Person>().Table.Where(x => x.ReferralCode == referralcode1.HearFromCode && x.IsEnable == true).Select(x => new { x.ReferralCode, x.PersonID, x.FullName }).FirstOrDefault();

                //    if (refferalmatch.ReferralCode != null)
                //    {
                //        var setting = uow.GenericRepository<EF.LocationSetting>().Table.Where(x => x.LocationId == referralcode1.LocationID).FirstOrDefault();

                //        if (setting.ReferralIsActive == true)
                //        {
                //            var code = GetCode((int)referralcode1.LocationID);

                //            var creditnote = new EF.StudentCreditNote()
                //            {
                //                LocationId = (int)referralcode1.LocationID,
                //                Type = 3,
                //                Code = code,
                //                StudentId = refferalmatch.PersonID,
                //                Amount = decimal.Parse(setting.ReferralProgram),
                //                Percentage = 0,
                //                Reason = "CreditNote has been generated against this person " + booking.FullName + " having Booking number " + referralcode1.BookingNumber + " as a Referral Gift for " + refferalmatch.FullName,
                //                CreatedDate = DateTime.Now,
                //                CreatedBy = Common.Globals.User.ID,
                //                ApprovedBy = Common.Globals.User.ID,
                //                UpdatedDate = DateTime.Now,
                //                Status = 3,
                //                IsUtilized = false,
                //                IsEnable = true,
                //                BookingId = placement.BookingID
                //            };

                //            uow.GenericRepository<StudentCreditNote>().Insert(creditnote);
                //            uow.SaveChanges();
                //        }
                //    }
                //}
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> CheckOutPlacementAsync(int id, DateTime checkOuttime)
        {
            var Oldplacement = uow.GenericRepository<BedSpacePlacement>().GetByIdAsNoTracking(x => x.BedSpacePlacementID == id);
            var placement = GetBedSpacePlacementById(id);
            var room = placement.BedSpace.BedName;
            var booking = uow.GenericRepository<EF.Booking>().Table.Where(x => x.BookingID == placement.BookingID).Select(x => new { x.BookingNumber, x.PersonID, x.LocationID }).FirstOrDefault();

            if (placement != null)
            {
                if (checkOuttime < placement.CheckIn)
                {
                    return false;
                }

//#if !DEBUG // Skip Messerschmitt API and TTLock block in debug mode (localhost)
                //ValidatePendingInvoicePeriods(booking.PersonID, placement, checkOuttime);

                var clientIntegrations = uow.GenericRepository<ClientIntegration>().Table.ToList();


                if (booking.LocationID == (int)LocationEnum.Dubai) // Use Messerschmitt API for Dubai 
                {
                    var clientIntegrationMesserschmitt = clientIntegrations.FirstOrDefault(x => x.Client_Name == "Messerschmitt");
                    if (clientIntegrationMesserschmitt == null)
                    {
                        throw new Exception("Messerschmitt integration information not found.");
                    }

                    // Determine the identifier to pass based on room type
                    var roomType = placement.BedSpace.Room.RoomType.RoomName;
                    string identifierForCheckout = roomType == "Double Room"
                        ? placement.BedSpace.BedName
                        : placement.BedSpace.Room.RoomName;

                    var messerschmittResponse = await iTTLockAuth.CheckOutMesserschmitt(
                        identifierForCheckout,
                        placement.TID,
                        clientIntegrationMesserschmitt.Access_Token
                    );

                    if (messerschmittResponse?.Result != 0)
                    {
                        throw new Exception($"Messerschmitt Checkout Failed: {messerschmittResponse.Msg}");
                    }


                    // Handle double room scenario: Check for other active placements
                    if (roomType == "Double Room")
                    {
                        // Efficiently check if another bedspace in the same room is occupied
                        var hasOtherActivePlacement = uow.GenericRepository<BedSpacePlacement>().Table
                            .Any(bp => bp.BedSpace.RoomID == placement.BedSpace.RoomID
                                       && bp.BedSpacePlacementID != placement.BedSpacePlacementID
                                       && bp.CheckOut == null
                                       && bp.CheckIn != null);

                        if (hasOtherActivePlacement)
                        {
                            // Perform EMS check-in for the current bed and handle the response
                            var ciemsResponse = await iTTLockAuth.EMSCheckin(
                                identifierForCheckout,
                                clientIntegrationMesserschmitt.Access_Token
                            );

                        }
                    }

                }
                // TTLock
                //else
                //{
                //    var lockId = placement.BedSpace.Room.RoomLockId;
                //    var cardId = placement.CardID;
                //    long date = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                //    var clientIntegration = uow.GenericRepository<ClientIntegration>().Table
                //        .FirstOrDefault(x => x.Client_Name == "ScienerApp");

                //    var clientId = clientIntegration?.Client_ID;
                //    var accessToken = clientIntegration?.Access_Token;

                //    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(accessToken) || lockId == null)
                //    {
                //        throw new Exception("Missing client ID, access token, or lock ID.");
                //    }

                //    int deleteType = 2;
                //    var deleteCardAsync = await DeleteCardAsync(clientId, accessToken, lockId ?? 0, cardId ?? 0, deleteType, date);
                //}

//#endif

                placement.CheckOut = checkOuttime;

                uow.GenericRepository<BedSpacePlacement>().Update(placement);
                uow.SaveChanges();
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<BedSpacePlacement>(Oldplacement, placement);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.CheckOutPlacement,
                        PK = placement.BedSpacePlacementID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "BedSpacePlacement",
                        Reference = booking.BookingNumber,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = booking.PersonID,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }

                UserManageService.UnActivePerson(booking.PersonID);
                var person = uow.GenericRepository<EF.Person>().Table.Where(x => x.PersonID == booking.PersonID).FirstOrDefault();
                person.ReferralCode = null;
                uow.GenericRepository<EF.Person>().Update(person);
                uow.SaveChanges();


                return true;
            }
            else
            {
                throw new Exception("Placement not found to update check out.");
            }
        }

        #endregion

        public BedSpacePlacement ImportBedSpacePlacement(AddBedSpacePlacementVM model)
        {
            // Check for overlapping placements with the same move-in/move-out dates
            var overlappingPlacement = CheckForOverlappingPlacements(model.BedSpaceID, model.MoveIn, model.MoveOut);

            if (overlappingPlacement != null)
            {
                throw new Exception($"Cannot import placement: The selected bedspace is already occupied from {overlappingPlacement.MoveIn:dd/MM/yyyy} to {overlappingPlacement.MoveOut:dd/MM/yyyy}. Please change the move-out date of the existing placement before importing this one.");
            }

            bool ocupy = CheckIfBedSpaceOcupied(model.BedSpaceID);

            if (ocupy)
            {
                throw new Exception("Bed space already occupied.");
            }

            var bookings = uow.GenericRepository<EF.Booking>().GetById(model.BookingID);
            if (bookings == null)
            {
                throw new InvalidOperationException("Booking not found.");
            }

            int personID = bookings.PersonID;


            var personBookings = uow.GenericRepository<EF.Booking>()
                            .GetAll()
                            .Where(b => b.PersonID == personID)
                            .Select(b => b.BookingID)
                            .ToList();

            var existingPlacementOld = uow.GenericRepository<BedSpacePlacement>()
                .GetAll()
                .Any(p => personBookings.Contains(p.BookingID) && p.IsEnable && p.CheckOut == null);

            if (existingPlacementOld)
            {
                throw new InvalidOperationException("An active placement already exists for this person.");
            }

            BedSpacePlacement placement = new BedSpacePlacement
            {
                BookingID = model.BookingID,
                BedSpaceID = model.BedSpaceID,
                MoveIn = model.MoveIn,
                MoveOut = model.MoveOut,
                CheckIn = model.CheckIn,
                //CheckOut = model.CheckOut,
                IsEnable = true,
                AccessibilityRequest = model.Requests,
                CreatedDate = model.CreatedDate,
                CreatedBy = model.CreatedBy
            };

            uow.GenericRepository<BedSpacePlacement>().Insert(placement);
            uow.SaveChanges();


            // update Dates from bookings
            EF.Booking booking = uow.GenericRepository<EF.Booking>().GetById(model.BookingID);
            if (booking != null)
            {
                booking.CheckInDate = placement.MoveIn;
                booking.CheckOutDate = placement.MoveOut;
            }

            uow.GenericRepository<EF.Booking>().Update(booking);
            uow.SaveChanges();

            return placement;
        }

        private void ValidatePendingInvoicePeriods(int studentId, BedSpacePlacement placement, DateTime checkOutDate)
        {
            var lastInvoice = uow.GenericRepository<Invoicing>().Table
                .Where(i => i.StudentId == studentId && i.Refunded == null)
                .OrderByDescending(i => i.InvoiceDate)
                .ThenByDescending(i => i.InvoicingDetails.Max(d => d.ToDate))
                .FirstOrDefault();

            if (lastInvoice != null)
            {
                if (DateTime.Now >= new DateTime(2024, 03, 26))
                {
                    var lastInvoiceDetail = lastInvoice.InvoicingDetails.OrderByDescending(d => d.ToDate).FirstOrDefault();
                    if (lastInvoiceDetail?.ToDate.HasValue == true)
                    {
                        var missingPeriodStartDate = lastInvoiceDetail.ToDate.Value.AddDays(1);
                        var missingPeriodEndDate = checkOutDate.AddDays(-1);

                        if (missingPeriodStartDate >= placement.MoveIn &&
                            missingPeriodEndDate <= checkOutDate)
                        {
                            if (missingPeriodStartDate <= missingPeriodEndDate)
                            {
                                throw new Exception($"Cannot checkout: Billing for the period between {missingPeriodStartDate:yyyy-MM-dd} and {missingPeriodEndDate:yyyy-MM-dd} is missing. Please create invoice for this period before checking out.");
                            }
                        }
                    }
                }
            }
            else
            {
                if (placement.MoveIn < checkOutDate)
                {
                    var totalStayDays = (checkOutDate - placement.MoveIn).Days;
                    if (totalStayDays > 0) // Only throw error if there's a significant stay period
                    {
                        throw new Exception($"Cannot checkout: No invoices found for this resident. Please create invoice for the period between {placement.MoveIn:yyyy-MM-dd} and {checkOutDate.AddDays(-1):yyyy-MM-dd} before checking out.");
                    }
                }
            }
        }
        public async Task<bool> ReissueCard(int id, string encoderNumber)
        {
            try
            {
                var assignedLocationIds = locationContextService.GetAssignedLocationIds();
                var placement = GetBedSpacePlacementById(id);
                if (placement == null)
                {
                    throw new Exception("Placement not found to reissue card");
                }
                var booking = uow.GenericRepository<EF.Booking>().Table.Where(x => x.BookingID == placement.BookingID)
                   .Select(x => new { x.BookingNumber, x.PersonID, x.Person.FullName, x.Person.Email, x.CheckOutDate, x.LocationID }).FirstOrDefault();
                var clientIntegrations = uow.GenericRepository<ClientIntegration>().Table.ToList();
                if (booking.LocationID == (int)LocationEnum.Dubai)
                {
                    var clientIntegrationMesserschmitt = clientIntegrations.FirstOrDefault(x => x.Client_Name == "Messerschmitt");
                    if (clientIntegrationMesserschmitt == null)
                    {
                        throw new Exception("Messerschmitt integration information not found.");
                    }

                    string placementMoveOut = placement.MoveOut.ToString("MM/dd/yyyy");

                    // Determine the identifier to pass based on room type
                    var roomType = placement.BedSpace.Room.RoomType.RoomName;
                    string identifierForCheckout = roomType == "Double Room"
                        ? placement.BedSpace.BedName
                        : placement.BedSpace.Room.RoomName;

                    // First deactivate the previous card (if any) so that only the new card remains valid
                    if (!string.IsNullOrEmpty(placement.TID))
                    {
                        var messerschmittCheckoutResponse = await iTTLockAuth.CheckOutMesserschmitt(
                            identifierForCheckout,
                            placement.TID,
                            clientIntegrationMesserschmitt.Access_Token
                        );

                        if (messerschmittCheckoutResponse?.Result != 0)
                        {
                            throw new Exception($"Previous card deactivation failed. Room: {placement.BedSpace.Room.RoomName}, Message: {messerschmittCheckoutResponse?.Msg}");
                        }
                    }

                    var checkInResponse = await iTTLockAuth.CheckInGuest(identifierForCheckout, DateTime.Now.ToString("MM/dd/yyyy"), placementMoveOut, encoderNumber);
                    if (checkInResponse.Result == 0)
                    {
                        string TID = checkInResponse.Data.Tid;

                        if (!string.IsNullOrEmpty(TID))
                        {
                            placement.TID = TID;
                            uow.GenericRepository<BedSpacePlacement>().Update(placement);
                            uow.SaveChanges();
                        }
                    }
                    else
                    {
                        throw new Exception($"Re-Issue of a card failed. Room: {placement.BedSpace.Room.RoomName}, Message: {checkInResponse?.Msg}");
                    }
                  
                    var ciemsResponse = await iTTLockAuth.EMSCheckin(placement.BedSpace.Room.RoomName, clientIntegrationMesserschmitt.Access_Token);


                    if (ciemsResponse?.Result != 0)
                    {
                        throw new Exception($"CIEMS Check-in failed. Room: {placement.BedSpace.Room.RoomName}, Message: {ciemsResponse?.Msg}");
                    }

                }
                var Oldplacement = uow.GenericRepository<BedSpacePlacement>().GetByIdAsNoTracking(x => x.BedSpacePlacementID == id);
                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<BedSpacePlacement>(Oldplacement, placement);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.ReissueCard,
                        PK = placement.BedSpacePlacementID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "BedSpacePlacement",
                        Reference = booking.BookingNumber,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = booking.PersonID,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool CheckIfBedSpaceOcupied(int bedSpaceId, int bedSpacePlacementId = 0)
        {
            var data = uow.GenericRepository<BedSpacePlacement>().Table.Where(x => x.IsEnable == true && x.BedSpaceID == bedSpaceId && x.CheckOut == null);

            if (bedSpacePlacementId > 0)
                data = data.Where(x => x.BedSpacePlacementID != bedSpacePlacementId);

            return data.Any();
        }
        public BedSpacePlacement CheckForOverlappingPlacements(int bedSpaceId, DateTime moveIn, DateTime moveOut, int excludePlacementId = 0)
        {
            var existingPlacements = uow.GenericRepository<BedSpacePlacement>().Table
                .Where(x => x.IsEnable == true && x.BedSpaceID == bedSpaceId && x.CheckOut == null);

            // Exclude the current placement if updating
            if (excludePlacementId > 0)
            {
                existingPlacements = existingPlacements.Where(x => x.BedSpacePlacementID != excludePlacementId);
            }

            var allPlacements = existingPlacements.ToList();

            var overlappingPlacement = allPlacements.FirstOrDefault(placement =>
                (moveIn < placement.MoveOut && moveOut > placement.MoveIn)
            ); 

            return overlappingPlacement;
        }

        public bool DeleteBedSpacePlacement(int id)
        {
            var Oldplacement = uow.GenericRepository<BedSpacePlacement>().GetByIdAsNoTracking(x => x.BedSpacePlacementID == id);
            var placement = GetBedSpacePlacementById(id);
            var booking = uow.GenericRepository<EF.Booking>().Table.Where(x => x.BookingID == placement.BookingID).Select(x => new { x.BookingNumber, x.PersonID }).FirstOrDefault();

            if (placement != null)
            {
                placement.IsEnable = false;

                uow.GenericRepository<BedSpacePlacement>().Update(placement);
                uow.SaveChanges();

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<BedSpacePlacement>(Oldplacement, placement);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeletePlacement,
                        PK = placement.BedSpacePlacementID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "BedSpacePlacement",
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
            {
                throw new Exception("Placement not found to delete.");
            }
        }

        public async Task<bool> SwapBedSpacePlacementAsync(BedSpacePlacementMigrationVM migrationVM)
        {
            try
            {
                bool ocupy = CheckIfBedSpaceOcupied(migrationVM.BedSpaceId);

                if (ocupy)
                {
                    throw new Exception("Bed space already occupied.");
                }

                var placement = GetBedSpacePlacementById(migrationVM.PlacementId);
                if (placement == null)
                {
                    throw new Exception("Placement not found.");
                }

                var oldBedSpace = placement.BedSpace;
                var oldRoomId = oldBedSpace.RoomID;
                var oldRoomType = oldBedSpace.Room.RoomType.RoomName;
                var oldIdentifierForCheckout = oldRoomType == "Double Room"
                    ? oldBedSpace.BedName
                    : oldBedSpace.Room.RoomName;

                var newBedSpace = uow.GenericRepository<BedSpace>().Table
                    .Include(b => b.Room.RoomType)
                    .FirstOrDefault(b => b.BedSpaceID == migrationVM.BedSpaceId);

                if (newBedSpace == null)
                {
                    throw new Exception("Target bed space not found.");
                }

                var booking = uow.GenericRepository<EF.Booking>().Table
                    .Where(x => x.BookingID == placement.BookingID)
                    .Select(x => new { x.BookingNumber, x.PersonID, x.LocationID })
                    .FirstOrDefault();

                ClientIntegration clientIntegrationMesserschmitt = null;

                if (booking.LocationID == (int)LocationEnum.Dubai && placement.CheckIn != null)
                {
                    if (string.IsNullOrWhiteSpace(migrationVM.EncoderNumber))
                    {
                        throw new Exception("Encoder number is required.");
                    }

                    var clientIntegrations = uow.GenericRepository<ClientIntegration>().Table.ToList();
                    clientIntegrationMesserschmitt = clientIntegrations.FirstOrDefault(x => x.Client_Name == "Messerschmitt");
                    if (clientIntegrationMesserschmitt == null)
                    {
                        throw new Exception("Messerschmitt integration information not found.");
                    }

                    if (!string.IsNullOrEmpty(placement.TID))
                    {
                        var messerschmittResponse = await iTTLockAuth.CheckOutMesserschmitt(
                            oldIdentifierForCheckout,
                            placement.TID,
                            clientIntegrationMesserschmitt.Access_Token
                        );

                        if (messerschmittResponse?.Result != 0)
                        {
                            throw new Exception($"Messerschmitt Checkout Failed: {messerschmittResponse.Msg}");
                        }

                        if (oldRoomType == "Double Room")
                        {
                            var hasOtherActivePlacement = uow.GenericRepository<BedSpacePlacement>().Table
                                .Any(bp => bp.BedSpace.RoomID == oldRoomId
                                           && bp.BedSpacePlacementID != placement.BedSpacePlacementID
                                           && bp.CheckOut == null
                                           && bp.CheckIn != null);

                            if (hasOtherActivePlacement)
                            {
                                await iTTLockAuth.EMSCheckin(
                                    oldIdentifierForCheckout,
                                    clientIntegrationMesserschmitt.Access_Token
                                );
                            }
                        }
                    }
                }

                BedSpacePlacementMigration bedSpacePlacementMigration = new BedSpacePlacementMigration()
                {
                    BedSpaceplacementId = migrationVM.PlacementId,
                    OldBedSpaceId = placement.BedSpaceID,
                    NewBedSpaceId = migrationVM.BedSpaceId,
                    Remarks = migrationVM.Remarks,
                    OldBedSpaceName = oldBedSpace.Room.RoomName + "-" + oldBedSpace.BedName + "-" + oldRoomType,
                    CreatedBy = Common.Globals.User.ID,
                    CreatedDate = DateTime.Now
                };

                uow.GenericRepository<BedSpacePlacementMigration>().Insert(bedSpacePlacementMigration);

                placement.BedSpaceID = migrationVM.BedSpaceId;
                placement.UpdatedBy = Common.Globals.User.Email;
                placement.UpdatedDate = DateTime.Now;

                uow.GenericRepository<EF.BedSpacePlacement>().Update(placement);
                uow.SaveChanges();

                if (booking.LocationID == (int)LocationEnum.Dubai && placement.CheckIn != null)
                {
                    if (clientIntegrationMesserschmitt == null)
                    {
                        var clientIntegrations = uow.GenericRepository<ClientIntegration>().Table.ToList();
                        clientIntegrationMesserschmitt = clientIntegrations.FirstOrDefault(x => x.Client_Name == "Messerschmitt");
                        if (clientIntegrationMesserschmitt == null)
                        {
                            throw new Exception("Messerschmitt integration information not found.");
                        }
                    }

                    var newRoomType = newBedSpace.Room.RoomType.RoomName;
                    var newIdentifierForCheckIn = newRoomType == "Double Room"
                        ? newBedSpace.BedName
                        : newBedSpace.Room.RoomName;
                    string placementMoveOut = placement.MoveOut.ToString("MM/dd/yyyy");
                    string checkInDate = placement.CheckIn.Value.ToString("MM/dd/yyyy");

                    var checkInResponse = await iTTLockAuth.CheckInGuest(
                        newIdentifierForCheckIn,
                        checkInDate,
                        placementMoveOut,
                        migrationVM.EncoderNumber
                    );

                    if (checkInResponse.Result == 0)
                    {
                        string tid = checkInResponse.Data.Tid;
                        if (!string.IsNullOrEmpty(tid))
                        {
                            placement.TID = tid;
                            uow.GenericRepository<EF.BedSpacePlacement>().Update(placement);
                            uow.SaveChanges();
                        }
                    }
                    else
                    {
                        throw new Exception($"Guest Check-in failed. Room: {newBedSpace.Room.RoomName}, Message: {checkInResponse?.Msg}");
                    }

                    var ciemsResponse = await iTTLockAuth.EMSCheckin(
                        newBedSpace.Room.RoomName,
                        clientIntegrationMesserschmitt.Access_Token
                    );

                    if (ciemsResponse?.Result != 0)
                    {
                        throw new Exception($"CIEMS Check-in failed. Room: {newBedSpace.Room.RoomName}, Message: {ciemsResponse?.Msg}");
                    }
                }

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Update,
                    ActionId = (int)Enumeration.CorrespondenceAction.SwapBedSpace,
                    PK = placement.BedSpacePlacementID.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "Swap Bed Space",
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = booking.PersonID,
                    Reference = booking.BookingNumber,
                };
                auditLogsService.AddAuditLog(auditLog);

                return true;
            }

            catch (Exception)
            {
                throw;
            }

        }


        #region Algorithm
        public SelectListVM AssignBedSpaceToPerson(int bookingId, AddBedSpacePlacementVM model)
        {
            var booking = uow.GenericRepository<EF.Booking>().Table
                .Include(b => b.PriceConfig.RoomType)
                .FirstOrDefault(b => b.BookingID == bookingId);

            if (booking == null)
            {
                return null;
            }
            // Filter available bed spaces by the booking's room type

            var availableBedSpaces = GetAvailableBedSpacesForRoomType(booking.PriceConfig.RoomType.RoomName);

            // Group available bed spaces by building, floor, and room
            var groupedBedSpaces = availableBedSpaces
                .GroupBy(b => new { b.BuildingName, b.FloorName, b.RoomName })
                .OrderBy(group => group.Key.BuildingName) // Order by building name
                .ThenBy(group => group.Key.FloorName) // Then by floor name
                .SelectMany(group => group.OrderBy(b => b.BedName).ToList()) // Order by bed name within each room
                .ToList();

            // Select the first available bed space from the grouped bed spaces
            var assignedBedSpace = groupedBedSpaces.FirstOrDefault();

            if (assignedBedSpace != null)
            {
                model.BedSpaceID = assignedBedSpace.BedSpaceID;
                return assignedBedSpace;
            }
            else
            {
                return null;
            }
        }

        #endregion


        public static HttpClient HttpClientInstance = new HttpClient();
        public static async Task<DeleteCardResponse> DeleteCardAsync(string clientId, string accessToken, int lockId, int cardId, int deleteType, long date)
        {
            try
            {
                {
                    string apiUrl = "https://euapi.sciener.com/v3/identityCard/delete";

                    // Create request parameters
                    var requestParameters = new Dictionary<string, string>
            {
                {"clientId", clientId},
                {"accessToken", accessToken},
                {"lockId", lockId.ToString()},
                {"cardId", cardId.ToString()},
                {"deleteType", deleteType.ToString()},
                {"date", date.ToString()}
            };

                    // Format parameters as application/x-www-form-urlencoded
                    var content = new FormUrlEncodedContent(requestParameters);

                    // Send POST request
                    var response = await HttpClientInstance.PostAsync(apiUrl, content).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return JsonConvert.DeserializeObject<DeleteCardResponse>(responseContent);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new HttpRequestException($"Error: {response.StatusCode} - {response.ReasonPhrase}. Content: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in DeleteCardAsync: {ex.Message}", ex);
            }
        }

        // In your services, add these methods to return queryable objects
        public IQueryable<BedSpacePlacement> GetPlacementQueryable()
        {
            return uow.GenericRepository<BedSpacePlacement>().Table;
        }





    }
}
