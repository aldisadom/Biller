using Contracts.Requests.Seller;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Seller;

/// <summary>
/// example
/// </summary>
public class SellerAddRequestExample : IExamplesProvider<SellerAddRequest>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public SellerAddRequest GetExamples()
    {
        return new SellerAddRequest()
        {
            UserId = Guid.NewGuid(),
            CompanyName = "Glass Garden",
            CompanyNumber = "LL48662",
            Street = "Main road 5",
            City = "Winterfell",
            State = "Wolfswood",
            Email = "MainKeep@winter.com",
            Phone = "+123450679",
            BankName = "AB SEB Bankas",
            BankNumber = "LT08704444444444444",
        };
    }
}
