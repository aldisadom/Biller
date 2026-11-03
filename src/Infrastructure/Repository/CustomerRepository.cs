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

    public async Task<(IEnumerable<CustomerEntity>, int)> Get(Guid? sellerId, int page, int pageSize)
    {
        string filter = @"WHERE (@SellerId IS NULL OR c.seller_id = @SellerId)";

        string countSql = $"SELECT COUNT(*) FROM customers c {filter}";
        string dataSql = $@"SELECT c.*, s.name AS series_name FROM customers c
                            JOIN series s ON s.id = c.series_id
                            {filter}
                            ORDER BY c.company_name, c.id
                            LIMIT @PageSize OFFSET @Offset";

        var parameters = new { SellerId = sellerId, PageSize = pageSize, Offset = (page - 1) * pageSize };

        int totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql, parameters);
        IEnumerable<CustomerEntity> customers = await _dbConnection.QueryAsync<CustomerEntity>(dataSql, parameters);

        return (customers, totalCount);
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
