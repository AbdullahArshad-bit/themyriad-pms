using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using PMS.

namespace TheMyriad.Common
{
    public class PayGatewayApiHelper<T>
    {
        ////dev@bits-global.com
        //public static string AuthInfo = "ZWJkM2I5ZWEtYjM0OS00ZTQzLWJiOTEtOTgxNDY1N2UxMGVhOjZjZDRmNmUxLWEyYWYtNGQyMi1hZDg3LTQ1MWMyMWU3NGMxNg==";

        ////ashirazi@fimpartners.com Development
        //public static string AuthInfo = "OWYxZTA4NDQtMDc1NS00YTA2LTg3MjctM2IwNTY0OWQ0ZWJkOjZmMmNkZmQ2LTUwODUtNDM5Ni05ZmFlLTBjZTBmMDJkMTcxNw==";

        ////ashirazi@fimpartners.com Production Live
        public static string AuthInfo = "NjM2MTQ0OWMtNWU1ZS00NWRiLWIzMjEtNzM5ZmFlODlmY2EzOjllZWYzODMyLWZmZjUtNGY5ZC05ODU2LTRmMGU2NWExNzBkYw==";

        ////Development
        //public static string baseApiAddress = "https://api-gateway.sandbox.ngenius-payments.com/";

        ////Live
        public static string baseApiAddress = "https://api-gateway.ngenius-payments.com/";

        public static ApiResponse<T> Get(string action, Dictionary<string, string> headers)
        {
            ApiResponse<T> resp = new ApiResponse<T>();

            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(baseApiAddress);
                    client.DefaultRequestHeaders.Accept.Clear();

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, action);
                    foreach (var val in headers)
                    {
                        request.Headers.Add(val.Key, val.Value);
                    }


                    HttpResponseMessage response = client.SendAsync(request).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        var data = JsonConvert.DeserializeObject<T>(result);
                        resp = new ApiResponse<T> { Success = true, Code = 200, Message = "", Data = data };
                    }
                    else
                    {
                        var res = response.Content.ReadAsStringAsync().Result;
                        res = Newtonsoft.Json.JsonConvert.DeserializeObject<ReturnMessage>(res).Message;
                        resp = new ApiResponse<T> { Success = false, Code = 500, Message = res, Data = default(T) };
                    }
                }
            }
            catch (Exception ex)
            {
                resp = new ApiResponse<T> { Success = false, Code = 500, Message = ex.Message, Data = default(T) };
            }

            return resp;
        }
        public static ApiResponse<T> Post(string action, object body, Dictionary<string, string> headers, string mediaType = "application/vnd.ni-identity.v1+json")
        {
            ApiResponse<T> resp = new ApiResponse<T>();

            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(baseApiAddress);
                    client.DefaultRequestHeaders.Accept.Clear();

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, action);

                    string JsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);

                    request.Content = new StringContent(JsonBody,
                                                        Encoding.UTF8, mediaType);
                    foreach (var val in headers)
                    {
                        request.Headers.Add(val.Key, val.Value);
                    }


                    HttpResponseMessage response = client.SendAsync(request).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(result);
                        resp = new ApiResponse<T> { Success = true, Code = 200, Message = "", Data = data };
                    }
                    else
                    {
                        var res = response.Content.ReadAsStringAsync().Result;
                        res = Newtonsoft.Json.JsonConvert.DeserializeObject<ReturnMessage>(res).Message;
                        resp = new ApiResponse<T> { Success = false, Code = 500, Message = res, Data = default(T) };
                    }
                }
            }
            catch (Exception ex)
            {
                resp = new ApiResponse<T> { Success = false, Code = 500, Message = ex.Message, Data = default(T) };
            }

            return resp;
        }
        public static ApiResponse<T> Put(string action, object body, Dictionary<string, string> headers, string mediaType = "application/vnd.ni-identity.v1+json")
        {
            ApiResponse<T> resp = new ApiResponse<T>();

            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(baseApiAddress);
                    client.DefaultRequestHeaders.Accept.Clear();

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, action);

                    string JsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);

                    request.Content = new StringContent(JsonBody,
                                                        Encoding.UTF8, mediaType);
                    foreach (var val in headers)
                    {
                        request.Headers.Add(val.Key, val.Value);
                    }

                    HttpResponseMessage response = client.SendAsync(request).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        var data = JsonConvert.DeserializeObject<T>(result);
                        resp = new ApiResponse<T> { Success = true, Code = 200, Message = "", Data = data };
                    }
                    else
                    {
                        var res = response.Content.ReadAsStringAsync().Result;
                        res = Newtonsoft.Json.JsonConvert.DeserializeObject<ReturnMessage>(res).Message;
                        resp = new ApiResponse<T> { Success = false, Code = 500, Message = res, Data = default(T) };
                    }
                }
            }
            catch (Exception ex)
            {
                resp = new ApiResponse<T> { Success = false, Code = 500, Message = ex.Message, Data = default(T) };
            }

            return resp;
        }
        public static ApiResponse<T> Delete(string action, Dictionary<string, string> headers)
        {
            ApiResponse<T> resp = new ApiResponse<T>();

            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(baseApiAddress);
                    client.DefaultRequestHeaders.Accept.Clear();

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, action);

                    foreach (var val in headers)
                    {
                        request.Headers.Add(val.Key, val.Value);
                    }

                    HttpResponseMessage response = client.SendAsync(request).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(result);
                        resp = new ApiResponse<T> { Success = true, Code = 200, Message = "", Data = data };
                    }
                    else
                    {
                        var res = response.Content.ReadAsStringAsync().Result;
                        res = Newtonsoft.Json.JsonConvert.DeserializeObject<ReturnMessage>(res).Message;
                        resp = new ApiResponse<T> { Success = false, Code = 500, Message = res, Data = default(T) };
                    }
                }
            }
            catch (Exception ex)
            {
                resp = new ApiResponse<T> { Success = false, Code = 500, Message = ex.Message, Data = default(T) };
            }

            return resp;
        }
    }
}
