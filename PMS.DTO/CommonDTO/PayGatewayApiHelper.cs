using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using PMS.DTO;

namespace PMS.DTO.CommonDTO
{
    public class PayGatewayApiHelper<T>
    {
        // Dev/UAT AuthInfo
        //public static string AuthInfo = "OWYxZTA4NDQtMDc1NS00YTA2LTg3MjctM2IwNTY0OWQ0ZWJkOjZmMmNkZmQ2LTUwODUtNDM5Ni05ZmFlLTBjZTBmMDJkMTcxNw==";

        //Production Live Admin AuthInfo
        public static string AuthInfo = "NjM2MTQ0OWMtNWU1ZS00NWRiLWIzMjEtNzM5ZmFlODlmY2EzOjllZWYzODMyLWZmZjUtNGY5ZC05ODU2LTRmMGU2NWExNzBkYw==";

        //Production Live StudentPortal AuthInfo
        public static string AuthInfoStudentPortal = "MjlmMTE5N2QtOWU5Zi00NmM1LThkNjQtZDhmOTFhM2E0M2I0Ojc2NjViNDg4LWRiNWYtNDk2Yy1iOTIxLWNkM2JmYTI2ODMzYw==";

        //Dev/UAT
        //public static string baseApiAddress = "https://api-gateway.sandbox.ngenius-payments.com/";

        //Production Live
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
                        res = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<object>>(res).Message;
                        resp = new ApiResponse<T> { Success = false, Code = 500, Message = ExtractErrorMessage(res), Data = default(T) };
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
                        resp = new ApiResponse<T> { Success = false, Code = 500, Message = ExtractErrorMessage(res), Data = default(T) };
                    }
                }
            }
            catch (Exception ex)
            {
                resp = new ApiResponse<T> { Success = false, Code = 500, Message = ex.Message, Data = default(T) };
            }

            return resp;
        }

        private static string ExtractErrorMessage(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                return "Empty error response from payment gateway.";

            try
            {
                var obj = JObject.Parse(responseBody);

                var message = obj["message"]?.ToString();
                if (!string.IsNullOrWhiteSpace(message))
                    return message;

                var errors = obj["errors"] as JArray;
                if (errors != null && errors.Count > 0)
                {
                    var firstErrorMessage = errors[0]?["message"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(firstErrorMessage))
                        return firstErrorMessage;
                }
            }
            catch
            {
                // Fall back to raw response if JSON shape is unexpected.
            }

            return responseBody;
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
                        res = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<object>>(res).Message;
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
                        res = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<object>>(res).Message;
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
