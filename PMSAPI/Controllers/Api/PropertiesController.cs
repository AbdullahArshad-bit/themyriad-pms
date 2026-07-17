using PMS.DTO;
using PMS.Services.Services.LocationContext;
using PMS.Services.Services.Setup;
using PMSAPI.Models;
using System;
using System.Linq;
using System.Web.Http;

namespace PMSAPI.Controllers.Api
{
    /// <summary>
    /// Property details for integration sync. Returns locations assigned to the logged-in PMS user.
    /// </summary>
    [RoutePrefix("integration/api/v1/properties")]
    public class PropertiesController : IntegrationApiController
    {
        private readonly ISetupService setupService;
        private readonly ILocationContextService locationContextService;

        public PropertiesController(ISetupService _setupService, ILocationContextService _locationContextService)
        {
            setupService = _setupService;
            locationContextService = _locationContextService;
        }

        [HttpGet]
        [Route("")]
        public ApiResponse<System.Collections.Generic.List<PropertyResponse>> GetProperties()
        {
            try
            {
                var properties = setupService.GetAllLocations().Select(MapProperty).ToList();
                return Success(properties);
            }
            catch (Exception ex)
            {
                return Fail<System.Collections.Generic.List<PropertyResponse>>(ex.GetBaseException().Message);
            }
        }

        [HttpGet]
        [Route("{propertyId:int}")]
        public ApiResponse<PropertyResponse> GetProperty(int propertyId)
        {
            try
            {
                var location = setupService.GetLocationByID(propertyId);
                if (location == null || !location.IsEnable)
                {
                    return NotFound<PropertyResponse>("Property not found.");
                }

                return Success(MapProperty(location));
            }
            catch (Exception ex)
            {
                return Fail<PropertyResponse>(ex.GetBaseException().Message);
            }
        }

        private static PropertyResponse MapProperty(PMS.EF.Location location)
        {
            return new PropertyResponse
            {
                Id = location.LocationID,
                Name = location.LocationName,
                Description = location.LocationDescription,
                Prefix = location.Prefix,
                IsEnabled = location.IsEnable
            };
        }
    }
}
