using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.DTO.ViewModels.InspectionViewModels;
using PMS.DTO.ViewModels;
using PMS.EF;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;

namespace PMS.Services.Services.Inspection
{
    public interface IInspectionService
    {
        //Rating list
        List<RatingListVM> GetRating();

        AddRatingListVM GetRatingById(int id);

        bool AddRatingList(AddRatingListVM model);

        bool UpdateRatingList(AddRatingListVM model);

        bool DeleteInspection(int id);
        bool DeleteGenerateInspection(int id);

        List<RatingListItem> getRatingListItemsById(int id);

        //inspectionFields

        List<InspectionFieldsVM> getInspectionFields();

        AddInspectionFieldsVM getInspectionFieldById(int id);

        bool AddInspectionFIeld(AddInspectionFieldsVM model);

        bool UpdateInspectionField(AddInspectionFieldsVM model);

        bool DeleteInspectionField(int id);

        //inspections
        List<InspectionsVM> getInspections();
        List<Status> getStaus();



        List<InspectionTypeVM> getInspectionTypes();

        List<InspectionToCompareAgainstVM> getInspectionToCompareAgainst();
    
        AddInspectionsVM getInspectionById(int id);

        bool AddInspectionsVM(AddInspectionsVM model);

        bool UpdateInspection(AddInspectionsVM model);

        bool DeleteInspections(int id);

        bool GenerateInspectionsVM(GenetateInspectionVM model);
        List<GenetatedInspectionsVM> getGeneratedInspections();
        GenerateInspection UpdateGenerateInspectionsVM(UpdateGenetatedInspectionsVM model);
        GenerateInspection getGeneratedInspectionsbyid(int id);

        
        List<InpectionFieldsSelected> getInspectionFieldsSelected();

        bool ADDInspectionDetails(InspectionRatingDataVM model);

        List<MaintenanceVM> MaintenanceRequest();
        InspectionViewVM GenerateViewRequest(int id);

    }
}
