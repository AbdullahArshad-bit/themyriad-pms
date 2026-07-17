using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.DTO.ViewModels.TaxViewModels;


namespace PMS.Services.Services.Tax
{
    public interface ITaxService
    {
        List<TaxVM> GetTax();

        AddTaxVM GetTaxById(int id);

        bool AddTax(AddTaxVM model);

        bool UpdateTax(AddTaxVM model);

        bool DeleteTax(int id);
        List<EF.Tax> GetAll();
    }
}
