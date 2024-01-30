
using BlazorEcommerce.Shared;
using Blazored.LocalStorage;

namespace BlazorEcommerce.Client.Services.CartService
{
    public class CartService : ICartService
    {
        public event Action OnChange;
        private readonly ILocalStorageService _localStorageService;
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public CartService(ILocalStorageService localStorageService, HttpClient httpClient, AuthenticationStateProvider authenticationStateProvider)
        {
            _localStorageService = localStorageService;
            _httpClient = httpClient;
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task AddToCart(CartItem cartItem)
        {
            if((await _authenticationStateProvider.GetAuthenticationStateAsync()).User.Identity.IsAuthenticated)
            {
                Console.WriteLine("user is authenticated");
            }


            var cart = await _localStorageService.GetItemAsync<List<CartItem>>("cart");
            if(cart == null)
            {
                cart = new List<CartItem>();
            }

            var sameItem = cart.Find(x => x.ProductId == cartItem.ProductId && x.ProductTypeId == cartItem.ProductTypeId);
            if(sameItem == null)
            {
                cart.Add(cartItem);
            }
            else
            {
                sameItem.Quantity += cartItem.Quantity;
            }

            await _localStorageService.SetItemAsync("cart", cart);
            OnChange.Invoke();
        }

        public async Task<List<CartItem>> GetCartItems()
        {
            var cart = await _localStorageService.GetItemAsync<List<CartItem>>("cart");
            if (cart == null)
            {
                cart = new List<CartItem>();
            }
            return cart;
        }

        public async Task<List<CartProductResponse>> GetCartProducts()
        {
            var cartItems = await _localStorageService.GetItemAsync<List<CartItem>>("cart");
            var response = await _httpClient.PostAsJsonAsync("api/cart/products", cartItems);
            var cartProducts = await response.Content.ReadFromJsonAsync<ServiceResponse<List<CartProductResponse>>>();
            return cartProducts.Data;
        }

        public async Task RemoveProductFromCart(int productId, int productTypeId)
        {
            var cart = await _localStorageService.GetItemAsync<List<CartItem>>("cart");
            if(cart == null)
            {
                return;
            }

            var cartItem = cart.Find(x => x.ProductId == productId
                            && x.ProductTypeId == productTypeId);
            
            if(cartItem != null)
            {
                cart.Remove(cartItem);
                await _localStorageService.SetItemAsync("cart", cart);
                OnChange.Invoke();
            }
        }

        public async Task UpdateQuantity(CartProductResponse product)
        {
            var cart = await _localStorageService.GetItemAsync<List<CartItem>>("cart");
            if (cart == null)
            {
                return;
            }

            var cartItem = cart.Find(x => x.ProductId == product.ProductId
                            && x.ProductTypeId == product.ProductTypeId);

            if (cartItem != null)
            {
                cartItem.Quantity = product.Quantity;
                await _localStorageService.SetItemAsync("cart", cart);
            }
        }
    }
}
