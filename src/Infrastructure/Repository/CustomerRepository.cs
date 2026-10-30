using Dapper;
using Domain.Entities;
using Domain.Repositories;
using System.Data;

namespace Infrastructure.Repository;

public class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnection _dbConnection;

    public CustomerRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<CustomerEntity?> Get(Guid id)
    {
        string sql = @"SELECT c.*, s.name AS series_name FROM customers c
                        JOIN series s ON s.id = c.series_id
                        WHERE c.id=@Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<CustomerEntity>(sql, new { id });
    }

    public async Task<IEnumerable<CustomerEntity>> GetBySellerId(Guid sellerId)
    {
        string sql = @"SELECT c.*, s.name AS series_name FROM customers c
                        JOIN series s ON s.id = c.series_id
                        WHERE c.seller_id=@SellerId";

        return await _dbConnection.QueryAsync<CustomerEntity>(sql, new { sellerId });
    }

    public async Task<IEnumerable<CustomerEntity>> Get()
    {
        string sql = @"SELECT c.*, s.name AS series_name FROM customers c
                        JOIN series s ON s.id = c.series_id";

        return await _dbConnection.QueryAsync<CustomerEntity>(sql);
    }

    public async Task<Guid> Add(CustomerEntity customer)
    {
        string sql = @"INSERT INTO customers
                        (seller_id, series_id, company_name, street, city, email, phone, state, company_number)
                        VALUES (@SellerId, @SeriesId, @CompanyName, @Street, @City, @Email, @Phone, @State, @CompanyNumber)
                        RETURNING id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, customer);
    }

    // Null when the customer doesn't exist, false when it has invoices and the series would change.
    public async Task<bool?> Update(CustomerEntity customer)
    {
        string sql = @"WITH updated AS (
                            UPDATE customers
                            SET series_id=@SeriesId, company_name=@CompanyName, street=@Street, city=@City, email=@Email,
                                phone=@Phone, state=@State, company_number=@CompanyNumber
                            WHERE id=@Id
                              AND (series_id=@SeriesId OR NOT EXISTS (SELECT 1 FROM invoices WHERE customer_id=@Id))
                            RETURNING id
                        )
                        SELECT EXISTS (SELECT 1 FROM updated) FROM customers
                        WHERE id=@Id";

        return await _dbConnection.ExecuteScalarAsync<bool?>(sql, customer);
    }

    public async Task Delete(Guid id)
    {
        string sql = @"DELETE FROM customers
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
