using Dapper;
using Domain.Entities;
using Domain.Repositories;
using System.Data;

namespace Infrastructure.Repository;

public class InvoiceClientRepository : IInvoiceClientRepository
{
    private readonly IDbConnection _dbConnection;

    public InvoiceClientRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<InvoiceClientEntity?> Get(Guid id)
    {
        string sql = @"SELECT * FROM invoice_clients
                        WHERE id=@Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<InvoiceClientEntity>(sql, new { id });
    }

    public async Task<IEnumerable<InvoiceClientEntity>> GetByUser(Guid userId)
    {
        string sql = @"SELECT * FROM invoice_clients
                        WHERE id=@userId";

        return await _dbConnection.QueryAsync<InvoiceClientEntity>(sql, new { userId });
    }

    public async Task<IEnumerable<InvoiceClientEntity>> Get()
    {
        string sql = @"SELECT * FROM invoice_clients";

        return await _dbConnection.QueryAsync<InvoiceClientEntity>(sql);
    }

    public async Task<Guid> Add(InvoiceClientEntity item)
    {
        string sql = @"INSERT INTO invoice_clients
                        (user_id, company_name, street, city, email, phone)
                        VALUES (@UserId, @CompanyName, @Street, @City, @Email, @Phone)
                        RETURNING id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, item);
    }

    public async Task Update(InvoiceClientEntity item)
    {
        string sql = @"UPDATE invoice_clients
                        SET company_name=@CompanyName, street=@Street, city=@City, email=@Email, phone=@Phone
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, item);
    }

    public async Task Delete(Guid id)
    {
        string sql = @"DELETE FROM invoice_clients
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
