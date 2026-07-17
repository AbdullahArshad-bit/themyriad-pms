using PMS.DTO;
using PMS.Services.Services.Booking;
using PMS.Services.Services.Invoicings;
using PMS.Services.Services.Payment;
using PMSAPI.Models;
using System;
using System.Web.Http;

namespace PMSAPI.Controllers.Api
{
    /// <summary>
    /// Folio / balance for a reservation. PMS is invoice-centric, so the folio is
    /// presented from the resident's invoices, payments and outstanding balance.
    /// </summary>
    [RoutePrefix("integration/api/v1")]
    public class FolioController : IntegrationApiController
    {
        private readonly IBookingService bookingService;
        private readonly IInvoicingService invoicingService;
        private readonly IPaymentService paymentService;

        public FolioController(IBookingService _bookingService, IInvoicingService _invoicingService, IPaymentService _paymentService)
        {
            bookingService = _bookingService;
            invoicingService = _invoicingService;
            paymentService = _paymentService;
        }

        [HttpGet]
        [Route("properties/{propertyId:int}/reservations/{reservationId:int}/folio")]
        public ApiResponse<FolioResponse> GetFolio(int propertyId, int reservationId)
        {
            try
            {
                var booking = bookingService.GetBookingByID(reservationId);
                if (booking == null)
                {
                    return NotFound<FolioResponse>("Reservation not found.");
                }

                var personId = booking.PersonID;
                var invoices = invoicingService.GetByPersonId(personId);
                var payments = paymentService.GetPayment(personId);

                return Success(new FolioResponse
                {
                    ReservationId = reservationId,
                    GuestId = personId,
                    PropertyId = booking.LocationID,
                    Charges = invoices,
                    Payments = payments
                });
            }
            catch (Exception ex)
            {
                return Fail<FolioResponse>(ex.GetBaseException().Message);
            }
        }
    }
}
