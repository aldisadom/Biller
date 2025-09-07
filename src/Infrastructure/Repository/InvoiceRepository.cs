using Common.Enums;
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

    public async Task<InvoiceEntity?> Get(Guid id)
    {
        string sql = @"SELECT * FROM invoices
                        WHERE id=@Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<InvoiceEntity>(sql, new { id });
    }

    public async Task<IEnumerable<InvoiceEntity>> GetByUserId(Guid userId)
    {
        string sql = @"SELECT * FROM invoices
                        WHERE user_id=@UserId";

        return await _dbConnection.QueryAsync<InvoiceEntity>(sql, new { userId });
    }

    public async Task<IEnumerable<InvoiceEntity>> GetBySellerId(Guid sellerId)
    {
        string sql = @"SELECT * FROM invoices
                        WHERE seller_id=@SellerId";

        return await _dbConnection.QueryAsync<InvoiceEntity>(sql, new { sellerId });
    }

    public async Task<IEnumerable<InvoiceEntity>> GetByCustomerId(Guid customerId)
    {
        string sql = @"SELECT * FROM invoices
                        WHERE customer_id=@CustomerId";

        return await _dbConnection.QueryAsync<InvoiceEntity>(sql, new { customerId });
    }

    public async Task<IEnumerable<InvoiceEntity>> Get()
    {
        string sql = @"SELECT * FROM invoices";

        return await _dbConnection.QueryAsync<InvoiceEntity>(sql);
    }

    public async Task<Guid> Add(InvoiceEntity invoice)
    {
        string sql = @"INSERT INTO invoices
                        (customer_id, seller_id, user_id, file_path, invoice_number, user_data, created_date, due_date,
                        seller_data, customer_data, items_data, comments, total_price, status)
                        VALUES (@CustomerId, @SellerId, @UserId, @FilePath, @InvoiceNumber, @UserData, @CreatedDate, @DueDate,
                        @SellerData, @CustomerData, @ItemsData, @Comments, @TotalPrice, @Status)
                        RETURNING id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, invoice);
    }

    public async Task Update(InvoiceEntity invoice)
    {
        string sql = @"UPDATE invoices
                        SET customer_id=@CustomerId, seller_id=@SellerId, file_path=@FilePath, invoice_number=@InvoiceNumber, user_data=@UserData,
                        created_date=@CreatedDate, due_date=@DueDate, seller_data=@SellerData, customer_data=@CustomerData,
                        items_data=@ItemsData, comments=@Comments, total_price=@TotalPrice
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, invoice);
    }

    public async Task UpdateStatus(Guid id, InvoiceStatus status)
    {
        string sql = @"UPDATE invoices
                        SET status=@status
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { Id = id, Status = status });
    }

    public async Task Delete(Guid id)
    {
        string sql = @"DELETE FROM invoices
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
