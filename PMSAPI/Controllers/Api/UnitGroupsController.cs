using PMS.DTO;
using PMS.Services.Services.Setup;
using PMSAPI.Models;
using System;
using System.Linq;
using System.Web.Http;

namespace PMSAPI.Controllers.Api
{
    /// <summary>
    /// Room types / unit groups sync for a property. Backed by the existing PMS Setup service.
    /// </summary>
    [RoutePrefix("integration/api/v1/properties")]
    public class UnitGroupsController : IntegrationApiController
    {
        private readonly ISetupService setupService;

        public UnitGroupsController(ISetupService _setupService)
        {
            setupService = _setupService;
        }

        [HttpGet]
        [Route("{propertyId:int}/unit-groups")]
        public ApiResponse<System.Collections.Generic.List<UnitGroupResponse>> GetUnitGroups(int propertyId)
        {
            try
            {
                var unitGroups = setupService.GetRoomTypesForAPI()
                    .Where(rt => rt.LocationId == propertyId)
                    .Select(MapUnitGroup)
                    .ToList();

                return Success(unitGroups);
            }
            catch (Exception ex)
            {
                return Fail<System.Collections.Generic.List<UnitGroupResponse>>(ex.GetBaseException().Message);
            }
        }

        [HttpGet]
        [Route("{propertyId:int}/unit-groups/{unitGroupId:int}")]
        public ApiResponse<UnitGroupResponse> GetUnitGroup(int propertyId, int unitGroupId)
        {
            try
            {
                var roomType = setupService.GetRoomTypeByID(unitGroupId);
                if (roomType == null)
                {
                    return NotFound<UnitGroupResponse>("Unit group not found.");
                }
                return Success(MapUnitGroup(roomType));
            }
            catch (Exception ex)
            {
                return Fail<UnitGroupResponse>(ex.GetBaseException().Message);
            }
        }

        private static UnitGroupResponse MapUnitGroup(PMS.EF.RoomType roomType)
        {
            return new UnitGroupResponse
            {
                Id = roomType.RoomTypeID,
                Code = roomType.RoomCode,
                Name = roomType.RoomName,
                Description = roomType.RoomDescription,
                Area = roomType.RoomArea,
                BedSpace = roomType.BedSpace,
                ActualPrice = roomType.Actual_Price,
                IsEnabled = roomType.IsEnable,
                Features = roomType.RoomTypeFeatures == null ? null : roomType.RoomTypeFeatures.Select(f => new RoomTypeFeatureResponse
                {
                    Id = f.RoomTypeFeatureId,
                    AllRoomFeatureId = f.AllRomFeatureID,
                    Name = f.AllRoomFeature != null ? f.AllRoomFeature.FeatureName : null
                }).ToList(),
                PriceConfigs = roomType.PriceConfigs == null ? null : roomType.PriceConfigs.Select(p => new PriceConfigResponse
                {
                    Id = p.PriceConfigID,
                    TermId = p.TermID,
                    Price = p.Price,
                    InitialDeposit = p.InitialDeposit,
                    Currency = p.Currency,
                    IsAvailable = p.IsAvailable
                }).ToList()
            };
        }
    }
}
