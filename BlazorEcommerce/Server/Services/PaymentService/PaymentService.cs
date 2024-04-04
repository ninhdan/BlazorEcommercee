using Azure.Core;
using Stripe;
using Stripe.Checkout;

namespace BlazorEcommerce.Server.Services.PaymentService
{
    public class PaymentService : IPaymentService
    {
        private readonly IAuthService _authService;
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;

        const string secret = "whsec_415efc5724f4fb999600c01bce76ff63a88306abef306c626314f6b66bf4532e";

        public PaymentService(IAuthService authService, IOrderService orderService, ICartService cartService) 
        {
            StripeConfiguration.ApiKey = "sk_test_51OcT68AjULt95inBF4Vsm7Av8mB4KKT9lFE1ry98Q9gSDjbIG36qwPjxUCeXnohuU0HPf72R11d40W00C5BLJlRy00hUiJIPRF";

            _authService = authService;
            _orderService = orderService;
            _cartService = cartService;
        }


        public async Task<Session> CreateCheckoutSession()
        {
            var products = (await _cartService.GetDbCartProducts()).Data;
            var lineItems = new List<SessionLineItemOptions>();
            products.ForEach(product => lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmountDecimal = product.Price * 100,
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = product.Title,
                        Images = new List<string> { product.ImageUrl }
                    }
                },
                Quantity = product.Quantity
            }));

            var options = new SessionCreateOptions
            {
                CustomerEmail = _authService.GetUserEmail(),
                ShippingAddressCollection =
                    new SessionShippingAddressCollectionOptions
                    {
                        AllowedCountries = new List<string> { "US" }
                    },
                PaymentMethodTypes = new List<string>
                {
                    "card"
                },
                LineItems = lineItems,
                Mode = "payment",
                //SuccessUrl =  "https://localhost:7008/order-success",
                //CancelUrl = "https://localhost:7008/cart",
                SuccessUrl = GetSuccessUrl(),
                CancelUrl = GetCancelUrl()
            };

            var service = new SessionService();
            Session session = service.Create(options);
            return session;
        }

        private string GetSuccessUrl()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string baseUrl;

            // Xác định URL dựa trên môi trường
            if (environment == "Development")
            {
                baseUrl = "https://localhost:7008";
            }
            else
            {
                baseUrl = "https://ecommerce-donet.azurewebsites.net";
            }

            return $"{baseUrl}/order-success";
        }

        private string GetCancelUrl()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string baseUrl;

            // Xác định URL dựa trên môi trường
            if (environment == "Development")
            {
                baseUrl = "https://localhost:7008";
            }
            else
            {
                baseUrl = "https://ecommerce-donet.azurewebsites.net";
            }

            return $"{baseUrl}/cart";
        }

        public async Task<ServiceResponse<bool>> FulfillOrder(HttpRequest request)
        {
            var json = await new StreamReader(request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                        json,
                        request.Headers["Stripe-Signature"],
                        secret
                    );

                if (stripeEvent.Type == Events.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;
                    var user = await _authService.GetUserByEmail(session.CustomerEmail);
                    await _orderService.PlaceOrder(user.Id);
                }

                return new ServiceResponse<bool> { Data = true };
            }
            catch (StripeException e)
            {
                return new ServiceResponse<bool> { Data = false, Success = false, Message = e.Message };
            }
        }
    }
}
