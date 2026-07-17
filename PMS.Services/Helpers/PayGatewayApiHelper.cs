using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using static PMS.DTO.ViewModels.PaymentGatewayViewModel;

namespace PMS.Services.Helpers
{
    public class PayGatewayApiHelper<T>
    {
        public static CheckoutResponse Get(string action, Dictionary<string, string> headers, string Muscatapikey, string baseApiAddress)
        {
            CheckoutResponse checkoutResponse = new CheckoutResponse();

            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(baseApiAddress);
                    client.DefaultRequestHeaders.Accept.Clear();

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, baseApiAddress + action);

                    request.Headers.Add("thawani-api-key", Muscatapikey);
                    HttpResponseMessage response = client.SendAsync(request).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        checkoutResponse = JsonConvert.DeserializeObject<CheckoutResponse>(result);
                    }
                    else
                    {
                        var res = response.Content.ReadAsStringAsync().Result;
                        checkoutResponse = JsonConvert.DeserializeObject<CheckoutResponse>(res);
                    }
                }
            }
            catch (Exception ex)
            {
                checkoutResponse = null;
            }

            return checkoutResponse;
        }

        public static Customer Post(string action, object body, Dictionary<string, string> headers, string Muscatapikey, string baseApiAddress)
        {
            Customer customer = new Customer();
            try
            {
                WebRequest tRequest = WebRequest.Create(baseApiAddress + action);
                tRequest.Method = "post";
                tRequest.ContentType = "application/json";
                //var data = body;
                //var data = new

                //{
                //    client_customer_id = "test@test.com"

                //};

                var serializer = new JavaScriptSerializer();
                var json = serializer.Serialize(body);
                Byte[] byteArray = Encoding.UTF8.GetBytes(json);
                tRequest.Headers.Add("thawani-api-key", Muscatapikey);
                tRequest.ContentLength = byteArray.Length;
                using (Stream dataStream = tRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    //using (WebResponse tResponse = tRequest.GetResponse())
                    using (WebResponse tResponse = tRequest.GetResponse())
                    {
                        using (Stream dataStreamResponse = tResponse.GetResponseStream())
                        {
                            using (StreamReader tReader = new StreamReader(dataStreamResponse))
                            {
                                var result = tReader.ReadToEnd();
                                customer = JsonConvert.DeserializeObject<Customer>(result);
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                string str = ex.Message;
            }
            return customer;
        }
        public static CheckoutResponse Post(string action, object body, string Muscatapikey, string baseApiAddress)
        {
            //ApiResponse<T> resp = new ApiResponse<T>();
            CheckoutResponse checkoutResponse = new CheckoutResponse();
            try
            {
                WebRequest tRequest = WebRequest.Create(baseApiAddress + action);
                tRequest.Method = "post";
                tRequest.ContentType = "application/json";
                //var data = body;
                var data = body;
                var serializer = new JavaScriptSerializer();
                var json = serializer.Serialize(data);
                Byte[] byteArray = Encoding.UTF8.GetBytes(json);
                tRequest.Headers.Add("thawani-api-key", Muscatapikey);
                tRequest.ContentLength = byteArray.Length;
                using (Stream dataStream = tRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    //using (WebResponse tResponse = tRequest.GetResponse())
                    using (WebResponse tResponse = tRequest.GetResponse())
                    {
                        using (Stream dataStreamResponse = tResponse.GetResponseStream())
                        {
                            using (StreamReader tReader = new StreamReader(dataStreamResponse))
                            {
                                var result = tReader.ReadToEnd();
                                checkoutResponse = JsonConvert.DeserializeObject<CheckoutResponse>(result);
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                string str = ex.Message;
                string stackTrace = ex.StackTrace;
            }
            return checkoutResponse;
        }
        public static PaymentListResponse GetPaymentDetail(string action, Dictionary<string, string> headers, string Muscatapikey, string baseApiAddress)
        {
            PaymentListResponse checkoutResponse = new PaymentListResponse();
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(baseApiAddress);
                    client.DefaultRequestHeaders.Accept.Clear();
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, baseApiAddress + action);
                    request.Headers.Add("thawani-api-key", Muscatapikey);
                    HttpResponseMessage response = client.SendAsync(request).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        checkoutResponse = JsonConvert.DeserializeObject<PaymentListResponse>(result);
                    }
                    else
                    {
                        var res = response.Content.ReadAsStringAsync().Result;
                        checkoutResponse = JsonConvert.DeserializeObject<PaymentListResponse>(res);
                    }
                }
            }
            catch (Exception ex)
            {
                checkoutResponse = null;
            }
            return checkoutResponse;
        }
    }
}
