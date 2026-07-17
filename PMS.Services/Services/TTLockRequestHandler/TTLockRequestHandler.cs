using Newtonsoft.Json;
using PMS.Services.Services.TTLockIntegration;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Net;

namespace PMS.Services.Services.TTLockRequestHandler
{
    public class TTLockRequestHandler : ITTLockRequestHandler
    {
        private readonly Lazy<ITTLockAuth> _ttLockAuth;
        public static readonly HttpClient _httpClientInstance = new HttpClient();

        public TTLockRequestHandler(Lazy<ITTLockAuth> ttLockAuth)
        {
            _ttLockAuth = ttLockAuth;
        }

        //sciener app backup
        public async Task<T> GetAsync<T>(string resource)
        {
            try
            {
                var response = await _httpClientInstance.GetAsync(resource).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (HttpRequestException ex)
            {
                // Handle network-level or response-related exceptions
                throw new Exception($"Error making GET request to {resource}: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                // Handle timeout cases
                throw new Exception($"Request to {resource} timed out: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                // Handle deserialization issues
                throw new Exception($"Error deserializing response from {resource}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // General exception handling for other issues
                throw new Exception($"An unexpected error occurred while making the GET request: {ex.Message}", ex);
            }
        }

        public async Task<T> GetAsync<T>(string resource, Dictionary<string, string> headers = null)
        {
            try
            {
                // Add headers to the HTTP client if provided
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (!_httpClientInstance.DefaultRequestHeaders.Contains(header.Key))
                        {
                            _httpClientInstance.DefaultRequestHeaders.Add(header.Key, header.Value);
                        }
                    }
                }

                var response = await _httpClientInstance.GetAsync(resource).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                return JsonConvert.DeserializeObject<T>(content);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error making GET request to {resource}: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"Request to {resource} timed out: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Error deserializing response from {resource}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred while making the GET request: {ex.Message}", ex);
            }
        }




        //public async Task<T> PostAsync<T>(string resource, Dictionary<string, string> formParams = null)
        //{
        //    try
        //    {
        //        HttpContent content = new FormUrlEncodedContent(formParams);

        //        // Send POST request
        //        var response = await _httpClientInstance.PostAsync(resource, content);
        //        if (!response.IsSuccessStatusCode)
        //        {
        //            var errorContent = await response.Content.ReadAsStringAsync();
        //            throw new HttpRequestException($"Error: {response.StatusCode}, Content: {errorContent}");
        //        }

        //        var responseContent = await response.Content.ReadAsStringAsync();
        //        return JsonConvert.DeserializeObject<T>(responseContent);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Request to {resource} failed: {ex.Message}", ex);
        //    }
        //}



        //public async Task<T> PostAsync<T>(string resource, Dictionary<string, string> formParams = null)
        //{
        //    try
        //    {
        //        HttpContent content = new FormUrlEncodedContent(formParams);

        //        // Send POST request
        //        var response = await _httpClientInstance.PostAsync(resource, content);
        //        if (!response.IsSuccessStatusCode)
        //        {
        //            var errorContent = await response.Content.ReadAsStringAsync();
        //            throw new HttpRequestException($"Error: {response.StatusCode}, Content: {errorContent}");
        //        }

        //        var responseContent = await response.Content.ReadAsStringAsync();
        //        return JsonConvert.DeserializeObject<T>(responseContent);
        //    }
        //    catch (HttpRequestException httpEx)
        //    {
        //        throw new Exception($"HTTP Request error while posting to {resource}: {httpEx.Message}", httpEx);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Request to {resource} failed: {ex.Message}", ex);
        //    }
        //}

        public async Task<T> PostAsync<T>(string resource, Dictionary<string, string> formParams = null)
        {
            try
            {
                HttpContent content = new FormUrlEncodedContent(formParams);

                // Send POST request
                var response = await _httpClientInstance.PostAsync(resource, content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error: {response.StatusCode}, Content: {errorContent}");
                }


                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            catch (HttpRequestException httpEx)
            {
                throw new Exception($"HTTP Request error while posting to {resource}: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Request to {resource} failed: {ex.Message}", ex);
            }
        }

        //Messerschmitt
        //public async Task<T> PostAsync<T>(string resource, Dictionary<string, string> formParams = null, Dictionary<string, string> headers = null)
        //{
        //    try
        //    {
        //        var jsonContent = JsonConvert.SerializeObject(formParams);
        //        HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        //        var requestMessage = new HttpRequestMessage(HttpMethod.Post, resource)
        //        {
        //            Content = content
        //        };

        //        if (headers != null)
        //        {
        //            foreach (var header in headers)
        //            {
        //                if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
        //                {
        //                    continue;
        //                }
        //                requestMessage.Headers.Add(header.Key, header.Value);
        //            }
        //        }

        //        var response = await _httpClientInstance.SendAsync(requestMessage);
        //        if (!response.IsSuccessStatusCode)
        //        {
        //            var errorContent = await response.Content.ReadAsStringAsync();
        //            throw new HttpRequestException($"Error: {response.StatusCode}, Content: {errorContent}");
        //        }

        //        var responseContent = await response.Content.ReadAsStringAsync();
        //        return JsonConvert.DeserializeObject<T>(responseContent);
        //    }
        //    catch (HttpRequestException httpEx)
        //    {
        //        // Log the exception
        //        Console.WriteLine($"HTTP Request error while posting to {resource}: {httpEx.Message}");
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the exception
        //        Console.WriteLine($"Request to {resource} failed: {ex.Message}");
        //        throw;
        //    }
        //}

        public async Task<T> PostAsync<T>(string resource, Dictionary<string, string> formParams = null, Dictionary<string, string> headers = null)
        {
            try
            {
                var jsonContent = JsonConvert.SerializeObject(formParams);
                HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, resource)
                {
                    Content = content
                };

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        requestMessage.Headers.Add(header.Key, header.Value);
                    }
                }

                var response = await _httpClientInstance.SendAsync(requestMessage);

                // Handle HTTP 500 as a successful response
                if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(responseContent);
                }

                // Throw an exception for other non-success status codes
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error: {response.StatusCode}, Content: {errorContent}");
                }

                var successResponseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(successResponseContent);
            }
            catch (HttpRequestException httpEx)
            {
                // Log the exception
                Console.WriteLine($"HTTP Request error while posting to {resource}: {httpEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Request to {resource} failed: {ex.Message}");
                throw;
            }
        }


        public async Task<T> PostAsyncNew<T>(string resource, Dictionary<string, string> formParams = null)
        {
            try
            {
                // Convert form parameters to JSON
                var jsonContent = JsonConvert.SerializeObject(formParams);
                HttpContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send POST request
                var response = await _httpClientInstance.PostAsync(resource, content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Error: {response.StatusCode}, Content: {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            catch (HttpRequestException httpEx)
            {
                throw new Exception($"HTTP Request error while posting to {resource}: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Request to {resource} failed: {ex.Message}", ex);
            }
        }



    }
}

