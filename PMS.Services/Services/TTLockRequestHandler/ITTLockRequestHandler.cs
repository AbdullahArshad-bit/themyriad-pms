using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMS.Services.Services.TTLockRequestHandler
{
    public interface ITTLockRequestHandler
    {
        Task<T> GetAsync<T>(string url);

        Task<T> GetAsync<T>(string resource, Dictionary<string, string> headers = null);

        //Task<T> PostAsync<T>(string resource, object model);
        Task<T> PostAsync<T>(string url, Dictionary<string, string> parameters);

        //Messerschmitt
        Task<T> PostAsync<T>(string resource, Dictionary<string, string> formParams = null, Dictionary<string, string> headers = null);

        Task<T> PostAsyncNew<T>(string resource, Dictionary<string, string> formParams = null);

    }
}
