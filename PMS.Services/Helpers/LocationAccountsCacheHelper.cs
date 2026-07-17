using PMS.DTO.ViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;

namespace PMS.Services.Helpers
{
    public static class LocationAccountsCacheHelper
    {
        private const string LOCATION_ACCOUNTS_KEY = "LocationAccounts_";
        private const string LOCATION_TRANSACTION_PASSWORD_KEY = "LocationTransactionPassword_";

        public static LocationAccountsCacheVM GetLocationAccounts(int locationId, UnitOfWork<PMSEntities> uow)
        {
            var cacheKey = $"{LOCATION_ACCOUNTS_KEY}{locationId}";
            var cache = HttpRuntime.Cache;

            var cachedAccounts = cache[cacheKey] as LocationAccountsCacheVM;
            if (cachedAccounts != null)
            {
                return cachedAccounts;
            }

            // If not in cache, fetch from database and cache it
            var locationSettings = uow.GenericRepository<EF.LocationSetting>()
                .Table.FirstOrDefault(ls => ls.LocationId == locationId);

            if (locationSettings != null)
            {
                var accounts = new LocationAccountsCacheVM
                {
                    LocationId = locationId,
                    AccountReceivableId = locationSettings.Def_Acc_Rec,
                    AccountPayableId = locationSettings.Def_Acc_Pay,
                    RevenueAccountId = locationSettings.Def_Acc_Discount,
                    AdvancePaymentAccountId = locationSettings.Def_Acc_Adv_Pay,
                    TransactionPassword = locationSettings.TransactionPassword,
                    LastUpdated = DateTime.Now
                };

                // Cache for exactly 4 hours 
                cache.Insert(
                    cacheKey,
                    accounts,
                    null,
                    DateTime.Now.AddHours(4),
                    Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.Default,
                    null
                );

                return accounts;
            }

            return null;
        }

        public static void UpdateLocationSettingsCache(int locationId, UnitOfWork<PMSEntities> uow)
        {
            var accountsCacheKey = $"{LOCATION_ACCOUNTS_KEY}{locationId}";
            var passwordCacheKey = $"{LOCATION_TRANSACTION_PASSWORD_KEY}{locationId}";
            var cache = HttpRuntime.Cache;
            
            // Remove both caches
            cache.Remove(accountsCacheKey);
            cache.Remove(passwordCacheKey);

            // Pre-load both caches with fresh data
            GetLocationAccounts(locationId, uow);
            GetLocationTransactionPassword(locationId, uow);
        }

        public static string GetLocationTransactionPassword(int locationId, UnitOfWork<PMSEntities> uow)
        {
            var cacheKey = $"{LOCATION_TRANSACTION_PASSWORD_KEY}{locationId}";
            var cache = HttpRuntime.Cache;

            var cachedPassword = cache[cacheKey] as string;
            if (cachedPassword != null)
            {
                return cachedPassword;
            }

            // If not in cache, fetch from database and cache it
            var locationSettings = uow.GenericRepository<EF.LocationSetting>()
                .Table.FirstOrDefault(ls => ls.LocationId == locationId);

            var transactionPassword = locationSettings?.TransactionPassword;

            if (transactionPassword != null)
            {
                // Cache for exactly 4 hours (same as accounts cache)
                cache.Insert(
                    cacheKey,
                    transactionPassword,
                    null,
                    DateTime.Now.AddHours(4),
                    Cache.NoSlidingExpiration,
                    System.Web.Caching.CacheItemPriority.Default,
                    null
                );
            }

            return transactionPassword;
        }

    }

}
