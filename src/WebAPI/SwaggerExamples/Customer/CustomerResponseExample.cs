﻿using Contracts.Responses.Customer;
using Swashbuckle.AspNetCore.Filters;

namespace WebAPI.SwaggerExamples.Customer;

/// <summary>
/// example
/// </summary>
public class CustomerResponseExample : IExamplesProvider<CustomerResponse>
{
    /// <summary>
    /// example
    /// </summary>
    /// <returns></returns>
    public CustomerResponse GetExamples()
    {
        return new CustomerResponse()
        {
            Id = Guid.NewGuid(),
            SellerId = Guid.NewGuid(),
            InvoiceName = "TT",
            CompanyName = "Throne Takers",
            CompanyNumber = "TT6663325",
            Street = "Ocean road 1",
            City = "Casterly Rock",
            State = "Westerlands",
            Email = "IronThrone@backstab.com",
            Phone = "+9623330679"
        };
    }
}
