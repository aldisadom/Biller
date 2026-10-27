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
        string sql = @"SELECT i.*, s.name AS series_name FROM invoices i
                        JOIN series s ON s.id = i.series_id
                        WHERE i.id=@Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<InvoiceEntity>(sql, new { id });
    }

    public async Task<(IEnumerable<InvoiceEntity>, int)> Get(Guid? userId, Guid? sellerId, Guid? customerId, int page, int pageSize)
    {
        string filter = @"WHERE (@UserId IS NULL OR i.user_id = @UserId)
                          AND (@SellerId IS NULL OR i.seller_id = @SellerId)
                          AND (@CustomerId IS NULL OR i.customer_id = @CustomerId)";

        string countSql = $"SELECT COUNT(*) FROM invoices i {filter}";
        string dataSql = $@"SELECT i.*, s.name AS series_name FROM invoices i
                            JOIN series s ON s.id = i.series_id
                            {filter}
                            ORDER BY i.created_date DESC
                            LIMIT @PageSize OFFSET @Offset";

        var parameters = new { UserId = userId, SellerId = sellerId, CustomerId = customerId, PageSize = pageSize, Offset = (page - 1) * pageSize };

        int totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql, parameters);
        IEnumerable<InvoiceEntity> items = await _dbConnection.QueryAsync<InvoiceEntity>(dataSql, parameters);

        return (items, totalCount);
    }

    public async Task<Guid> Add(InvoiceEntity invoice)
    {
        string sql = @"WITH reserved AS (
                            UPDATE series
                            SET next_number = next_number + 1
                            WHERE id = @SeriesId
                              AND next_number < 2147483647
                              AND EXISTS (SELECT 1 FROM customers WHERE id = @CustomerId AND series_id = @SeriesId)
                            RETURNING next_number - 1 AS invoice_number
                        )
                        INSERT INTO invoices
                        (customer_id, seller_id, user_id, series_id, invoice_number, user_data, created_date, due_date,
                        seller_data, customer_data, items_data, comments, total_price, status)
                        SELECT @CustomerId, @SellerId, @UserId, @SeriesId, reserved.invoice_number, @UserData, @CreatedDate, @DueDate,
                        @SellerData, @CustomerData, @ItemsData, @Comments, @TotalPrice, @Status
                        FROM reserved
                        RETURNING id";

        return await _dbConnection.InTransaction(async transaction =>
        {
            await _dbConnection.LockSeries(invoice.SeriesId, transaction);

            return await _dbConnection.ExecuteScalarAsync<Guid>(sql, invoice, transaction);
        });
    }

    public async Task Update(InvoiceEntity invoice)
    {
        string sql = @"UPDATE invoices
                        SET user_data=@UserData,
                        created_date=@CreatedDate, due_date=@DueDate, seller_data=@SellerData, customer_data=@CustomerData,
                        items_data=@ItemsData, comments=@Comments, total_price=@TotalPrice
                        WHERE id=@Id";

        await _dbConnection.InTransaction(async transaction =>
        {
            await LockSeriesOfInvoice(invoice.Id, transaction);
            await _dbConnection.ExecuteAsync(sql, invoice, transaction);
        });
    }

    public async Task UpdateStatus(Guid id, InvoiceStatus status)
    {
        string sql = @"UPDATE invoices
                        SET status=@status
                        WHERE id=@Id";

        await _dbConnection.InTransaction(async transaction =>
        {
            await LockSeriesOfInvoice(id, transaction);
            await _dbConnection.ExecuteAsync(sql, new { Id = id, Status = status }, transaction);
        });
    }

    // Gives the number back only when the invoice is the latest of its series. Invoices from before
    // migration 020 can have number 0, which is never given back: next_number stays >= 1.
    public async Task Delete(Guid id)
    {
        string sql = @"WITH deleted AS (
                            DELETE FROM invoices
                            WHERE id = @Id
                            RETURNING series_id, invoice_number
                        )
                        UPDATE series s
                        SET next_number = s.next_number - 1
                        FROM deleted d
                        WHERE s.id = d.series_id
                          AND s.next_number = d.invoice_number + 1
                          AND d.invoice_number >= 1";

        await _dbConnection.InTransaction(async transaction =>
        {
            await LockSeriesOfInvoice(id, transaction);
            await _dbConnection.ExecuteAsync(sql, new { id }, transaction);
        });
    }

    // An invoice's series_id never changes, so it can be read before the lock.
    private async Task LockSeriesOfInvoice(Guid invoiceId, IDbTransaction transaction)
    {
        string sql = @"SELECT series_id FROM invoices
                        WHERE id=@InvoiceId";

        Guid? seriesId = await _dbConnection.ExecuteScalarAsync<Guid?>(sql, new { invoiceId }, transaction);
        if (seriesId is not null)
            await _dbConnection.LockSeries(seriesId.Value, transaction);
    }
}
