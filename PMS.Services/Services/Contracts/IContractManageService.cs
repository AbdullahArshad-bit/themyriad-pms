using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.ContractViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;

namespace PMS.Services.Services.Contracts
{
    public interface IContractManageService
    {
        #region Contract Types

        List<ContractTypesVM> GetContractTypes();
        ContractTypesVM GetContractTypeById(int id);
        ContractType AddContractType(ContractTypesVM model);
        ContractType UpdateContractType(ContractTypesVM model);
        bool DeleteContractType(int id);

        List<SelectListVM> GetContractTypesDD();

        #endregion

        #region Contracts

        List<ContractsListVM> GetContracts(string contractNumber = "");
        AddContractVM GetContractVMById(int id);
        EF.Contract GetContractById(int id);
        AddContractVM AddContract(AddContractVM model);
        AddContractVM UpdateContract(AddContractVM model);
        List<string> GetUrl();
        bool DeleteContract(int id);

        bool AddStudentContract(StudentConractsVM studentConractsVM,string SendEmailTo, int SenderId, out string message);


        StudentConractsVM GetStudentContractById(int id);
        List<StudentConractsListVM> GetStudentContracts(DateTime? FromDate, DateTime? ToDate);
        List<StudentConractsListVM> GetUnsignedContracts();
        List<StudentConractsListVM> GetSingleStudentContract(int personId);

        bool SignContractDocument(int id, string Signature, string signatureby);
        bool SignPrecheckinDocument(int id, string StudentSignature, string signatureby);
        bool MarkContractAsSigned(int id);
        List<SelectListVM> GetEmailMessagesDDList();
        StudentConractsVM GetContractDetailByPlacment(int PlacementId);
        bool AddPreCheckInDocumentation(PreCheckInDocumentationVM preCheckInDocumentationVM, out string message);
        PreCheckInDocumentationVM GetPreCheckInDocumentationById(int id);

        bool CancelContract(int id, string cancellationReason);
        //api
        ApiResponse<List<StudentConractsListVM>> GetAllById(int Id);
        StudentConractsListVM GetSingleStudentContractLatest(int personId);

        #endregion

        #region Contract Generation Helpers
        ContractTemplateCache GetCachedContractTemplate(int contractId);
        ContractPlacementData GetPlacementWithRelatedDataOptimized(int placementId);
        string ProcessContractContentOptimized(string template, ContractPlacementData pd, StudentConractsVM vm, string currentDate, string contractDueDate);
        #endregion
    }
}
