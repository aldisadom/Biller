namespace Contracts.Responses.Seller;

public record SellerListResponse
{
    public List<SellerResponse> Sellers { get; set; } = [];
}
