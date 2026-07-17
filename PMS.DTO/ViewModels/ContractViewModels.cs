using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.DTO.ViewModels.ContractViewModels
{
    public class ContractsListVM
    {
        public int ContractID { get; set; }

        public int ContractTypeID { get; set; }
        public string ContractNumber { get; set; }
        public string ContractName { get; set; }
        public int ContractVersion { get; set; }
        public string ContractReferenceNumber { get; set; }
        public string ContractType { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublish { get; set; }

        public string ContentType { get; set; }
        public string ContentValue { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
    }

    public class ContractTypesVM
    {
        public int ContractTypeID { get; set; }

        [Display(Name = "Contract Name")]
        [Required]
        public string ContractTypeName { get; set; }

        [Display(Name = "Contract Description")]
        public string ContractTypeDescription { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class AddContractVM
    {
        public int? LocationId { get; set; }
        public int ContractID { get; set; }
        public int OriginalContractID { get; set; }
        public ContractProperties Properties { get; set; }
        public ContractContent Content { get; set; }
        public List<ContractAssertions> Assertions { get; set; }
        public ContractSignature Signature { get; set; }
        public ContractEmail Email { get; set; }
        public ContractNotes Notes { get; set; }

        public bool IsEnable { get; set; }
        public bool IsPublish { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }
    public class ContractProperties
    {
        [Required, Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Contract Number")]
        public string ContractNumber { get; set; }

        [Required, MaxLength(255)]
        [Display(Name = "Name")]
        public string ContractName { get; set; }

        [Required]
        [Display(Name = "Version")]
        public int ContractVersion { get; set; }
        
        [Display(Name = "Reference No.")]
        public string ContractReferenceNumber { get; set; }

        [Required]
        [Display(Name = "Contract Type")]
        public int ContractTypeID { get; set; }
        public List<SelectListVM> ContractTypeList { get; set; }
        public string Description { get; set; }
    }
    public class ContractContent
    {

        [Display(Name = "Content")]
        public string EditorContent { get; set; }

        [Display(Name = "File")]
        public HttpPostedFileBase FileContent { get; set; }

        public string FileContentPath { get; set; }

        [Required, Display(Name = "Content Type")]
        public string ContentType { get; set; }
    }
    public class ContractAssertions
    {
        public int ContractAssertionID { get; set; }
        public bool ShowCheckBox { get; set; }
        public bool AssertionRequired { get; set; }
        public string AssertionText { get; set; }
        public string AssertionHyperlinkText { get; set; }
        public string AssertionHyperlinkUrl { get; set; }
    }
    public class ContractSignature
    {

        [Required, Display(Name = "Require Electronic Signature")]
        public bool RequireElectronicSignature { get; set; }

        [Display(Name = "Signature Text")]
        public string SignatureText { get; set; }
    }
    public class ContractEmail
    {
        public List<SelectListVM> EmailMessageList { get; set; }

        [Required, Display(Name = "Email Message")]
        public int EmailMessageID { get; set; }
        public bool SendEmailOnContractAcceptance { get; set; }
        public bool IncludeContractAsPDF { get; set; }
    }
    public class ContractNotes
    {
        public string Notes { get; set; }
    }



    public class StudentConractsVM
    {
        public int Id { get; set; }
        public int PlacementId { get; set; }
        public int PersonId { get; set; }
        public int? LocationID { get; set; }
        public string PersonCode { get; set; }
        public string PersonFullName { get; set; }
        public string ContractName { get; set; }
        public int BookingId { get; set; }
        public int ContractId { get; set; }
        public string Content { get; set; }
        public decimal GrossAmount { get; set; }
        public Nullable<decimal> DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public Nullable<decimal> TaxAmount { get; set; }
        public Nullable<decimal> RegistrationFee { get; set; }
        public decimal SecurityDeposit { get; set; }
        public bool IsSigned { get; set; }
        public bool IsCancel { get; set; }
        public bool IsSignedTerms { get; set; }
        public bool IsSignedCodeOfConduct { get; set; }
        public bool IsPublish { get; set; }

        public string Signature { get; set; }
        public string ContractUrl { get; set; }
        public string contractkery { get; set; }
        public string CancellationReason { get; set; }
    }
    public class PreCheckInDocumentationVM
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public int PlacementId { get; set; }
        public int BookingId { get; set; }
        public string PersonCode { get; set; }
        public string PersonFullName { get; set; }
        public string DocumentationName { get; set; }
        public string DocumentationContent { get; set; }
        public string DocumentationKey { get; set; }
        public bool IsSigned { get; set; }
        public bool IsSignedTerms { get; set; }
        public bool IsSignedCodeOfConduct { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string DocumentationUrl { get; set; }
        public string StudentSignature { get; set; }
    }

    public class StudentConractsListVM
    {
        public int Id { get; set; }
        public int PlacementId { get; set; }
        public int PersonId { get; set; }
        public string PersonCode { get; set; }
        public string PersonFullName { get; set; }
        public string ContractName { get; set; }
        public int BookingId { get; set; }
        public int ContractId { get; set; }
        public string Content { get; set; }
        public string Occupancy { get; set; }
        public bool IsSigned { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string ContractKey { get; set; }
        public string ContractUrl { get; set; }
        public int? LocationId { get; set; }
        public DateTime? Movein { get; set; }
        public string MoveinFormatted => Movein?.ToString("d/M/yyyy");
        public DateTime? MoveOut { get; set; }
        public string MoveOutFormatted => MoveOut?.ToString("d/M/yyyy");
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public bool InHouse { get; set; }
        public bool CheckedOut { get; set; }
        public bool IsCancel { get; set; }
        public string CancellationReason { get; set; }
    }
}
