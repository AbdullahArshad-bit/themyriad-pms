using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.NetIntViewModel
{
    public class Token
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
        public int refresh_expires_in { get; set; }
        public string token_type { get; set; }
    }

    public class TokenError
    {
        public string message { get; set; }
        public int code { get; set; }
    }

    public class OrderResponse
    {
        public _links _links { get; set; }
        public string action { get; set; }
        public string language { get; set; }
        public merchantAttributes merchantAttributes { get; set; }
        public amount amount {  get; set; }
        public string reference { get; set; }
        public string OrderReference { get; set; }
        public string currency { get; set; }
        public string merchantOrderReference {  get; set; }
        public string outletId { get; set; }
        public string paymentStatus { get; set; }

        public DateTime createDateTime { get; set; }
    }
    public class OrderBody
    {
        public string action { get; set; }
        public string emailAddress { get; set; }
        public string merchantOrderReference { get; set; }
        public billingAddress billingAddress { get; set; }
        public amount amount { get; set; }
        public merchantAttributes merchantAttributes { get; set; }
    }
    public class billingAddress
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string address1 { get; set; }
        public string city { get; set; }
        public string countryCode { get; set; }
    }
    public class amount
    {
        public string currencyCode { get; set; }
        public int value { get; set; }
    }
    public class merchantAttributes
    {
        public string redirectUrl { get; set; }
        public bool maskPaymentInfo { get; set; }
        public bool skipConfirmationPage { get; set; }
    }
    public class payment
    {
        public string href { get; set; }
        public authResponse authResponse { get; set; }
        public paymentMethod paymentMethod { get; set; }
        public savedCard savedCard { get; set; }
        public string state { get; set; }
    }
    public class _links
    {
        public payment payment { get; set; }
    }
    public class OrderResponseError
    {
        public DateTime timestamp { get; set; }
        public int status { get; set; }
        public string error { get; set; }
        public string message { get; set; }
        public string path { get; set; }
    }

    public class TransactionResponse
    {
        public string action { get; set; }
        public string language { get; set; }
        public merchantAttributes merchantAttributes { get; set; }
        public billingAddress billingAddress { get; set; }
        public string reference { get; set; }
        public string outletId { get; set; }
        public DateTime createDateTime { get; set; }
        public _embedded _embedded { get; set; }
    }
    public class _embedded
    {
        public List<payment> payment { get; set; }
    }
    public class authResponse
    {
        public string authorizationCode { get; set; }
        public bool success { get; set; }
        public string resultCode { get; set; }
        public string resultMessage { get; set; }
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

    //Muscat Paymentgateway Models


    public class AuthHeader
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
        public int refresh_expires_in { get; set; }
        public string token_type { get; set; }
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

    public class CheckoutResponse
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string description { get; set; }

        public DATA data { get; set; }
       
    }
    public class PaymentListResponse
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string description { get; set; }

        public List<payment_method> data { get; set; }
       
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

        public subscription subscription { get; set; }

    }

    public class Products
    {
        public string name { get; set; }
        public int quantity { get; set; }
        public int unit_amount { get; set; }
    }

    public class Metedata
    {
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; }

    }


    public class Checkout
    {

        public string client_reference_id { get; set; }

        public string mode { get; set; }

        public List<Products> products { get; set; }

        public string customer_id { get; set; }

        public string success_url { get; set; }

        public string cancel_url { get; set; }
        public string paln_id { get; set; }


        public Metedata metadata { get; set; }



    }

    public class Product
    {
        public string name { get; set; }
        public int quantity { get; set; }
        public decimal unit_amount { get; set; }
    }

    public class Metedataa
    {
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; }

    }
    public class GetTransResponse
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string description { get; set; }

        public DaTa data { get; set; }

    }

    public class DaTa
    {
        public string status { get; set; }

        public string client_reference_id { get; set; }
    }

    public class subscription
    {
        public payment_method payment_method { get; set; }

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


}
