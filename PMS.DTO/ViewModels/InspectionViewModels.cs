using PMS.Common.Filters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.DTO.ViewModels.InspectionViewModels
{
    public class RatingListVM
    {
        public int InspectionRatingListID { get; set; }

        public string Name { get; set; }

        public bool IsActive { get; set; }
    }

    public class AddRatingListVM
    {

        public int InspectionRatingListID { get; set; }

        [Required, Display(Name = "Name")]
        public string Name { get; set; }

        [Required, Display(Name = "Active")]
        public bool IsActive { get; set; }

        public List<RatingListItem> RatingListItem { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }

    }
    public class RatingListItem
    {
        public int RatingListItemDetailID { get; set; }

        [Required, Display(Name = "Item Name")]
        public string RatingListItemName { get; set; }
        [Display(Name = "Item Description")]
        public string RatingListItemDescripttion { get; set; }
        [Required, Display(Name = "Percent of Charge")]
        public int RatingListItemPercent { get; set; }
    }

    public class InspectionFieldsVM
    {
        public int INspectionFieldID { get; set; }
        public string ModelName { get; set; }
        public string ShortLabel { get; set; }
        public string OrgGroup { get; set; }

        public string ratingList { get; set; }

        public bool IsActive { get; set; }
        public int ratingListID { get; set; }

        public List<RatingListVM> RatingList { get; set; }
    }


    public class AddInspectionFieldsVM
    {

        public int INspectionFieldID { get; set; }
        [Required, Display(Name = "Rating List item")]
        public int ratingListItemID { get; set; }

        public string ratingList { get; set; }

        [Required, Display(Name = "Model Name")]
        public string ModelName { get; set; }

        [Display(Name = "Long Label")]
        public string LongLabel { get; set; }

        [Required, Display(Name = "Short Label")]
        public string ShortLabel { get; set; }

        [Display(Name = "Organization Group")]
        public string OrgGroup { get; set; }

        public bool AllowNotes { get; set; }
        public bool AllowImages { get; set; }
        public bool AllowExternalIdentifier { get; set; }

        public bool AssociatedWithMonetary { get; set; }


        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class InspectionsVM
    {
        public int InspectionID { get; set; }
        public string CompareTo { get; set; }
        public string InspectionName { get; set; }
        public string InspectionType { get; set; }
        public bool IsActive { get; set; }
    }
    public class AddInspectionsVM
    {
        public int InspectionID { get; set; }
        public string CompareTo { get; set; }
        public int? CompareToInt { get; set; }
        [Required, Display(Name = "Inspection Name")]
        public string InspectionName { get; set; }
        [Display(Name = "Description")]
        public string InspectionDescription { get; set; }
        public List<InspectionGroup> InspectionGroup { get; set; }
        public List<InspectionFieldForSlect> InspectionFieldForSlect { get; set; }

        [Required, Display(Name = "Inspection Type")]
        public string InspectionType { get; set; }
        [Required, Display(Name = "Inspection Type")]
        public int InspectionTypeInt { get; set; }

        [Required, Display(Name = "Inspection Valid for")]
        public int InspectionValidDays { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class InspectionGroup
    {
        public int InspectionGroupID { get; set; }
        public string GroupName { get; set; }
    }

    public class InspectionFieldForSlect
    {
        public int InspectionFieldSelctID { get; set; }
        public string InspectionFieldID { get; set; }
        public int inspectionID { get; set; }
        public int InspectionFieldSelectedValue { get; set; }

    }

    public class InspectionTypeVM
    {
        public int InspectionTypeID { get; set; }
        public string InspectionTypeName { get; set; }
    }
    public class InspectionToCompareAgainstVM
    {
        public int InspectionComapareAgainstID { get; set; }
        public string InspectionCompareAgainstName { get; set; }
    }

    public class GenetatedInspectionsVM
    {
        public int ID { get; set; }
        public int InspectionID { get; set; }
        public string InspectionName { get; set; }

        public string InspectionType { get; set; }

        public string BedSpaceName { get; set; }

        public int BedSpaceID { get; set; }
        public string Remarks { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string Staff_Status { get; set; }
        public string Student_Status { get; set; }

        public string Maintenance_Status { get; set; }


        public string Maintenance_Remarks { get; set; }
        public bool IsEnable { get; set; }

    }
    public class GetGenetatedInspectionsVM
    {
        public int ID { get; set; }
        public int InspectionID { get; set; }
        public int BedSpaceID { get; set; }
        public string Remarks { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public int Student_Status { get; set; }
        public int Staff_Status { get; set; }
        public int Maintenance_Status { get; set; }
        public string Maintenance_Remarks { get; set; }

    }
    public class UpdateGenetatedInspectionsVM
    {
        public int ID { get; set; }
        public int InspectionID { get; set; }
        public string InspectionName { get; set; }

        public string InspectionType { get; set; }

        public string BedSpaceName { get; set; }

        public int BedSpaceID { get; set; }
        public string Remarks { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public int Staff_Status { get; set; }
        public int Student_Status { get; set; }

        public int Maintenance_Status { get; set; }


        public string Maintenance_Remarks { get; set; }

    }

    public class AllInspectionsData
    {
        public string inspectionName { get; set; }

        public int GeneratedInspectionID { get; set; }

        public string BedSpace_Name { get; set; }

        public List<InspectionFieldDetails> Fileds { get; set; }

    }

    public class InspectionFieldDetails
    {
        public int FieldID { get; set; }
        public string InspectionFieldName { get; set; }
        public List<RatingLists> ratingLists { get; set; }

    }

    public class RatingLists
    {
        public int RatingID { get; set; }
        public string RatingName { get; set; }

        public string ImageUrl { get; set; }

        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg, svg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase ImageSource { get; set; }

        public string RatingNote { get; set; }
        public int selectedID { get; set; }
        public int InspectionRatingDataID { get; set; }
        public List<RatingListItem> actualRatingLists { get; set; }
    }

    public class InspectionRatingDataVM
    {
        public int ID { get; set; }
        public int AssignedFieldID { get; set; }
        public int ratingListID { get; set; }
        public int GeneratedInspectionID { get; set; }
        public int SelectetRatinglistitemID { get; set; }
        public string RatingNote { get; set; }
        public string RatingimageUrl { get; set; }
        public Nullable<bool> IsEnable { get; set; }
        public Nullable<bool> IsActive { get; set; }

    }

    public class MaintenanceVM
    {
        public int GeneratedInspectionID { get; set; }

        public string InspectionName { get; set; }

        public string RatingName { get; set; }

        public string SelectedRating { get; set; }

        public string Note { get; set; }

        public string Image { get; set; }


    }
    public class InspectionViewVM
    {
        public string InspectionName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int EffectiveDays { get; set; }
        public string BedSpace { get; set; }
        public string StudentStatus { get; set; }
        public string StaffStatus { get; set; }
        public string MaintannaceStatus { get; set; }
        public string MaintananceRemarks { get; set; }

        public List<InspectionList> IFL { get; set; }

    }

    public class InspectionList
    {
        public string FieldName { get; set; }
        public string SelectedRating { get; set; }
        public string Note { get; set; }
        public string ImageAttached { get; set; }

        public string RecomendedCharge { get; set; }


    }


}
