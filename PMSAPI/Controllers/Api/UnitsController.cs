using PMS.DTO;
using PMS.Services.Services.Setup;
using PMSAPI.Models;
using System;
using System.Linq;
using System.Web.Http;

namespace PMSAPI.Controllers.Api
{
    /// <summary>
    /// Rooms / units sync for a property. Backed by the existing PMS Setup service.
    /// </summary>
    [RoutePrefix("integration/api/v1/properties")]
    public class UnitsController : IntegrationApiController
    {
        private readonly ISetupService setupService;

        public UnitsController(ISetupService _setupService)
        {
            setupService = _setupService;
        }

        [HttpGet]
        [Route("{propertyId:int}/units")]
        public ApiResponse<System.Collections.Generic.List<UnitResponse>> GetUnits(int propertyId)
        {
            try
            {
                var units = setupService.GetRooms()
                    .Where(r => r.RoomType != null && r.RoomType.LocationId == propertyId)
                    .Select(MapUnit)
                    .ToList();

                return Success(units);
            }
            catch (Exception ex)
            {
                return Fail<System.Collections.Generic.List<UnitResponse>>(ex.GetBaseException().Message);
            }
        }

        [HttpGet]
        [Route("{propertyId:int}/units/{unitId:int}")]
        public ApiResponse<UnitResponse> GetUnit(int propertyId, int unitId)
        {
            try
            {
                var room = setupService.GetRoomByID(unitId);
                if (room == null)
                {
                    return NotFound<UnitResponse>("Unit not found.");
                }
                return Success(MapUnit(room));
            }
            catch (Exception ex)
            {
                return Fail<UnitResponse>(ex.GetBaseException().Message);
            }
        }

        private static UnitResponse MapUnit(PMS.EF.Room room)
        {
            return new UnitResponse
            {
                Id = room.RoomID,
                RoomNumber = room.RoomName,
                FloorId = room.FloorID,
                RoomSize = room.RoomSize,
                Gender = room.RoomGender,
                UnitGroupId = room.RoomTypeID,
                UnitGroupName = room.RoomType != null ? room.RoomType.RoomName : null,
                IsEnabled = room.IsEnable,
                BedSpaces = room.BedSpaces == null ? null : room.BedSpaces.Select(b => new BedSpaceResponse
                {
                    Id = b.BedSpaceID,
                    Name = b.BedName,
                    Address = b.BedAddress,
                    Gender = b.RoomGender,
                    IsEnabled = b.IsEnable,
                    IsOccupied = b.Status
                }).ToList()
            };
        }
    }
}
