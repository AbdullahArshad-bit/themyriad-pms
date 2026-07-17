using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.DTO.ViewModels.SetupViewModels;
using PMS.EF;

namespace PMS.Services.Services.Setup
{
    public interface ISetupService
    {
        //All Room Features
        List<AllRoomFeature> GetAllRoomFeatures();
        AllRoomFeature GetAllRoomFeatureByID(int id);
        AllRoomFeature AddAllRoomFeature(AddAllRoomFeatureVM allRoomFeatureVM);
        AllRoomFeature UpdateAllRoomFeature(AddAllRoomFeatureVM allRoomFeatureVM);
        bool DeleteAllRoomFeature(int id);


        //Room Type Features
        List<RoomTypeFeature> GetAllRoomTypeFeatures();
        List<RoomTypeFeature> GetRoomTypeFeaturesByRoomTypeID(int roomTypeId);


        //Room Types
        List<RoomType> GetRoomTypes();
        List<RoomType> GetRoomTypesForAPI();
        List<RoomTypeDetail> GetRoomTypeDetails();
        RoomType GetRoomTypeByID(int id);
        RoomType GetRoomTypeByIDandLocation(int id, int? locationid);
        RoomType AddRoomType(AddRoomTypeVM roomTypeVM);
        RoomType UpdateRoomType(AddRoomTypeVM roomTypeVM);
        bool DeleteRoomType(int id);

        //Term
        List<Term> GetTerms();
        Term GetTermsByID(int id);
        Term AddTerm(AddTermVM addTermVM);
        Term AddTerm(AddTermVM addTermVM, HttpPostedFileBase file);
        Term UpdateTerm(AddTermVM addTermVM);
        bool DeleteTerm(int id);
        List<Term> GetTermsForDropDown();
        List<PriceConfigVM> GetTermsByRoomTypeID(int roomTypeId);
        List<FrequencyVm> GetFrequency();



        //Price Config.
        List<PriceConfig> GetPriceConfigs();
        PriceConfig GetPriceConfigByID(int id);
        PriceConfig AddPriceConfig(AddPriceConfigVM priceConfigVM);
        bool TryAddPriceConfig(AddPriceConfigVM priceConfig, out string reason);
        bool AddPriceConfiglist(AddPriceConfigVM priceConfigVM);
        PriceConfig UpdatePriceConfig(AddPriceConfigVM priceConfigVM);
        bool DeletePriceConfig(int id);
        List<PriceConfigVM> GetPriceConfigVM();

        //Location
        List<Location> GetLocations();
        List<Location> GetAllLocations();
        List<PriceConfigVM> GetTermsWithRoomNames();
        Location GetLocationByID(int id);
        Location AddLocation(AddLocationVM locationVM);
        Location UpdateLocation(AddLocationVM locationVM);
        bool DeleteLocation(int locationId);

        //Building
        List<Project> GetProjects();
        List<Project> GetProjects(int locationId);
        Project GetProjectByID(int id);
        Project AddProject(AddProjectVM projectVM);
        Project UpdateProject(AddProjectVM projectVM);
        bool DeleteProject(int id);

        //Blocks
        List<Building> GetBlocks();
        List<Building> GetBuildings();
        List<Building> GetBuildings(int projectId);
        Building GetBuildingByID(int id);
        Building AddBuilding(AddBuildingVM buildingVM);
        Building UpdateBuilding(AddBuildingVM buildingVM);
        bool DeleteBuilding(int id);

        //Floors
        List<Floor> GetFloors();
        List<Floor> GetFloors(int buildingId);
        Floor GetFloorByID(int id);
        Floor AddFloor(AddFloorVM floorVM);
        Floor UpdateFloor(AddFloorVM floorVM);
        bool DeleteFloor(int id);

        //Rooms
        List<Room> GetRooms();
        List<Room> GetRooms(int floorId);
        Room GetRoomByID(int id);
        Room AddRoom(AddRoomVM roomVM);
        Room ExcelAddRoom(AddRoomVM roomVM, HttpPostedFileBase file);
        Room UpdateRoom(AddRoomVM roomVM);
        bool DeleteRoom(int id);
        List<EF.GetAvailableRoomsForLandingPage_Result> GetAvailableRoomsForLandingPage(int LocationId, string university);
        //BedSpace
        List<BedSpace> GetBedSpaces(int roomId);
        List<BedSpace> GetBedSpaces();
        BedSpace GetBedSpaceByID(int id);
        BedSpace AddBedSpace(AddBedSpaceVM bedSpaceVM);
        BedSpace ExcelAddBedSpace(AddBedSpaceVM bedSpaceVM, HttpPostedFileBase file);
        BedSpace UpdateBedSpace(AddBedSpaceVM bedSpaceVM);
        bool DeleteBedSpace(int id);
        bool AddOrEditLocationSettings(LocationSettingsVM locationSettingsVM);
        LocationSettingsVM GetLocationSettingsByLocationid(int locationid);
        //Booking API Get All Data Terms and Images
        BookingSearchVM GetBookingSearches(string location, string roomType, string duration, string university, string currentCulture);
        BookingSearchVM GetRoomsDetail(string location, string currentCulture);
        PMS.EF.Booking AddNewBooking(BookingVM bookingVM);

        bool IsAlreadyBooked(string email, int LocationId);
        List<Currency> GetCurrency();

        V_RoomTypePriceDetail roomTypePriceDetail(int RoomTypePriceId);
        LocationSetting paymentGateway(int LocationId/*, string id*/);
        List<UniversitiesVM> GetUniversityListByLoactionId(int id, string culture);

        List<University> GetAllUniversityList();
        List<University> GetAllUniversities();

        UniversityVM GetUniversityById(int id);
        bool AddNewUniversity(UniversityVM universityVM);
        bool UpdateUniversity(UniversityVM universityVM);
        bool DeleteUniversity(int id);
        int? GetLastLocation();
        void UpdateLastLocation(int location);

        //Booking API Get All LocationSettings
        LocationSettingsApiVM GetLocationSettingByLocationid(int id, string culture);
        List<Location> GetLocationsByID(List<int> ids);
        List<UniversitiesVM> GetUniversityListByLoactionIdAPI(int id, string culture, string university);
        bool CheckMyriadIdAndEmail(string myriadID, string email);
        RefundRequest RefundRequest(RefundRequestVM requestVM);

    }
}
