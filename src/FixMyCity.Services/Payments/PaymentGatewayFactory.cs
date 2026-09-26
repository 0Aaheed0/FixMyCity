using System;
using System.Collections.Generic;
using System.Linq;

namespace FixMyCity.Services.Payments
{
    public interface IPaymentGatewayFactory
    {
        IPaymentGateway GetGateway(string paymentMethod);
        IEnumerable<string> GetSupportedMethods();
    }

    public class PaymentGatewayFactory : IPaymentGatewayFactory
    {
        private readonly IEnumerable<IPaymentGateway> _gateways;

        public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways)
        {
            _gateways = gateways;
        }

        public IPaymentGateway GetGateway(string paymentMethod)
        {
            var match = _gateways.FirstOrDefault(g => 
                g.GatewayName.Equals(paymentMethod, StringComparison.OrdinalIgnoreCase));

            if (match != null) return match;

            // Fallback to Mock if unspecified
            return _gateways.FirstOrDefault(g => g.GatewayName.Equals("Mock", StringComparison.OrdinalIgnoreCase))
                ?? _gateways.First();
        }

        public IEnumerable<string> GetSupportedMethods()
        {
            return _gateways.Select(g => g.GatewayName);
        }
    }
}
