using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories;

public interface ISellerRepository
{
    Task<SellerEntity?> Get(Guid id);
    Task<IEnumerable<SellerEntity>> GetByUser(Guid userId);
    Task<IEnumerable<SellerEntity>> Get();
    Task<Guid> Add(SellerEntity seller);
    Task Update(SellerEntity seller);
    Task Delete(Guid id);
}
