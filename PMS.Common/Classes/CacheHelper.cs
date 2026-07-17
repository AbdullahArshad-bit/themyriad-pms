using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Common.Classes
{
    public class CacheHelper
    {
        private static readonly ObjectCache Cache = MemoryCache.Default;

        public static T GetOrAdd<T>(string key, Func<T> fetchFunction, int cacheDurationInMinutes)
        {
            if (Cache.Contains(key))
            {
                return (T)Cache[key];
            }

            var value = fetchFunction();
            if (value != null)
            {
                Cache.Add(key, value, DateTimeOffset.UtcNow.AddMinutes(cacheDurationInMinutes));
            }

            return value;
        }
    }
}
