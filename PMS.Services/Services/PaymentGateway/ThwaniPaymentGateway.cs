using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using PMS.DTO;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Helpers;
using PMS.Services.Services.Payment;
using static PMS.Common.Classes.Enumeration;
using static PMS.DTO.ViewModels.PaymentGatewayViewModel;

namespace PMS.Services.Services.PaymentGateway
{
    public class ThwaniPaymentGateway : IPaymentGateway
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IPaymentService paymentService;

        public ThwaniPaymentGateway(UnitOfWork<PMSEntities> _uow, IPaymentService _paymentService)
        {
            uow = _uow;
            paymentService = _paymentService;
        }

        public ApiResponse<string> GeneratePaymentLink(Invoicing invoicing, string responseUrl)
        {
            var response = new ApiResponse<string>();
            try
            {
                string redirecturl = "";
                var config = uow.GenericRepository<SystemConfiguration>().Table.
                         Where(x => x.Type == (int)SystemConfigurationType.ThwaniOnlinePayment && x.IsEnable == true).
                         FirstOrDefault();

                if (config == null)
                    throw new Exception("Payment Configuration Not Found!");

                var paymentApiurl = new Uri(config.BaseUri + "/api/v1");
                var paymentredirectUri = new Uri(config.BaseUri + "/pay/");
                var orderresponse = CreateOrder(invoicing, config.SecretKey, paymentApiurl.ToString(), responseUrl);

                if (orderresponse != null)
                {
                    AddPaymentGatewayData(orderresponse.data.success_url.Substring(orderresponse.data.success_url.IndexOf("ref-")), orderresponse, orderresponse.data.invoice, invoicing.Code);

                    redirecturl = paymentredirectUri + orderresponse.data.session_id + "?key=" + config.PublisherKey;
                }

                response.Code = (int)HttpStatusCode.OK;
                response.Success = true;
                response.Data = redirecturl;
                response.Message = "SuccessFull";
                return response;
            }
            catch (Exception ex)
            {
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Success = false;
                response.Data = null;
                response.Message = "Payment Failed!";
                return response;
            }
        }

        public ApiResponse<PaymentResponseVM> GetTransactionStatus(string reference)
        {
            var response = new ApiResponse<PaymentResponseVM>();
            try
            {
                var model = new PaymentResponseVM();
                var config = uow.GenericRepository<SystemConfiguration>().Table.
                             Where(x => x.Type == (int)SystemConfigurationType.ThwaniOnlinePayment && x.IsEnable == true).
                             FirstOrDefault();

                if (config == null)
                    throw new Exception("Payment Configuration Not Found!");

                var paymentApiUri = new Uri(config.BaseUri + "/api/v1");
                CheckoutResponse tran = GetMuscatTransaction(reference, config.SecretKey, paymentApiUri.ToString());

                if (tran != null)
                {
                    model.Success = tran.data.payment_status == "paid";
                    model.Message = tran.data.payment_status;
                    model.Reference = reference;

                    if (model.Success)
                    {
                        var payment_Method = PayGatewayApiHelper<payment_method>.GetPaymentDetail("/payments?limit=1&skip=0&checkout_invoice=" + tran.data.invoice, null, config.SecretKey, paymentApiUri.ToString());
                        model.CardDigits = payment_Method?.data?.FirstOrDefault()?.masked_card;
                    }
                }
                else
                {
                    model.Success = false;
                    model.Message = "Payment Failed or Not Found!";
                }

                response.Success = true; // Request was successful (even if payment failed)
                response.Data = model;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
        }

        private CheckoutResponse CreateOrder(Invoicing invoice, string Muscatapikey, string BaseUri, string responseUrl)
        {
            var orderresponse = new CheckoutResponse();

            var request = HttpContext.Current.Request;
            string baseRedirectUrl = request.Url.Scheme + "://" + request.Url.Authority;
            var person = uow.GenericRepository<EF.Person>().Table.Where(x => x.PersonID == invoice.StudentId).FirstOrDefault();

            var gatewayCustomer = CreateCustomer(person.FullName, person.Email, Muscatapikey, BaseUri);
            if (gatewayCustomer.data.customer_client_id != null && gatewayCustomer.data.id != null)
            {
                List<Product> prod = new List<Product>();
                Metedata met = new Metedata
                {
                    InvoiceCode = invoice.Code,
                    Name = person.FullName,
                    Email = person.Email,
                    Contact = person.Phone
                };

                int maxProductNameLength = 40;
                foreach (var item in invoice.InvoicingDetails)
                {
                    var product = new Product
                    {
                        name = Truncate(item.ServiceName, maxProductNameLength),
                        quantity = 1,
                        unit_amount = Convert.ToInt32((decimal)(item.TotalAmount * 1000))
                    };
                    prod.Add(product);
                }

                string refs = "ref-" + Guid.NewGuid().ToString().Split('-')[0].ToUpper();

                Checkout checkout = new Checkout
                {
                    client_reference_id = person.Code,
                    mode = "payment",
                    products = prod,
                    customer_id = gatewayCustomer.data.id,
                    success_url = baseRedirectUrl + responseUrl + "PassSuccess&ref=" + refs,
                    cancel_url = baseRedirectUrl + responseUrl + "Fail&ref=" + refs,
                    paln_id = "",
                    metadata = met
                };
                orderresponse = PayGatewayApiHelper<OrderResponse>.Post("/checkout/session", checkout, Muscatapikey, BaseUri);
            }
            return orderresponse;
        }

        private Customer CreateCustomer(string FullName, string Email, string Muscatapikey, string baseApiAddress)
        {
            Customer customer = new Customer();
            try
            {
                var body = new
                {
                    client_customer_id = FullName + " - " + Email
                };
                customer = PayGatewayApiHelper<Customer>.Post("/customers", body, null, Muscatapikey, baseApiAddress);
            }
            catch (Exception)
            {
                return customer;
            }
            return customer;
        }

        private CheckoutResponse GetMuscatTransaction(string reference, string Muscatapikey, string BaseUri)
        {
            string invoiceID = "";
            try
            {
                var data = uow.GenericRepository<EF.PaymentGateway>().Table.Where(x => x.TranRef == reference).FirstOrDefault();
                if (data != null)
                {
                    invoiceID = data.PaymentInvoiceId;
                }
            }
            catch (Exception) { }

            if (!string.IsNullOrEmpty(invoiceID))
            {
                string action = "/checkout/invoice/" + invoiceID;
                CheckoutResponse res = PayGatewayApiHelper<TransactionResponse>.Get(action, null, Muscatapikey, BaseUri);
                return res;
            }
            return null;
        }

        private bool AddPaymentGatewayData(string refs, CheckoutResponse response, string paymentInvoiceId, string Code)
        {
            var model = new EF.PaymentGateway
            {
                TranRef = refs,
                PaymentInvoiceId = paymentInvoiceId,
                CustomerRef = response.data.client_reference_id,
                InvoiceId = Code,
                Amount = (decimal)(response.data.total_amount) / 1000,
                CreatedDate = DateTime.Now,
                Status = "Fail"
            };
            uow.GenericRepository<EF.PaymentGateway>().Insert(model);
            uow.SaveChanges();
            return true;
        }

        private string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
