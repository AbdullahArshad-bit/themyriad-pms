using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.PaymentViewModels;


namespace PMS.Services.Services.PaymentTypes
{
     public interface IPaymentTypesService

    {
        //Payment
        List<PaymentListVM> GetPayment();

        AddPaymentTypeVM GetPaymentById(int id);

        bool AddPayment(AddPaymentTypeVM model);

        bool UpdatePayment(AddPaymentTypeVM model);

        bool DeletePayment(int id);
        List<PaymentListVM> GetKeyCodePayment();
        OutputInvoicingVM GetInvoiceCode(int? id);
    }
}
