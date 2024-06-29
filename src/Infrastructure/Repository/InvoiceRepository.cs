using Dapper;
using Domain.Entities;
using Domain.Repositories;
using System.Data;

namespace Infrastructure.Repository;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly IDbConnection _dbConnection;

    public InvoiceRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<InvoiceDataEntity?> Get(Guid id)
    {
        string sql = @"SELECT * FROM invoices
                        WHERE id=@Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<InvoiceDataEntity>(sql, new { id });
    }

    public async Task<IEnumerable<InvoiceDataEntity>> GetByUserId(Guid userId)
    {
        string sql = @"SELECT * FROM invoices
                        WHERE user_id=@UserId";

        return await _dbConnection.QueryAsync<InvoiceDataEntity>(sql, new { userId });
    }

    public async Task<IEnumerable<InvoiceDataEntity>> GetBySellerId(Guid sellerId)
    {
        string sql = @"SELECT * FROM invoices
                        WHERE seller_id=@SellerId";

        return await _dbConnection.QueryAsync<InvoiceDataEntity>(sql, new { sellerId });
    }

    public async Task<IEnumerable<InvoiceDataEntity>> GetByCustomerId(Guid customerId)
    {
        string sql = @"SELECT * FROM invoices
                        WHERE customer_id=@CustomerId";

        return await _dbConnection.QueryAsync<InvoiceDataEntity>(sql, new { customerId });
    }

    public async Task<IEnumerable<InvoiceDataEntity>> Get()
    {
        string sql = @"SELECT * FROM invoices";

        return await _dbConnection.QueryAsync<InvoiceDataEntity>(sql);
    }

    public async Task<Guid> Add(InvoiceDataEntity invoice)
    {
        string sql = @"INSERT INTO invoices
                        (customer_id, seller_id, user_id, file_path, number, user_data, created_date, due_date,
                        seller_data, customer_data, items_data, comments, total_price)
                        VALUES (@CustomerId, @SellerId, @UserId, @FilePath, @Number, @UserData, @CreatedDate, @DueDate,
                        @SellerData, @CustomerData, @ItemsData, @Comments, @TotalPrice)
                        RETURNING id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, invoice);
    }

    public async Task Update(InvoiceDataEntity invoice)
    {
        string sql = @"UPDATE invoices
                        SET customer_id=@CustomerId, seller_id=@SellerId, file_path=@FilePath, number=@Number, user_data=@UserData,
                        created_date=@CreatedDate, due_date=@DueDate, seller_data=@SellerData, customer_data=@CustomerData,
                        items_data=@ItemsData, comments=@Comments, total_price=@TotalPrice
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, invoice);
    }

    public async Task Delete(Guid id)
    {
        string sql = @"DELETE FROM invoices
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
