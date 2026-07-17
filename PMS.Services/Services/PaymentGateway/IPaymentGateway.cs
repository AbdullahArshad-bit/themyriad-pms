using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.DTO;
using PMS.DTO.ViewModels.BookingViewModels;
using PMS.DTO.ViewModels.NetIntViewModel;
using PMS.EF;

using static PMS.DTO.ViewModels.PaymentGatewayViewModel;

namespace PMS.Services.Services.PaymentGateway
{
    public interface IPaymentGateway
    {
        ApiResponse<string> GeneratePaymentLink(Invoicing invoicing, string responseUrl);

        ApiResponse<PaymentResponseVM> GetTransactionStatus(string reference);
    }
}
