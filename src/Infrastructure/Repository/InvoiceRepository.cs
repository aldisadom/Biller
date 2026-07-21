using BillerContracts.Enums;
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

    public async Task<(IEnumerable<InvoiceEntity>, int)> Get(Guid? userId, Guid? sellerId, Guid? customerId, int page, int pageSize)
    {
        string filter = @"WHERE (@UserId IS NULL OR user_id = @UserId)
                          AND (@SellerId IS NULL OR seller_id = @SellerId)
                          AND (@CustomerId IS NULL OR customer_id = @CustomerId)";

        string countSql = $"SELECT COUNT(*) FROM invoices {filter}";
        string dataSql = $@"SELECT * FROM invoices {filter}
                            ORDER BY created_date DESC
                            LIMIT @PageSize OFFSET @Offset";

        var parameters = new { UserId = userId, SellerId = sellerId, CustomerId = customerId, PageSize = pageSize, Offset = (page - 1) * pageSize };

        int totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql, parameters);
        IEnumerable<InvoiceEntity> items = await _dbConnection.QueryAsync<InvoiceEntity>(dataSql, parameters);

        return (items, totalCount);
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
