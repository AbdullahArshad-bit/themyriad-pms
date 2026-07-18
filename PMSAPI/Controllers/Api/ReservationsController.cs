using PMS.DTO;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.DTO.ViewModels.BookingViewModels;
using PMS.DTO.ViewModels.PersonManageViewModels;
using PMS.Services.Services.BedSpacePlace;
using PMS.Services.Services.Booking;
using PMS.Services.Services.Integration;
using PMS.Services.Services.Person;
using PMSAPI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.Invoicings;
using PMS.DTO.ViewModels.ApiViewModels;

namespace PMSAPI.Controllers.Api
{
    /// <summary>
    /// Reservation lifecycle for a property. Reuses existing PMS Booking and
    /// BedSpacePlacement services (create, get, list, assign, check-in/out, cancel).
    /// </summary>
    [RoutePrefix("integration/api/v1")]
    public class ReservationsController : IntegrationApiController
    {
        private readonly IBookingService bookingService;
        private readonly IBedSpacePlacementService placementService;
        private readonly IWebhookService webhookService;
        private readonly IPersonService personService;
        private readonly IInvoicingService invoicingService;
        private readonly UnitOfWork<PMSEntities> uow;

        public ReservationsController(
            IBookingService _bookingService,
            IBedSpacePlacementService _placementService,
            IWebhookService _webhookService,
            IPersonService _personService,
            IInvoicingService _invoicingService,
            UnitOfWork<PMSEntities> _uow)
        {
            bookingService = _bookingService;
            placementService = _placementService;
            webhookService = _webhookService;
            personService = _personService;
            invoicingService = _invoicingService;
            uow = _uow;
        }

        [HttpPost]
        [Route("properties/{propertyId:int}/reservations")]
        public async Task<ApiResponse<ReservationResponse>> Create(int propertyId, CreateReservationRequest model)
        {
            try
            {
                if (model == null)
                {
                    return Fail<ReservationResponse>("Request body is required.");
                }

                // Resolve personId: either use the supplied ID or create/match a guest on the fly.
                int resolvedPersonId = 0;
                if (model.PersonId.HasValue && model.PersonId.Value > 0)
                {
                    resolvedPersonId = model.PersonId.Value;
                }
                else if (model.Guest != null)
                {
                    if (string.IsNullOrWhiteSpace(model.Guest.FullName) || string.IsNullOrWhiteSpace(model.Guest.Email))
                    {
                        return Fail<ReservationResponse>("Guest.FullName and Guest.Email are required when PersonId is not provided.");
                    }

                    // Try to match an existing guest by email first (avoid duplicates).
                    var existing = personService.GetPersonQueryable()
                        .FirstOrDefault(p => p.Email == model.Guest.Email && p.LocationId == propertyId);

                    if (existing != null)
                    {
                        resolvedPersonId = existing.PersonID;
                    }
                    else
                    {
                        // Create a new guest profile.
                        var createdBy = string.IsNullOrWhiteSpace(PMS.Common.Globals.User?.Email) ? "integration-api" : PMS.Common.Globals.User.Email;
                        var code = personService.GetMaxPersonCode(propertyId);  
                        var personVm = new AddPersonVM 
                        {
                            LocationId = propertyId,
                            Code = code,
                            Title = "Mr.",
                            FullName = model.Guest.FullName,
                            Email = model.Guest.Email,
                            Phone = model.Guest.Phone ?? "-",
                            Gender = model.Guest.Gender ?? "Male",
                            // [FIX] 'DOB' database mein Not Nullable hai. Agar API se DateOfBirth nahi aati,
                            // toh SQL Server 'datetime2' out-of-range error se bachne ke liye 1990-01-01 pass kiya gaya.
                            DOB = model.Guest.DateOfBirth ?? new DateTime(1990, 1, 1),
                            Nationality = model.Guest.Nationality ?? "Unknown",
                            PassportNumber = model.Guest.PassportNumber ?? "-",
                            ResidentWhatsappNumber = model.Guest.WhatsappNumber,
                            UniversityId = 1,
                            // [FIX] 'CreatedDate' bhi zaroori hai warna default 0001-01-01 pass hota aur SQL crash ho jata.
                            CreatedDate = DateTime.Now,
                            CreatedBy = createdBy,
                            IsActive = true
                        };
                        var created = personService.AddPerson(personVm, null);
                        if (created == null)
                        {
                            return Fail<ReservationResponse>("Failed to create guest profile.");
                        }
                        resolvedPersonId = created.PersonID;
                    }
                }
                else
                {
                    return Fail<ReservationResponse>("Either PersonId or Guest details must be provided.");
                }

                var createdByForBooking = string.IsNullOrWhiteSpace(PMS.Common.Globals.User?.Email) ? "integration-api" : PMS.Common.Globals.User.Email;
                var vm = new AddBookingVM
                {
                    PersonID = resolvedPersonId,
                    PriceConfigID = model.PriceConfigId,
                    CheckInDate = model.CheckInDate,
                    CheckOut = model.CheckOutDate,
                    Requests = model.SpecialRequests,
                    CreatedBy = createdByForBooking,
                    // [FIX] Yahan bhi 'CreatedDate' explicitly pass ki gayi hai taake booking create hotay waqt datetime2 ka error na aaye.
                    CreatedDate = DateTime.Now
                };

                // [FIX] Booking aur Invoice dono ko ek sath Database Transaction mein wrap kiya gaya.
                // Taake agar invoice generate hone mein koi error aaye toh booking bhi automatically Rollback (undo) ho jaye aur data corrupt na ho.
                using (var transaction = uow.Context.Database.BeginTransaction())
                {
                    try
                    {
                        var booking = bookingService.AddBooking(vm);
                        if (booking == null)
                        {
                            return Fail<ReservationResponse>("Reservation could not be created.");
                        }

                        // [FIX] Naya Invoice banane ka saara logic yahan Controller mein likha gaya PMS.Services ko chhere baghair.
                        var priceConfig = uow.GenericRepository<PriceConfig>().GetById(model.PriceConfigId);
                        decimal amount = priceConfig?.InitialDeposit ?? 0;

                        var depositService = uow.GenericRepository<PMS.EF.Service>().Table
                            .FirstOrDefault(s => s.LocationId == propertyId && s.ServiceAmount == amount && s.ServiceName.Contains("Deposit") && s.IsActive == true && s.IsEnable == true);

                        if (depositService == null) throw new Exception("No deposit service found.");

                        // [FIX] FOREIGN KEY ERROR FIX: API requests mein logged-in user nahi hota jis se User ID '0' assign hoti thi.
                        // Isay fix karne ke liye humne web.config se 'admin' ka email utha kar uski valid ID (onlineUserId) nikali hai.
                        string onlineBookingUserEmail = System.Configuration.ConfigurationManager.AppSettings["OnlineBookingUserEmail"];
                        var onlineBookingUser = uow.GenericRepository<UserMaster>().Table.FirstOrDefault(u => u.Email == onlineBookingUserEmail) ?? uow.GenericRepository<UserMaster>().Table.FirstOrDefault();
                        int onlineUserId = onlineBookingUser?.ID ?? throw new Exception("No user found in the system to assign to invoice CreatedBy.");

                        var invoice = new Invoicing
                        {
                            StudentId = resolvedPersonId,
                            LocationId = propertyId,
                            TermID = priceConfig?.TermID ?? 0,
                            Code = invoicingService.GetMaxInvoiceCodeString(propertyId, 2), 
                            NetAmount = amount,
                            TotalPrice = amount,
                            InvoiceDate = DateTime.Now,
                            DueDate = DateTime.Now.AddDays(7),
                            CreatedDate = DateTime.Now,
                            // [FIX] 'No booking details found' error fix: Invoice Type 1 (Rental) ko Type 2 (Deposit) kiya 
                            // kyunke rental mein room placement required hoti hai jo booking ke waqt nahi hoti.
                            InvoiceTypeId = 2, 
                            IsApproved = true,
                            // [FIX] Foreign Key Constraint error se bachne ke liye valid Admin ki ID assign ki.
                            CreatedBy = onlineUserId,
                            ApprovedBy = onlineUserId
                        };

                        var invoiceDetail = new InvoicingDetail
                        {
                            ServiceId = depositService.ServiceId,
                            ServiceName = depositService.ServiceName,
                            Price = amount,
                            TotalAmount = amount,
                            TaxAmount = 0,
                            Description = "Deposit for online booking",
                            FromDate = DateTime.Now,
                            ToDate = DateTime.Now.AddMonths(1)
                        };

                        invoice.InvoicingDetails.Add(invoiceDetail);

                        // [FIX] Direct database insert kiya 'SaveInvoice' method ko call kiye baghair taake user id over-write na ho.
                        uow.GenericRepository<Invoicing>().Insert(invoice);
                        uow.SaveChanges();
                        int invoiceId = invoice.Id; // [FIX] Nayi generate hone wali invoice ki ID nikal li.

                        transaction.Commit();

                        var mapped = MapBooking(booking);
                        // [FIX] Payment gateway ko pass karne ke liye nayi Invoice ID response mein bhej di.
                        mapped.InvoiceId = invoiceId;

                        await webhookService.DispatchAsync(propertyId, WebhookEventTypes.ReservationCreated, mapped);
                        return Success(mapped, "Reservation created.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        var baseEx = ex.GetBaseException();
                        if (baseEx is System.Data.Entity.Validation.DbEntityValidationException validationEx)
                        {
                            var errorMessages = validationEx.EntityValidationErrors
                                    .SelectMany(x => x.ValidationErrors)
                                    .Select(x => x.PropertyName + ": " + x.ErrorMessage);
                            return Fail<ReservationResponse>("Validation Error: " + string.Join("; ", errorMessages));
                        }
                        return Fail<ReservationResponse>(baseEx.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                var baseEx = ex.GetBaseException();
                return Fail<ReservationResponse>(baseEx.ToString());
            }
        }

        [HttpGet]
        [Route("properties/{propertyId:int}/reservations/{reservationId:int}")]
        public ApiResponse<ReservationResponse> Get(int propertyId, int reservationId)
        {
            try
            {
                var booking = bookingService.GetBookingByID(reservationId);
                if (booking == null || booking.LocationID != propertyId)
                {
                    return NotFound<ReservationResponse>("Reservation not found.");
                }
                return Success(MapBooking(booking));
            }
            catch (Exception ex)
            {
                return Fail<ReservationResponse>(ex.GetBaseException().Message);
            }
        }

        [HttpGet]
        [Route("properties/{propertyId:int}/reservations")]
        public ApiResponse<ReservationListResponse> List(
            int propertyId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string status = null,
            DateTime? modifiedSince = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 200) pageSize = 20;
                if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
                    return Fail<ReservationListResponse>("fromDate must be earlier than or equal to toDate.");
                if (!string.IsNullOrWhiteSpace(status) && !new[] { "active", "cancelled", "canceled" }.Contains(status.Trim().ToLowerInvariant()))
                    return Fail<ReservationListResponse>("status must be active or cancelled.");

                var query = bookingService.GetBookingQueryable()
                    .Where(b => b.LocationID == propertyId);

                if (fromDate.HasValue)
                    query = query.Where(b => b.CheckInDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(b => b.CheckInDate <= toDate.Value);

                if (!string.IsNullOrWhiteSpace(status))
                {
                    bool cancelled = status.Trim().ToLower() == "cancelled" || status.Trim().ToLower() == "canceled";
                    query = query.Where(b => b.IsCancel == cancelled);
                }

                if (modifiedSince.HasValue)
                    query = query.Where(b => b.UpdatedDate >= modifiedSince.Value || b.CreatedDate >= modifiedSince.Value);

                var total = query.Count();
                var bookings = query
                    .OrderByDescending(b => b.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList()
                    .Select(MapBooking)
                    .ToList();

                return Success(new ReservationListResponse
                {
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)total / pageSize),
                    Items = bookings
                });
            }
            catch (Exception ex)
            {
                return Fail<ReservationListResponse>(ex.GetBaseException().Message);
            }
        }

        [HttpPut]
        [Route("properties/{propertyId:int}/reservations/{reservationId:int}/assign-unit")]
        public async Task<ApiResponse<ReservationActionResponse>> AssignUnit(int propertyId, int reservationId, AssignUnitRequest model)
        {
            try
            {
                if (model == null)
                {
                    return Fail<ReservationActionResponse>("Request body is required.");
                }

                var booking = bookingService.GetBookingByID(reservationId);
                if (booking == null)
                {
                    return NotFound<ReservationActionResponse>("Reservation not found.");
                }

                var placementVm = new AddBedSpacePlacementVM
                {
                    BookingID = reservationId,
                    BedSpaceID = model.UnitId,
                    MoveIn = model.MoveIn ?? booking.CheckInDate,
                    MoveOut = model.MoveOut ?? booking.CheckOutDate ?? booking.CheckInDate,
                    Requests = model.Notes
                };

                var result = placementService.AssignBedSpaceToPerson(reservationId, placementVm);
                var payload = new
                {
                    ReservationId = reservationId,
                    PropertyId = propertyId,
                    Assignment = result
                };
                await webhookService.DispatchAsync(propertyId, WebhookEventTypes.ReservationAssigned, payload);
                return Success(new ReservationActionResponse { ReservationId = reservationId, PropertyId = propertyId, Assignment = result }, "Unit assigned.");
            }
            catch (Exception ex)
            {
                return Fail<ReservationActionResponse>(ex.GetBaseException().Message);
            }
        }

        [HttpPut]
        [Route("properties/{propertyId:int}/reservations/{reservationId:int}/check-in")]
        public async Task<ApiResponse<ReservationActionResponse>> CheckIn(int propertyId, int reservationId, CheckInRequest model)
        {
            try
            {
                var placement = placementService.GetPlacementQueryable()
                    .FirstOrDefault(p => p.BookingID == reservationId && p.IsEnable);

                if (placement == null)
                {
                    return NotFound<ReservationActionResponse>("No active unit placement found for this reservation. Assign a unit first.");
                }

                var checkInTime = (model != null && model.CheckInTime.HasValue) ? model.CheckInTime.Value : DateTime.Now;
                var ok = await placementService.CheckInPlacement(
                    placement.BedSpacePlacementID,
                    checkInTime,
                    model?.CardNumber,
                    model?.EncoderNumber);

                if (!ok)
                {
                    return Fail<ReservationActionResponse>("Check-in failed.");
                }

                var payload = new { PlacementId = placement.BedSpacePlacementID, ReservationId = reservationId, CheckInTime = checkInTime };
                await webhookService.DispatchAsync(propertyId, WebhookEventTypes.ReservationCheckedIn, payload);
                return Success(new ReservationActionResponse { ReservationId = reservationId, PlacementId = placement.BedSpacePlacementID, CheckInTime = checkInTime }, "Checked in.");
            }
            catch (Exception ex)
            {
                return Fail<ReservationActionResponse>(ex.GetBaseException().Message);
            }
        }

        [HttpPut]
        [Route("properties/{propertyId:int}/reservations/{reservationId:int}/check-out")]
        public async Task<ApiResponse<ReservationActionResponse>> CheckOut(int propertyId, int reservationId, CheckOutRequest model)
        {
            try
            {
                var placement = placementService.GetPlacementQueryable()
                    .FirstOrDefault(p => p.BookingID == reservationId && p.IsEnable);

                if (placement == null)
                {
                    return NotFound<ReservationActionResponse>("No active unit placement found for this reservation.");
                }

                var checkOutTime = (model != null && model.CheckOutTime.HasValue) ? model.CheckOutTime.Value : DateTime.Now;
                var ok = await placementService.CheckOutPlacementAsync(placement.BedSpacePlacementID, checkOutTime);

                if (!ok)
                {
                    return Fail<ReservationActionResponse>("Check-out failed.");
                }

                var payload = new { PlacementId = placement.BedSpacePlacementID, ReservationId = reservationId, CheckOutTime = checkOutTime };
                await webhookService.DispatchAsync(propertyId, WebhookEventTypes.ReservationCheckedOut, payload);
                return Success(new ReservationActionResponse { ReservationId = reservationId, PlacementId = placement.BedSpacePlacementID, CheckOutTime = checkOutTime }, "Checked out.");
            }
            catch (Exception ex)
            {
                return Fail<ReservationActionResponse>(ex.GetBaseException().Message);
            }
        }

        [HttpPut]
        [Route("properties/{propertyId:int}/reservations/{reservationId:int}/cancel")]
        public async Task<ApiResponse<ReservationActionResponse>> Cancel(int propertyId, int reservationId, CancelReservationRequest model)
        {
            try
            {
                var booking = bookingService.GetBookingByID(reservationId);
                if (booking == null)
                {
                    return NotFound<ReservationActionResponse>("Reservation not found.");
                }

                var ok = bookingService.CancelBooking(reservationId);
                if (!ok)
                {
                    return Fail<ReservationActionResponse>("Cancellation failed.");
                }

                var payload = new
                {
                    ReservationId = reservationId,
                    Status = "Cancelled",
                    Reason = model?.Reason,
                    CancelledAt = DateTime.Now
                };
                await webhookService.DispatchAsync(propertyId, WebhookEventTypes.ReservationCancelled, payload);
                return Success(new ReservationActionResponse { ReservationId = reservationId, Status = "Cancelled", Reason = model?.Reason, CancelledAt = payload.CancelledAt }, "Reservation cancelled.");
            }
            catch (Exception ex)
            {
                return Fail<ReservationActionResponse>(ex.GetBaseException().Message);
            }
        }

        private static ReservationResponse MapBooking(PMS.EF.Booking booking)
        {
            return new ReservationResponse
            {
                Id = booking.BookingID,
                ConfirmationNumber = booking.BookingNumber,
                PropertyId = booking.LocationID,
                GuestId = booking.PersonID,
                GuestName = booking.Person != null ? booking.Person.FullName : null,
                PriceConfigId = booking.PriceConfigID,
                UnitGroupName = booking.PriceConfig != null && booking.PriceConfig.RoomType != null
                    ? booking.PriceConfig.RoomType.RoomName : null,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                Channel = booking.Channel,
                Source = booking.Source,
                IsCancelled = booking.IsCancel,
                Status = (booking.IsCancel == true) ? "Cancelled" : "Active"
            };
        }
    }
}


