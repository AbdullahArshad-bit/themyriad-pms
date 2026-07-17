using PMS.Common.Classes;
using System;

namespace PMS.Services.Services.PaymentGateway
{
    public class PaymentGatewayFactory : IPaymentGatewayFactory
    {
        private readonly ThwaniPaymentGateway _thwaniGateway;
        private readonly NetIntPaymentGateway _netIntGateway;

        public PaymentGatewayFactory(ThwaniPaymentGateway thwaniGateway, NetIntPaymentGateway netIntGateway)
        {
            _thwaniGateway = thwaniGateway;
            _netIntGateway = netIntGateway;
        }

        public IPaymentGateway GetGateway(int locationId)
        {
            // LocationEnum: Muscat = 16, Dubai = 17
            if (locationId == (int)Enumeration.LocationEnum.Muscat)
            {
                return _thwaniGateway;
            }
            else if (locationId == (int)Enumeration.LocationEnum.Dubai)
            {
                return _netIntGateway;
            }

            throw new Exception("Payment gateway not configured for this location.");
        }
    }
}
