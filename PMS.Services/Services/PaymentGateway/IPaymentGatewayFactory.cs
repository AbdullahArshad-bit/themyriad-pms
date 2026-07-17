using PMS.Common.Classes;

namespace PMS.Services.Services.PaymentGateway
{
    public interface IPaymentGatewayFactory
    {
        IPaymentGateway GetGateway(int locationId);
    }
}
