namespace STHEnterprise.Api.Models.Cart
{
    public class RequestModels
    {
        public record AddCartItemRequest(int ProductId, int Quantity);
        public record UpdateCartItemRequest(int Quantity);

    }
}
