using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.DTO.ViewModels.SetupViewModels;
using PMS.EF;

namespace PMS.Services.Services.BedSpacePlace
{
    public interface IBedSpacePlacementService
    {
        PlacementsResponse GetPlacements(PlacementsBinding request,string QueryBY, string searchValue, string start, string lenght, string query = null, DateTime? FromDate = null, DateTime? ToDate = null, int personId = 0, int id = 0, string orderBy = null, string orderDir = "asc");
        Task<PlacementsResponse> GetPlacementsExportAsync(string QueryBY, string query = null, DateTime? FromDate = null, DateTime? ToDate = null);
        List<PlacementsListVM> GetCheckin(int personId = 0);

        List<PlacementsListVM> GetNoContractPlacements(int personId = 0);

        BedSpacePlacement GetBedSpacePlacementById(int id);

        BedSpacePlacement AddBedSpacePlacement(AddBedSpacePlacementVM model);

        BedSpacePlacement ImportBedSpacePlacement(AddBedSpacePlacementVM model);

        BedSpacePlacement UpdateBedSpacePlacement(AddBedSpacePlacementVM model);

        BedSpacePlacement UpdateBedSpacePlacementDate(AddBedSpacePlacementVM model);

        bool DeleteBedSpacePlacement(int id);

        List<SelectListVM> GetAvailableBedSpaces();

        SelectListVM GetBedSpaceByID(int id);

        List<SelectListVM> GetAllBedSpaces();

        Task<bool> CheckInPlacement(int id, DateTime checkIntime, string cardNumber, string encoderNumber);
        Task<bool> ReissueCard(int id, string encoderNumber);
        string GetMaxPersonReferalCode(int personid);

        Task<bool> CheckOutPlacementAsync(int id, DateTime checkOuttime);

        bool CheckIfBedSpaceOcupied(int bedSpaceId, int bedSpacePlacementId = 0);

        BedSpacePlacement CheckForOverlappingPlacements(int bedSpaceId, DateTime moveIn, DateTime moveOut, int excludePlacementId = 0);

        Task<bool> SwapBedSpacePlacementAsync(BedSpacePlacementMigrationVM migrationVM);

        List<PlacementHistoryVM> GetMigrationHistoryByPlacementId(int PlacementId);

        SelectListVM AssignBedSpaceToPerson(int bookingId, AddBedSpacePlacementVM model);

        List<SelectListVM> GetAvailableBedSpacesForRoomType(string roomTypeName);

        List<GuestCountListVM> GetGuestCounts();

        IQueryable<BedSpacePlacement> GetPlacementQueryable();

    }
}
