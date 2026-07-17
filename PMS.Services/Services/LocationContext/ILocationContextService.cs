using System.Collections.Generic;

namespace PMS.Services.Services.LocationContext
{
    /// <summary>
    /// Service for managing location context and filtering throughout the application
    /// </summary>
    public interface ILocationContextService
    {
        /// <summary>
        /// Gets the list of assigned location IDs for the current user
        /// Checks session first, then falls back to user's assigned locations
        /// </summary>
        /// <returns>List of location IDs</returns>
        List<int> GetAssignedLocationIds();

        /// <summary>
        /// Gets the current/primary location ID for the user
        /// </summary>
        /// <returns>Primary location ID or null if none assigned</returns>
        int? GetCurrentLocationId();

        /// <summary>
        /// Checks if the user has access to a specific location
        /// </summary>
        /// <param name="locationId">Location ID to check</param>
        /// <returns>True if user has access to the location</returns>
        bool HasLocationAccess(int locationId);

        /// <summary>
        /// Gets the count of assigned locations
        /// </summary>
        /// <returns>Number of assigned locations</returns>
        int GetAssignedLocationCount();

        /// <summary>
        /// Checks if user has multiple location access
        /// </summary>
        /// <returns>True if user has access to multiple locations</returns>
        bool HasMultipleLocationAccess();

        /// <summary>
        /// Sets the current location in session (for location switching)
        /// </summary>
        /// <param name="locationIds">List of location IDs to set</param>
        void SetCurrentLocation(List<int> locationIds);

        /// <summary>
        /// Clears the current location from session
        /// </summary>
        void ClearCurrentLocation();

        /// <summary>
        /// Gets the user's actual assigned locations (ignores session, always returns user's assigned locations)
        /// This is used for dropdowns and UI elements that should show all available locations
        /// </summary>
        /// <returns>List of all location IDs assigned to the user</returns>
        List<int> GetUserAssignedLocationIds();
    }
}
