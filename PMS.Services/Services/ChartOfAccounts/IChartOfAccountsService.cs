using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.COAViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Repository.Repositories.Generic;

namespace PMS.Services.Services.ChartOfAccounts
{
    
    public interface IChartOfAccountsService 
    {
        List<COAVM> GetChartOfAccounts();
        List<COAVM> GetAccountsByServiceType(int serviceTypeId);
        AddCOAVM GetCOAById(int id);

        bool AddCOA(AddCOAVM model);

        bool UpdateCOA(AddCOAVM model);

        bool DeleteAccount(int id);

        List<COAVM> GetAssetAccounts();

        List<COAVM> GetReceivableAccounts();

        List<COAVM> GetPayableAccounts();
        List<COAVM> GetDiscountAccounts();
        List<COAVM> GetLiablitiesAccounts();
    }
}
