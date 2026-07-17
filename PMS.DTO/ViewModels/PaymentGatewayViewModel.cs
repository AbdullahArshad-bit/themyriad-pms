using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels
{
    public class PaymentGatewayViewModel
    {
        public class CheckoutResponse
        {
            public bool success { get; set; }
            public int code { get; set; }
            public string description { get; set; }
            public DATA data { get; set; }
        }

        public class DATA
        {
            public string session_id { get; set; }
            public string client_reference_id { get; set; }
            public string customer_id { get; set; }
            public List<Product> products { get; set; }
            public int total_amount { get; set; }
            public string currency { get; set; }
            public string success_url { get; set; }
            public string cancel_url { get; set; }
            public string payment_status { get; set; }
            public string mode { get; set; }
            public string invoice { get; set; }
            public Metedata metedata { get; set; }
            public string created_at { get; set; }
            public string expire_at { get; set; }
        }

        public class Metedata
        {
            public string InvoiceCode { get; set; }
            public string Name { get; set; }
            public string Contact { get; set; }
            public string Email { get; set; }

        }

        public class Product
        {
            public string name { get; set; }
            public int quantity { get; set; }
            public decimal unit_amount { get; set; }
        }

        public class Customer
        {
            public bool result { get; set; }
            public int code { get; set; }
            public string description { get; set; }
            public Data data { get; set; }
        }

        public class Data
        {
            public string id { get; set; }
            public string customer_client_id { get; set; }
        }

        public class Checkout
        {
            public string client_reference_id { get; set; }
            public string mode { get; set; }
            public List<Product> products { get; set; }
            public string customer_id { get; set; }
            public string success_url { get; set; }
            public string cancel_url { get; set; }
            public string paln_id { get; set; }
            public Metedata metadata { get; set; }
        }

        public class OrderResponse
        {
            public _links _links { get; set; }
            public string action { get; set; }
            public string language { get; set; }
            public merchantAttributes merchantAttributes { get; set; }
            public string reference { get; set; }
            public string outletId { get; set; }
            public DateTime createDateTime { get; set; }
        }

        public class merchantAttributes
        {
            public string redirectUrl { get; set; }
            public bool maskPaymentInfo { get; set; }
            public bool skipConfirmationPage { get; set; }
        }

        public class _links
        {
            public payment payment { get; set; }
        }

        public class payment
        {
            public string href { get; set; }
            public authResponse authResponse { get; set; }
            public paymentMethod paymentMethod { get; set; }
            public savedCard savedCard { get; set; }
            public string state { get; set; }
        }

        public class paymentMethod
        {
            public string expiry { get; set; }
            public string cardholderName { get; set; }
            public string name { get; set; }
            public string cardType { get; set; }
            public string cardCategory { get; set; }
            public string issuingCountry { get; set; }
            public string pan { get; set; }
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

        public class authResponse
        {
            public string authorizationCode { get; set; }
            public bool success { get; set; }
            public string resultCode { get; set; }
            public string resultMessage { get; set; }
        }

        public class PaymentResponseVM
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public string CardDigits { get; set; }
            public string Reference { get; set; }
            public string BookingNumber { get; set; }
        }

        public class TransactionResponse
        {
            public string action { get; set; }
            public string language { get; set; }
            public merchantAttributes merchantAttributes { get; set; }
            public string reference { get; set; }
            public string outletId { get; set; }
            public DateTime createDateTime { get; set; }
            public _embedded _embedded { get; set; }
        }

        public class _embedded
        {
            public List<payment> payment { get; set; }
        }

        public class PaymentListResponse
        {
            public bool success { get; set; }
            public int code { get; set; }
            public string description { get; set; }

            public List<payment_method> data { get; set; }

        }

        public class payment_method
        {
            public string masked_card { get; set; }
            public string nickname { get; set; }
            public string brand { get; set; }
            public string card_type { get; set; }
            public string payment_id { get; set; }
            public int amount { get; set; }
        }

        public class PayGatewayOutput
        {
            public string ReferenceNo { get; set; }
            public decimal Amount { get; set; }
            public string InvoiceCode { get; set; }
            public string Date { get; set; }
            public string Card { get; set; }
            public string PaymentCode { get; set; }
        }
    }
}
