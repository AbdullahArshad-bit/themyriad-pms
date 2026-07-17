using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.DTO.ViewModels.PersonManageViewModels;
using PMS.DTO.ViewModels.UserManageViewModels;
using PMS.EF;

namespace PMS.Services.Services.Person
{
    public interface IPersonService
    {
        List<EF.Person> GetPersonsExport();

        List<EF.Person> GetPersons();
        PersonResponse GetPersonsByPages(PersonBinding person, string searchValue, string start, string lenght, string QueryBY, string orderBy, string orderDir, List<string> selectedColumns);
        List<EF.Person> GetCheckInPersons();

        List<V_GetPersonsforDeposit> GetPersonsNotCheckedinYet();
        List<EF.V_GetPersonsforDeposit> GetPersonsNotCheckedinYet(int invoiceTypeId);

        List<EF.Person> GetPersonsReservedCurrentlyOrCheckedOut();

        List<EF.Person> GetPersonsReservedCurrently();
        EF.Person GetPersonById(int id);
        EF.Person AddPerson(AddPersonVM personVM, HttpPostedFileBase file);
        EF.Person AddImportPerson(AddPersonVM personVM, HttpPostedFileBase file);
        EF.Person AddPersonWithoutSaving(AddPersonVM personVM, HttpPostedFileBase file);
        EF.Person UpdatePerson(AddPersonVM personVM, HttpPostedFileBase file);
        bool DeletePerson(int id);

        List<AddPersonVM> UploadPersonByExcelFile(string filePath);

        List<PersonDocument> GetPersonDocuments();

        string GetMaxPersonCode(int Locationid);


        EF.Person AddPersonWithBooking(AddPersonVM personVM);
        Task<ImportPersonsWithBookingResultVM> ImportPersonsWithBookingAsync(List<AddPersonVM> personVMs);
        bool GenrateUser(int Id);
        AddUserVM CheckUserMaster(int id);
        bool ResendEmail(int Userid);
        InHousePortalCredentialsResultVM SendPortalCredentialsToInHouseResidents(int? locationId = null);
        List<AddPersonVM> GetReferrals(string referralcode);

        IQueryable<EF.Person> GetPersonQueryable();

    }
}
