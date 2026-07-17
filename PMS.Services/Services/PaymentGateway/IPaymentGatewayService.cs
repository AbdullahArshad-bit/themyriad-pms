using PMS.DTO;
using PMS.DTO.ViewModels.BookingViewModels;
using PMS.DTO.ViewModels.NetIntViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PMS.DTO.ViewModels.PaymentGatewayViewModel;
using TransactionResponse = PMS.DTO.ViewModels.PaymentGatewayViewModel.TransactionResponse;

namespace PMS.Services.Services.PaymentGateway
{
    public interface IPaymentGatewayService
    {
        ApiResponse<string> PayNow(int Id, string responseUrl, bool isStudentPortal = false);
        ApiResponse<PaymentResponseVM> PaymentResponse(string refrence, string paymentresponse, bool IsAllowAnonymyouse = false);
        ApiResponse<PayGatewayOutput> GetUserPayment(string reference);
        //Network International Gateway
        

    }
}
