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

    public async Task<IEnumerable<InvoiceItemEntity>> GetByUser(Guid clientId)
    {
        string sql = @"SELECT * FROM invoice_items
                        WHERE client_id=@ClientId";

        return await _dbConnection.QueryAsync<InvoiceItemEntity>(sql, new { clientId });
    }

    public async Task<IEnumerable<InvoiceItemEntity>> Get()
    {
        string sql = @"SELECT * FROM invoice_items";

        return await _dbConnection.QueryAsync<InvoiceItemEntity>(sql);
    }

    public async Task<Guid> Add(InvoiceItemEntity item)
    {
        string sql = @"INSERT INTO invoice_items
                        (name, price, client_id)
                        VALUES (@Name, @Price, @ClientId)
                        RETURNING id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, item);
    }

    public async Task Update(InvoiceItemEntity item)
    {
        string sql = @"UPDATE invoice_items
                        SET name=@Name, price=@Price
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