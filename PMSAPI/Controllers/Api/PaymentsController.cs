using PMS.DTO;
using PMS.Services.Services.Integration;
using PMS.Services.Services.Booking;
using PMS.Repository.UnitOfWork;
using PMS.EF;
using PMS.Services.Services.PaymentGateway;
using PMSAPI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace PMSAPI.Controllers.Api
{
    /// <summary>
    /// Payment link generation and payment status. Reuses the existing PMS payment
    /// gateway service (Thawani / Network International, resolved per property).
    /// </summary>
    [RoutePrefix("integration/api/v1")]
    public class PaymentsController : IntegrationApiController
    {
        private readonly IPaymentGatewayService paymentGatewayService;
        private readonly IWebhookService webhookService;
        private readonly IBookingService bookingService;
        private readonly UnitOfWork<PMSEntities> uow;

        public PaymentsController(IPaymentGatewayService _paymentGatewayService, IWebhookService _webhookService, IBookingService _bookingService, UnitOfWork<PMSEntities> _uow)
        {
            paymentGatewayService = _paymentGatewayService;
            webhookService = _webhookService;
            bookingService = _bookingService;
            uow = _uow;
        }

        [HttpPost]
        [Route("properties/{propertyId:int}/reservations/{reservationId:int}/payment-link")]
        public async Task<ApiResponse<PaymentLinkResponse>> CreatePaymentLink(int propertyId, int reservationId, CreatePaymentLinkRequest model)
        {
            try
            {
                if (model == null || model.InvoiceId <= 0)
                {
                    return Fail<PaymentLinkResponse>("invoiceId is required. Create or retrieve an invoice first; this endpoint does not create invoices.");
                }

                var reservation = bookingService.GetBookingByID(reservationId);
                var invoice = uow.GenericRepository<Invoicing>().Table.FirstOrDefault(i => i.Id == model.InvoiceId);
                if (reservation == null || reservation.LocationID != propertyId) return NotFound<PaymentLinkResponse>("Reservation not found for this property.");
                if (invoice == null || invoice.LocationId != propertyId || invoice.StudentId != reservation.PersonID) return NotFound<PaymentLinkResponse>("Invoice not found for this reservation.");

                var responseUrl = string.IsNullOrWhiteSpace(model.ReturnUrl)
                    ? "/PaymentGateway/response?Respond="
                    : model.ReturnUrl;

                var response = paymentGatewayService.PayNow(model.InvoiceId, responseUrl);
                if (response == null || !response.Success)
                {
                    await webhookService.DispatchAsync(propertyId, WebhookEventTypes.PaymentFailed, new
                    {
                        ReservationId = reservationId,
                        InvoiceId = model.InvoiceId,
                        Message = response != null ? response.Message : "Payment link generation failed."
                    });
                    return Fail<PaymentLinkResponse>(response != null ? response.Message : "Payment link generation failed.");
                }

                var payload = new PaymentLinkResponse
                {
                    PaymentUrl = response.Data.ToString(),
                    TransactionReference = null // Or actual reference if available
                };
                await webhookService.DispatchAsync(propertyId, WebhookEventTypes.PaymentLinkCreated, new
                {
                    ReservationId = reservationId,
                    InvoiceId = model.InvoiceId,
                    Url = payload.PaymentUrl
                });
                return Success(payload, "Payment link generated.");
            }
            catch (Exception ex)
            {
                return Fail<PaymentLinkResponse>(ex.GetBaseException().Message);
            }
        }

        [HttpGet]
        [Route("properties/{propertyId:int}/payments/{reference}")]
        public async Task<ApiResponse<PaymentStatusResponse>> GetPaymentStatus(int propertyId, string reference)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reference))
                {
                    return Fail<PaymentStatusResponse>("Payment reference is required.");
                }

                var response = paymentGatewayService.GetUserPayment(reference);
                if (response == null || !response.Success)
                {
                    return Fail<PaymentStatusResponse>(response != null ? response.Message : "Payment status not found.");
                }

                if (response.Data != null)
                {
                    var invoice = uow.GenericRepository<Invoicing>().Table.FirstOrDefault(i => i.Code == response.Data.InvoiceCode);
                    var reservationId = invoice == null ? (int?)null : bookingService.GetBookingQueryable().Where(b => b.LocationID == propertyId && b.PersonID == invoice.StudentId).OrderByDescending(b => b.CreatedDate).Select(b => (int?)b.BookingID).FirstOrDefault();
                    await webhookService.DispatchAsync(propertyId, WebhookEventTypes.PaymentCompleted, new { ReservationId = reservationId, InvoiceId = invoice != null ? (int?)invoice.Id : null, InvoiceCode = response.Data.InvoiceCode, Reference = response.Data.ReferenceNo, Amount = response.Data.Amount, PaidAt = response.Data.Date });
                }

                return Success(new PaymentStatusResponse { ReferenceNo = response.Data.ReferenceNo, Amount = response.Data.Amount, InvoiceCode = response.Data.InvoiceCode, Date = response.Data.Date, Card = response.Data.Card, PaymentCode = response.Data.PaymentCode });
            }
            catch (Exception ex)
            {
                return Fail<PaymentStatusResponse>(ex.GetBaseException().Message);
            }
        }
    }
}

