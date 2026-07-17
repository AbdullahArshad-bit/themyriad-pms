using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PMS.Common;

namespace PMS.Services.Services.LocationContext
{
    /// <summary>
    /// Implementation of location context service for managing location-based filtering
    /// </summary>
    public class LocationContextService : ILocationContextService
    {
        private const string LOCATION_SESSION_KEY = "locationid";
        private const int SESSION_TIMEOUT_MINUTES = 525600; // 365 days

        /// <summary>
        /// Gets the list of assigned location IDs for the current user
        /// Checks session first, then falls back to user's assigned locations
        /// </summary>
        /// <returns>List of location IDs</returns>
        public List<int> GetAssignedLocationIds()
        {
            try
            {
                // First, try to get from session
                var sessionLocations = GetLocationIdsFromSession();
                if (sessionLocations != null && sessionLocations.Any())
                {
                    return sessionLocations;
                }

                // Fallback to user's assigned locations from Globals
                var userAssignedLocations = Globals.User?.AssignedLocations;
                if (userAssignedLocations != null && userAssignedLocations.Any())
                {
                    return userAssignedLocations;
                }

                // Return empty list if no locations found
                return new List<int>();
            }
            catch (Exception)
            {
                // Log the exception if you have logging configured
                // For now, return empty list to prevent application crashes
                return new List<int>();
            }
        }

        /// <summary>
        /// Gets the current/primary location ID for the user
        /// </summary>
        /// <returns>Primary location ID or null if none assigned</returns>
        public int? GetCurrentLocationId()
        {
            var assignedLocations = GetAssignedLocationIds();
            return assignedLocations?.FirstOrDefault();
        }

        /// <summary>
        /// Checks if the user has access to a specific location
        /// </summary>
        /// <param name="locationId">Location ID to check</param>
        /// <returns>True if user has access to the location</returns>
        public bool HasLocationAccess(int locationId)
        {
            var assignedLocations = GetAssignedLocationIds();
            return assignedLocations?.Contains(locationId) == true;
        }

        /// <summary>
        /// Gets the count of assigned locations
        /// </summary>
        /// <returns>Number of assigned locations</returns>
        public int GetAssignedLocationCount()
        {
            var assignedLocations = GetAssignedLocationIds();
            return assignedLocations?.Count ?? 0;
        }

        /// <summary>
        /// Checks if user has multiple location access
        /// </summary>
        /// <returns>True if user has access to multiple locations</returns>
        public bool HasMultipleLocationAccess()
        {
            return GetAssignedLocationCount() > 1;
        }

        /// <summary>
        /// Sets the current location in session (for location switching)
        /// </summary>
        /// <param name="locationIds">List of location IDs to set</param>
        public void SetCurrentLocation(List<int> locationIds)
        {
            try
            {
                if (HttpContext.Current?.Session != null)
                {
                    HttpContext.Current.Session[LOCATION_SESSION_KEY] = locationIds;
                    HttpContext.Current.Session.Timeout = SESSION_TIMEOUT_MINUTES;
                }
            }
            catch (Exception)
            {
                // Log the exception if you have logging configured
                // For now, silently fail to prevent application crashes
            }
        }

        /// <summary>
        /// Clears the current location from session
        /// </summary>
        public void ClearCurrentLocation()
        {
            try
            {
                if (HttpContext.Current?.Session != null)
                {
                    HttpContext.Current.Session.Remove(LOCATION_SESSION_KEY);
                }
            }
            catch (Exception)
            {
                // Log the exception if you have logging configured
                // For now, silently fail to prevent application crashes
            }
        }

        /// <summary>
        /// Static helper method for use in static classes like ExcelHelper
        /// </summary>
        /// <returns>List of assigned location IDs</returns>
        public static List<int> GetAssignedLocationIdsStatic()
        {
            try
            {
                // First, try to get from session
                var sessionLocations = GetLocationIdsFromSessionStatic();
                if (sessionLocations != null && sessionLocations.Any())
                {
                    return sessionLocations;
                }

                // Fallback to user's assigned locations from Globals
                var userAssignedLocations = Globals.User?.AssignedLocations;
                if (userAssignedLocations != null && userAssignedLocations.Any())
                {
                    return userAssignedLocations;
                }

                // Return empty list if no locations found
                return new List<int>();
            }
            catch (Exception)
            {
                // Log the exception if you have logging configured
                // For now, return empty list to prevent application crashes
                return new List<int>();
            }
        }

        /// <summary>
        /// Private helper method to get location IDs from session
        /// </summary>
        /// <returns>List of location IDs from session or null</returns>
        private List<int> GetLocationIdsFromSession()
        {
            try
            {
                if (HttpContext.Current?.Session != null)
                {
                    return HttpContext.Current.Session[LOCATION_SESSION_KEY] as List<int>;
                }
            }
            catch (Exception)
            {
                // Log the exception if you have logging configured
                // For now, return null to fallback to user assigned locations
            }
            return null;
        }

        /// <summary>
        /// Gets the user's actual assigned locations (ignores session, always returns user's assigned locations)
        /// This is used for dropdowns and UI elements that should show all available locations
        /// </summary>
        /// <returns>List of all location IDs assigned to the user</returns>
        public List<int> GetUserAssignedLocationIds()
        {
            try
            {
                // Always return user's assigned locations from Globals, ignore session
                var userAssignedLocations = Globals.User?.AssignedLocations;
                if (userAssignedLocations != null && userAssignedLocations.Any())
                {
                    return userAssignedLocations;
                }

                // Return empty list if no locations found
                return new List<int>();
            }
            catch (Exception)
            {
                // Log the exception if you have logging configured
                // For now, return empty list to prevent application crashes
                return new List<int>();
            }
        }

        /// <summary>
        /// Static helper method to get location IDs from session
        /// </summary>
        /// <returns>List of location IDs from session or null</returns>
        private static List<int> GetLocationIdsFromSessionStatic()
        {
            try
            {
                if (HttpContext.Current?.Session != null)
                {
                    return HttpContext.Current.Session[LOCATION_SESSION_KEY] as List<int>;
                }
            }
            catch (Exception)
            {
                // Log the exception if you have logging configured
                // For now, return null to fallback to user assigned locations
            }
            return null;
        }
    }
}
