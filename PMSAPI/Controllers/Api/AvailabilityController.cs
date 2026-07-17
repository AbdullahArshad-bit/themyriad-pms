using PMS.DTO;
using PMS.Services.Services.BedSpacePlace;
using PMS.Services.Services.Setup;
using PMSAPI.Models;
using System;
using System.Linq;
using System.Web.Http;

namespace PMSAPI.Controllers.Api
{
    /// <summary>
    /// Availability by unit group for a property. Backed by existing PMS Setup and
    /// BedSpacePlacement services (current inventory availability).
    /// </summary>
    [RoutePrefix("integration/api/v1/properties")]
    public class AvailabilityController : IntegrationApiController
    {
        private readonly ISetupService setupService;
        private readonly IBedSpacePlacementService placementService;

        public AvailabilityController(ISetupService _setupService, IBedSpacePlacementService _placementService)
        {
            setupService = _setupService;
            placementService = _placementService;
        }

        /// <summary>
        /// Gets availability for a property across unit groups for the requested date range.
        /// </summary>
        /// <param name="propertyId">Property identifier.</param>
        /// <param name="fromDate">Inclusive start date for the availability window. Required.</param>
        /// <param name="toDate">Inclusive end date for the availability window. Required.</param>
        /// <param name="unitGroupId">Optional unit group filter.</param>
        /// <returns>A list of availability rows with unit-group counts and pricing.</returns>
        [HttpGet]
        [Route("{propertyId:int}/availability")]
        public ApiResponse<System.Collections.Generic.List<AvailabilityResponse>> GetAvailability(int propertyId, DateTime? fromDate = null, DateTime? toDate = null, int? unitGroupId = null)
        {
            try
            {
                if (!fromDate.HasValue || !toDate.HasValue)
                {
                    return Fail<System.Collections.Generic.List<AvailabilityResponse>>("Both fromDate and toDate are required.");
                }

                if (toDate.Value < fromDate.Value)
                {
                    return Fail<System.Collections.Generic.List<AvailabilityResponse>>("toDate must be the same or later than fromDate.");
                }

                var roomTypes = setupService.GetRoomTypesForAPI()
                    .Where(rt => rt.LocationId == propertyId);

                if (unitGroupId.HasValue)
                {
                    roomTypes = roomTypes.Where(rt => rt.RoomTypeID == unitGroupId.Value);
                }

                var result = roomTypes.Select(rt =>
                {
                    var available = placementService.GetAvailableBedSpacesForRoomType(rt.RoomName);
                    var priceFrom = rt.PriceConfigs == null || !rt.PriceConfigs.Any()
                        ? (decimal?)null
                        : rt.PriceConfigs.Min(p => p.Price);

                    return new AvailabilityResponse
                    {
                        UnitGroupId = rt.RoomTypeID,
                        UnitGroupName = rt.RoomName,
                        AvailableCount = available == null ? 0 : available.Count,
                        PriceFrom = priceFrom,
                        Currency = null,
                        FromDate = fromDate.Value,
                        ToDate = toDate.Value
                    };
                }).ToList();

                return Success(result);
            }
            catch (Exception ex)
            {
                return Fail<System.Collections.Generic.List<AvailabilityResponse>>(ex.GetBaseException().Message);
            }
        }
    }
}
