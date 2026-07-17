using Newtonsoft.Json;
using PMS.DTO;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Helpers;
using PMS.Services.Services.Notifications;
using PMS.Services.Services.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static PMS.Common.Classes.Enumeration;
using static PMS.DTO.ViewModels.PaymentGatewayViewModel;

namespace PMS.Services.Services.PaymentGateway
{
    public class PaymentGatewayService : IPaymentGatewayService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly INotificationService notificationService;
        private readonly IPaymentService paymentService;
        private readonly IPaymentGatewayFactory gatewayFactory;

        public PaymentGatewayService(
            UnitOfWork<PMSEntities> _uow, 
            INotificationService _notificationService, 
            IPaymentService _paymentService,
            IPaymentGatewayFactory _gatewayFactory)
        {
            uow = _uow;
            notificationService = _notificationService;
            paymentService = _paymentService;
            gatewayFactory = _gatewayFactory;
        }

        public ApiResponse<PayGatewayOutput> GetUserPayment(string reference)
        {
            var response = new ApiResponse<PayGatewayOutput>();
            try
            {
                var paymentRecord = uow.GenericRepository<EF.PaymentGateway>().Table
                    .Where(x => x.TranRef == reference && x.Status == "paid")
                    .FirstOrDefault();

                PayGatewayOutput payment = null;
                if (paymentRecord != null)
                {
                    var paymentCode = (from invoice in uow.GenericRepository<Invoicing>().Table
                                       join studentLedger in uow.GenericRepository<StudentLedger>().Table
                                           on invoice.Id equals studentLedger.InvoiceId
                                       where invoice.Code == paymentRecord.InvoiceId
                                           && (
                                               (invoice.LocationId == (int)LocationEnum.Muscat && studentLedger.PaymentTypeName == "Thwani Online Payment")
                                               ||
                                               (invoice.LocationId == (int)LocationEnum.Dubai && studentLedger.PaymentTypeName == "N-Genius")
                                           )
                                       orderby studentLedger.Id descending
                                       select studentLedger.Code).FirstOrDefault();


                    payment = new PayGatewayOutput
                    {
                        ReferenceNo = paymentRecord.PaymentInvoiceId,
                        Amount = paymentRecord.Amount ?? 0,
                        InvoiceCode = paymentRecord.InvoiceId,
                        Date = paymentRecord.CreatedDate.ToString(),
                        Card = paymentRecord.CardLastDigits,
                        PaymentCode = paymentCode
                    };
                }

                response.Success = true;
                response.Data = payment;
                response.Message = "Success!";
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
        }

        public ApiResponse<string> PayNow(int Id, string responseUrl, bool isStudentPortal = false)
        {
            try
            {
                var invoice = uow.GenericRepository<Invoicing>().Table.Where(x => x.Id == Id).FirstOrDefault();
                if (invoice == null) throw new Exception("Invoice Not Found!");

                var gateway = gatewayFactory.GetGateway(invoice.LocationId);
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Items["UseStudentPortalNetIntCredentials"] = isStudentPortal;
                }

                return gateway.GeneratePaymentLink(invoice, responseUrl);
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Code = (int)HttpStatusCode.BadRequest,
                    Success = false,
                    Message = ex.Message
                };
            }
            finally
            {
                if (HttpContext.Current != null && HttpContext.Current.Items.Contains("UseStudentPortalNetIntCredentials"))
                {
                    HttpContext.Current.Items.Remove("UseStudentPortalNetIntCredentials");
                }
            }
        }

        public ApiResponse<PaymentResponseVM> PaymentResponse(string reference, string paymentResponse, bool IsAllowAnonymyouse = false)
        {
            var response = new ApiResponse<PaymentResponseVM>();
            try
            {
                // Find the existing payment record to determine location
                var paymentRecord = uow.GenericRepository<EF.PaymentGateway>().Table.Where(x => x.TranRef == reference).FirstOrDefault();
                if (paymentRecord == null) throw new Exception("Payment record not found!");

                var invoice = uow.GenericRepository<Invoicing>().Table.Where(x => x.Code == paymentRecord.InvoiceId).FirstOrDefault();
                if (invoice == null) throw new Exception("Invoice not found!");

                if (paymentResponse == "Fail")
                {
                    return new ApiResponse<PaymentResponseVM> { Success = false, Message = "Transaction Cancelled", Data = new PaymentResponseVM { Success = false } };
                }

                var gateway = gatewayFactory.GetGateway(invoice.LocationId);
                var statusResponse = gateway.GetTransactionStatus(reference);

                if (statusResponse.Success && statusResponse.Data.Success)
                {
                    var model = statusResponse.Data;
                    
                    // Avoid double processing
                    if (paymentRecord.Status != "paid")
                    {
                        paymentRecord.Status = "paid";
                        paymentRecord.CardLastDigits = model.CardDigits;
                        paymentRecord.Currency = (invoice.LocationId == (int)LocationEnum.Muscat) ? "OMR" : "AED";
                        paymentRecord.Message = "Paid By Credit Card";
                        uow.SaveChanges();

                        string paymentKeyCode = (invoice.LocationId == (int)LocationEnum.Muscat) ? "Thwani-01" : "Network-01";
                        bool isInvoiceUpdated = AddInvoicePayment(paymentRecord.InvoiceId, IsAllowAnonymyouse, paymentKeyCode);

                        if (isInvoiceUpdated)
                        {
                            model.Message = "Payment Added Successfully!";
                        }
                        else
                        {
                            model.Message = "Invoice Already Paid or Not Found!";
                        }

                        // Send Notification
                        var user = uow.GenericRepository<EF.Person>().Table.Where(x => x.Code == paymentRecord.CustomerRef).FirstOrDefault();
                        if (user != null)
                        {
                            var description = $"Your Payment: {paymentRecord.Amount} has been paid against Invoice: {paymentRecord.InvoiceId}. Ref: {paymentRecord.PaymentInvoiceId}";
                            notificationService.SendNotification(null, user.PersonID, "Student", "New Payment", description, "/Student/Payment/PaymentList", (PMS.Common.Globals.User == null ? user.Email : PMS.Common.Globals.User.Email));
                        }
                    }

                    response.Success = true;
                    response.Data = model;
                }
                else
                {
                    response.Success = false;
                    response.Message = statusResponse.Message ?? "Payment verification failed.";
                }

                return response;
            }
            catch (Exception ex)
            {
                return new ApiResponse<PaymentResponseVM> { Success = false, Message = ex.Message };
            }
        }

        private bool AddInvoicePayment(string InvoiceCode, bool IsAllowAnonymyouse, string paymentKeyCode)
        {
            var invoice = uow.GenericRepository<Invoicing>().Table.Where(x => x.Code == InvoiceCode && x.IsPaid != true).FirstOrDefault();
            if (invoice == null) return false;

            var payment = uow.GenericRepository<EF.PaymentGateway>().Table.Where(x => x.InvoiceId == InvoiceCode && x.Status == "paid").FirstOrDefault();
            if (payment == null) return false;

            var paymentType = uow.GenericRepository<PaymentType>().Table.Where(x => x.KeyCode == paymentKeyCode).FirstOrDefault();
            var code = paymentService.GetMaxCode(invoice.LocationId);

            var userId = IsAllowAnonymyouse ? 1 : PMS.Common.Globals.User.ID;

            var model = new StudentLedger
            {
                PaymentDate = DateTime.Now.Date,
                Code = code,
                InvoiceId = invoice.Id,
                StudentId = invoice.StudentId,
                CreditAmount = invoice.NetAmount,
                IsApproved = true,
                CreatedBy = userId,
                CreatedDate = DateTime.Now,
                LocationId = invoice.LocationId,
                ApprovedBy = userId,
                PaymentTypeId = paymentType.PaymentId,
                PaymentTypeName = paymentType.PayementName
            };
            invoice.IsPaid = true;
            uow.GenericRepository<StudentLedger>().Insert(model);
            uow.SaveChanges();
            return true;
        }
    }
}
