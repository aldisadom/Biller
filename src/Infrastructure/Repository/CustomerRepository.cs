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
        string sql = @"SELECT * FROM customers
                        WHERE id=@Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<CustomerEntity>(sql, new { id });
    }

    public async Task<IEnumerable<CustomerEntity>> GetBySeller(Guid sellerId)
    {
        string sql = @"SELECT * FROM customers
                        WHERE seller_id=@SellerId";

        return await _dbConnection.QueryAsync<CustomerEntity>(sql, new { sellerId });
    }

    public async Task<IEnumerable<CustomerEntity>> Get()
    {
        string sql = @"SELECT * FROM customers";

        return await _dbConnection.QueryAsync<CustomerEntity>(sql);
    }

    public async Task<Guid> Add(CustomerEntity item)
    {
        string sql = @"INSERT INTO customers
                        (user_id, company_name, street, city, email, phone, state, invoice_name)
                        VALUES (@UserId, @CompanyName, @Street, @City, @Email, @Phone, @State, @InvoiceName)
                        RETURNING id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, item);
    }

    public async Task Update(CustomerEntity item)
    {
        string sql = @"UPDATE customers
                        SET company_name=@CompanyName, street=@Street, city=@City, email=@Email,
                            phone=@Phone, state=@State, invoice_name=@InvoiceName
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, item);
    }

    public async Task Delete(Guid id)
    {
        string sql = @"DELETE FROM customers
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
