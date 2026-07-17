using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using Newtonsoft.Json;
using PMS.DTO;
using PMS.DTO.CommonDTO;
using PMS.DTO.ViewModels.NetIntViewModel;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.Payment;
using static PMS.Common.Classes.Enumeration;
using static PMS.DTO.ViewModels.PaymentGatewayViewModel;

namespace PMS.Services.Services.PaymentGateway
{
    public class NetIntPaymentGateway : IPaymentGateway
    {
        private readonly UnitOfWork<PMSEntities> _uow;
        private readonly IPaymentService _paymentService;

        //Dev/UAT Outlet ID
        //private static string outletId = "b4f07f30-5611-46ab-931d-a23c8be502a4";

        //Production Live Admin Outlet ID
        private static string outletId = "9e94fe2f-04f4-4df9-9669-7c471ea5c735";

        //Production Live StudentPortal Outlet ID
        private static string outletIdStudentPortal = "b4497510-2e7b-40c4-881e-aa1a72f63306";

        private class NetIntCredentials
        {
            public string AuthInfo { get; set; }
            public string OutletId { get; set; }
        }

        public NetIntPaymentGateway(UnitOfWork<PMSEntities> uow, IPaymentService paymentService)
        {
            _uow = uow;
            _paymentService = paymentService;
        }

        private Token GetToken(string authInfo)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", "Basic " + authInfo);
            headers.Add("Accept", "application/vnd.ni-identity.v1+json");

            var token = PayGatewayApiHelper<Token>.Post("identity/auth/access-token",
                null,
                headers,
                "application/vnd.ni-identity.v1+json");

            if (token.Success)
                return token.Data;
            else
                return null;
        }

        private NetIntCredentials GetCredentialsByContext()
        {
            if (IsStudentPortalRequest())
            {
                System.Diagnostics.Debug.WriteLine("[NetInt] Context: Student portal credentials selected.");
                return new NetIntCredentials
                {
                    AuthInfo = PayGatewayApiHelper<string>.AuthInfoStudentPortal,
                    OutletId = outletIdStudentPortal
                };
            }

            System.Diagnostics.Debug.WriteLine("[NetInt] Context: Admin credentials selected.");
            return new NetIntCredentials
            {
                AuthInfo = PayGatewayApiHelper<string>.AuthInfo,
                OutletId = outletId
            };
        }

        private bool IsStudentPortalRequest()
        {
            var context = HttpContext.Current;
            if (context == null || context.Request == null)
                return false;

            var explicitStudentPortalFlag = context.Items["UseStudentPortalNetIntCredentials"];
            if (explicitStudentPortalFlag is bool isStudentPortal)
            {
                System.Diagnostics.Debug.WriteLine("[NetInt] Explicit student portal flag detected: " + isStudentPortal);
                return isStudentPortal;
            }

            var routeData = context.Request.RequestContext?.RouteData;
            var routeArea = routeData?.DataTokens?["area"] as string;
            if (!string.IsNullOrWhiteSpace(routeArea) &&
                routeArea.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var rawUrl = context.Request.RawUrl ?? string.Empty;
            if (rawUrl.IndexOf("/student/", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        public ApiResponse<string> GeneratePaymentLink(Invoicing invoicing, string responseUrl)
        {
            var response = new ApiResponse<string>();
            
            var orderResponse = CreateOrder(invoicing, responseUrl);
            if (orderResponse.Success && orderResponse.Data != null)
            {
                response.Code = (int)HttpStatusCode.OK;
                response.Success = true;
                response.Data = orderResponse.Data._links.payment.href;
                response.Message = "SuccessFull";
                return response;
            }

            response.Code = (int)HttpStatusCode.NotFound;
            response.Success = false;
            response.Data = null;
            response.Message = string.IsNullOrWhiteSpace(orderResponse.Message) ? "Payment Failed!" : orderResponse.Message;
            return response;
        }

        public ApiResponse<PaymentResponseVM> GetTransactionStatus(string reference)
        {
            var response = new ApiResponse<PaymentResponseVM>();
            try
            {
                var model = new PaymentResponseVM();
                var tran = GetTransaction(reference);

                if (tran != null && tran._embedded != null && tran._embedded.payment != null && tran._embedded.payment.Count > 0)
                {
                    var payment = tran._embedded.payment[0];
                    model.Success = payment.authResponse != null && payment.authResponse.success;
                    model.Message = payment.authResponse != null ? payment.authResponse.resultMessage : "No auth response";
                    model.Reference = tran.reference;
                    model.CardDigits = payment.paymentMethod != null ? payment.paymentMethod.pan : "";
                }
                else
                {
                    model.Success = false;
                    model.Message = "Transaction not found";
                }

                response.Success = true;
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

        public string GetBaseUrl(HttpRequest request)
        {
            return request.Url.Scheme + "://" + request.Url.Authority;
        }

        public ApiResponse<DTO.ViewModels.NetIntViewModel.OrderResponse> CreateOrder(Invoicing invoicing, string responseUrl)
        {
            var credentials = GetCredentialsByContext();
            System.Diagnostics.Debug.WriteLine("[NetInt] CreateOrder using outlet: " + credentials.OutletId);
            var token = GetToken(credentials.AuthInfo);
            var response = new ApiResponse<DTO.ViewModels.NetIntViewModel.OrderResponse>();

            if (token == null)
            {
                response.Success = false;
                response.Message = "Unable to get payment gateway access token.";
                return response;
            }

            string baseRedirectUrl = GetBaseUrl(HttpContext.Current.Request);
            var configuredBaseUrl = ConfigurationManager.AppSettings["PaymentGatewayBaseUrl"];

            if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            {
                baseRedirectUrl = configuredBaseUrl.TrimEnd('/');
            }
            else if (baseRedirectUrl.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // N-Genius can reject localhost callback URLs at order-creation time.
                // Keep legacy fallback unless PaymentGatewayBaseUrl is explicitly configured.
                baseRedirectUrl = "http://www.themyriad.com:8020";
            }

            var callbackPath = responseUrl;
            if (!string.IsNullOrWhiteSpace(callbackPath) &&
                !callbackPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !callbackPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // NetInt redirect URL should be a clean path; keep old callers compatible by stripping query placeholders.
                var queryIndex = callbackPath.IndexOf('?');
                if (queryIndex >= 0)
                {
                    callbackPath = callbackPath.Substring(0, queryIndex);
                }
            }

            // Use callback path if provided, otherwise fallback to default callback path.
            string redirectUrl = string.IsNullOrEmpty(callbackPath)
                ? baseRedirectUrl + "/PaymentGateway/PayGatewayResponse"
                : (callbackPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || callbackPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? callbackPath
                    : baseRedirectUrl + callbackPath);

            OrderBody body = new OrderBody
            {
                action = "SALE",
                emailAddress = invoicing.Person.Email,
                merchantOrderReference = invoicing.Person.Code,
                billingAddress = new billingAddress
                {
                    firstName = invoicing.Person.FullName,
                    lastName = invoicing.Person.FullName,
                    address1 = "test",
                    city = "test",
                    countryCode = "test"
                },
                amount = new amount
                {
                    currencyCode = invoicing.LocationId == (int)LocationEnum.Muscat ? "OMR" : "AED",
                    value = Convert.ToInt32(invoicing.TotalPrice) * 100
                },
                merchantAttributes = new DTO.ViewModels.NetIntViewModel.merchantAttributes
                {
                    redirectUrl = redirectUrl,
                    maskPaymentInfo = false,
                    skipConfirmationPage = true   // Auto-redirect after payment without waiting for button click
                }
            };

            // DEBUG: See exact redirectUrl in VS Output window (View > Output > Debug)
            System.Diagnostics.Debug.WriteLine("[NetInt] ======= ORDER CREATION =======");
            System.Diagnostics.Debug.WriteLine("[NetInt] redirectUrl sent to N-Genius: " + redirectUrl);
            System.Diagnostics.Debug.WriteLine("[NetInt] skipConfirmationPage: true");

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", token.token_type + " " + token.access_token);
            headers.Add("Accept", "application/vnd.ni-payment.v2+json");

            var order = PayGatewayApiHelper<DTO.ViewModels.NetIntViewModel.OrderResponse>.Post("transactions/outlets/" + credentials.OutletId + "/orders",
                body,
                headers,
                "application/vnd.ni-payment.v2+json");

            if (order.Success && order.Data != null)
            {
                System.Diagnostics.Debug.WriteLine("[NetInt] Order created OK. Reference: " + order.Data.reference);
                System.Diagnostics.Debug.WriteLine("[NetInt] Payment link: " + order.Data._links?.payment?.href);
                AddPaymentGatewayData(order.Data.reference, order.Data, "", invoicing.Code);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[NetInt] Order creation FAILED: " + order.Message);
            }

            return order;
        }

        private bool AddPaymentGatewayData(string refs, DTO.ViewModels.NetIntViewModel.OrderResponse response, string paymentInvoiceId, string Code)
        {
            var model = new EF.PaymentGateway
            {
                TranRef = refs,
                PaymentInvoiceId = paymentInvoiceId,
                CustomerRef = response.merchantOrderReference,
                InvoiceId = Code,
                Amount = (decimal)(response.amount.value) / 100,
                CreatedDate = DateTime.Now,
                Status = "Fail"
            };
            _uow.GenericRepository<EF.PaymentGateway>().Insert(model);
            _uow.SaveChanges();
            return true;
        }

        public DTO.ViewModels.NetIntViewModel.TransactionResponse GetTransaction(string reference)
        {
            var primaryCredentials = GetCredentialsByContext();
            var secondaryCredentials = new NetIntCredentials
            {
                AuthInfo = PayGatewayApiHelper<string>.AuthInfo,
                OutletId = outletId
            };

            var response = GetTransactionByCredentials(reference, primaryCredentials);
            if (response != null)
                return response;

            if (primaryCredentials.OutletId != secondaryCredentials.OutletId)
            {
                response = GetTransactionByCredentials(reference, secondaryCredentials);
                if (response != null)
                    return response;
            }

            var studentCredentials = new NetIntCredentials
            {
                AuthInfo = PayGatewayApiHelper<string>.AuthInfoStudentPortal,
                OutletId = outletIdStudentPortal
            };

            if (primaryCredentials.OutletId != studentCredentials.OutletId &&
                secondaryCredentials.OutletId != studentCredentials.OutletId)
            {
                response = GetTransactionByCredentials(reference, studentCredentials);
                if (response != null)
                    return response;
            }

            return null;
        }

        private DTO.ViewModels.NetIntViewModel.TransactionResponse GetTransactionByCredentials(string reference, NetIntCredentials credentials)
        {
            System.Diagnostics.Debug.WriteLine("[NetInt] Transaction lookup using outlet: " + credentials.OutletId + ", reference: " + reference);
            var token = GetToken(credentials.AuthInfo);
            if (token == null)
            {
                System.Diagnostics.Debug.WriteLine("[NetInt] Token generation failed for outlet: " + credentials.OutletId);
                return null;
            }

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", token.token_type + " " + token.access_token);

            string action = "transactions/outlets/" + credentials.OutletId + "/orders/" + reference;
            var trans = PayGatewayApiHelper<DTO.ViewModels.NetIntViewModel.TransactionResponse>.Get(action, headers);

            if (trans.Success)
            {
                System.Diagnostics.Debug.WriteLine("[NetInt] Transaction lookup success for outlet: " + credentials.OutletId);
                return trans.Data;
            }

            System.Diagnostics.Debug.WriteLine("[NetInt] Transaction lookup failed for outlet: " + credentials.OutletId + ", message: " + trans.Message);

            return null;
        }
    }
}
