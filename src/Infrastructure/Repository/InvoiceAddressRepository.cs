using Dapper;
using Domain.Entities;
using Domain.Repositories;
using System.Data;

namespace Infrastructure.Repository;

public class InvoiceAddressRepository : IInvoiceAddressRepository
{
    private readonly IDbConnection _dbConnection;

    public InvoiceAddressRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<InvoiceAddressEntity?> Get(Guid id)
    {
        string sql = @"SELECT * FROM invoice_addresses
                        WHERE id=@Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<InvoiceAddressEntity>(sql, new { id });
    }

    public async Task<IEnumerable<InvoiceAddressEntity>> GetByUser(Guid userId)
    {
        string sql = @"SELECT * FROM invoice_addresses
                        WHERE user_id=@UserId";

        return await _dbConnection.QueryAsync<InvoiceAddressEntity>(sql, new { userId });
    }

    public async Task<IEnumerable<InvoiceAddressEntity>> Get()
    {
        string sql = @"SELECT * FROM invoice_addresses";

        return await _dbConnection.QueryAsync<InvoiceAddressEntity>(sql);
    }

    public async Task<Guid> Add(InvoiceAddressEntity item)
    {
        string sql = @"INSERT INTO invoice_addresses
                        (user_id, company_name, street, city, email, phone, state)
                        VALUES (@UserId, @CompanyName, @Street, @City, @Email, @Phone, @State)
                        RETURNING id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, item);
    }

    public async Task Update(InvoiceAddressEntity item)
    {
        string sql = @"UPDATE invoice_addresses
                        SET company_name=@CompanyName, street=@Street, city=@City, email=@Email, phone=@Phone, state=@State
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, item);
    }

    public async Task Delete(Guid id)
    {
        string sql = @"DELETE FROM invoice_addresses
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
