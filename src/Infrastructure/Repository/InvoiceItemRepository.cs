using Dapper;
using Domain.Entities;
using Domain.Repositories;
using System.Data;

namespace Infrastructure.Repository;

public class InvoiceItemRepository : IInvoiceItemRepository
{
    private readonly IDbConnection _dbConnection;

    public InvoiceItemRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<InvoiceItemEntity?> Get(Guid id)
    {
        string sql = @"SELECT * FROM invoice_items
                        WHERE id=@Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<InvoiceItemEntity>(sql, new { id });
    }

    public async Task<IEnumerable<InvoiceItemEntity>> Get(List<Guid> ids)
    {
        string sql = @"SELECT * FROM invoice_items
                        WHERE id=ANY(@Ids)";

        return await _dbConnection.QueryAsync<InvoiceItemEntity>(sql, new { ids });
    }

    public async Task<IEnumerable<InvoiceItemEntity>> GetByAddressId(Guid addressId)
    {
        string sql = @"SELECT * FROM invoice_items
                        WHERE address_id=@AddressId";

        return await _dbConnection.QueryAsync<InvoiceItemEntity>(sql, new { addressId });
    }

    public async Task<IEnumerable<InvoiceItemEntity>> Get()
    {
        string sql = @"SELECT * FROM invoice_items";

        return await _dbConnection.QueryAsync<InvoiceItemEntity>(sql);
    }

    public async Task<Guid> Add(InvoiceItemEntity item)
    {
        string sql = @"INSERT INTO invoice_items
                        (name, price, address_id, quantity)
                        VALUES (@Name, @Price, @AddressId, @Quantity)
                        RETURNING id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, item);
    }

    public async Task Update(InvoiceItemEntity item)
    {
        string sql = @"UPDATE invoice_items
                        SET name=@Name, price=@Price, quantity=@Quantity
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, item);
    }

    public async Task Delete(Guid id)
    {
        string sql = @"DELETE FROM invoice_items
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
