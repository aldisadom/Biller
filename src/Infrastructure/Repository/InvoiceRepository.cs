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

    // Invoice items show the name and price of the item version they name (BIL-85).
    private const string ItemsSql = @"SELECT ii.*, v.name, v.price FROM invoice_items ii
                        JOIN item_versions v ON v.item_id = ii.item_id AND v.version = ii.version
                        WHERE ii.invoice_id = ANY(@Ids)
                        ORDER BY ii.invoice_id, ii.line_no";

    public async Task<InvoiceEntity?> Get(Guid id)
    {
        string sql = @"SELECT i.*, s.name AS series_name FROM invoices i
                        JOIN series s ON s.id = i.series_id
                        WHERE i.id=@Id";

        InvoiceEntity? invoice = await _dbConnection.QuerySingleOrDefaultAsync<InvoiceEntity>(sql, new { id });
        if (invoice is null)
            return null;

        await LoadItems([invoice]);
        return invoice;
    }

    private async Task LoadItems(List<InvoiceEntity> invoices)
    {
        if (invoices.Count == 0)
            return;

        IEnumerable<InvoiceItemEntity> invoiceItems = await _dbConnection.QueryAsync<InvoiceItemEntity>(ItemsSql,
            new { Ids = invoices.Select(i => i.Id).ToList() });
        ILookup<Guid, InvoiceItemEntity> byInvoice = invoiceItems.ToLookup(ii => ii.InvoiceId);

        foreach (InvoiceEntity invoice in invoices)
            invoice.Items = byInvoice[invoice.Id].ToList();
    }

    // The invoice items as arrays for unnest(...) WITH ORDINALITY: line_no is the position in the list, from 1.
    private static object WithItems(InvoiceEntity invoice, object invoiceParameters)
    {
        var parameters = new DynamicParameters(invoiceParameters);
        parameters.Add("ItemIds", invoice.Items.Select(ii => ii.ItemId).ToArray());
        parameters.Add("Versions", invoice.Items.Select(ii => ii.Version).ToArray());
        parameters.Add("Quantities", invoice.Items.Select(ii => ii.Quantity).ToArray());
        parameters.Add("ItemComments", invoice.Items.Select(ii => ii.Comments).ToArray());
        return parameters;
    }

    public async Task<(IEnumerable<InvoiceEntity> Invoices, int TotalCount, int Page)> Get(Guid? userId, Guid? sellerId, Guid? customerId, int page, int pageSize)
    {
        string filter = @"WHERE (@UserId IS NULL OR i.user_id = @UserId)
                          AND (@SellerId IS NULL OR i.seller_id = @SellerId)
                          AND (@CustomerId IS NULL OR i.customer_id = @CustomerId)";

        string countSql = $"SELECT COUNT(*) FROM invoices i {filter}";
        string dataSql = $@"SELECT i.*, s.name AS series_name, COUNT(*) OVER() AS total_count FROM invoices i
                            JOIN series s ON s.id = i.series_id
                            {filter}
                            ORDER BY i.created_date DESC
                            LIMIT @PageSize OFFSET @Offset";

        var (invoiceEntities, totalCount, foundPage) = await _dbConnection.QueryPage<InvoiceEntity>(dataSql, countSql,
            new { UserId = userId, SellerId = sellerId, CustomerId = customerId }, page, pageSize);

        List<InvoiceEntity> invoices = invoiceEntities.ToList();
        await LoadItems(invoices);
        return (invoices, totalCount, foundPage);
    }

    // An invoice item without a version gets its item's latest version (BIL-85).
    public async Task<Guid> Add(InvoiceEntity invoice)
    {
        string sql = @"WITH reserved AS (
                            UPDATE series
                            SET next_number = next_number + 1
                            WHERE id = @SeriesId
                              AND next_number < 2147483647
                              AND EXISTS (SELECT 1 FROM customers WHERE id = @CustomerId AND series_id = @SeriesId)
                            RETURNING next_number - 1 AS invoice_number
                        ),
                        inv AS (
                            INSERT INTO invoices
                            (customer_id, seller_id, user_id, series_id, invoice_number, created_date, due_date,
                            comments, status)
                            SELECT @CustomerId, @SellerId, @UserId, @SeriesId, reserved.invoice_number, @CreatedDate, @DueDate,
                            @Comments, @Status
                            FROM reserved
                            RETURNING id
                        ),
                        items_added AS (
                            INSERT INTO invoice_items (invoice_id, line_no, item_id, version, quantity, comments)
                            SELECT inv.id, ii.ord, ii.item_id,
                                COALESCE(ii.version, (SELECT MAX(version) FROM item_versions WHERE item_id = ii.item_id)),
                                ii.quantity, ii.comments
                            FROM inv, unnest(@ItemIds::uuid[], @Versions::int[], @Quantities::numeric[], @ItemComments::text[])
                                WITH ORDINALITY AS ii(item_id, version, quantity, comments, ord)
                        )
                        SELECT id FROM inv";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, WithItems(invoice, new
        {
            invoice.CustomerId,
            invoice.SellerId,
            invoice.UserId,
            invoice.SeriesId,
            invoice.CreatedDate,
            invoice.DueDate,
            invoice.Comments,
            invoice.Status
        }));
    }

    // Invoice items 1..n are upserted and those above n deleted: the two touch different rows, so the
    // key (invoice_id, line_no) never sees an old and a new invoice item at once.
    public async Task Update(InvoiceEntity invoice)
    {
        string sql = @"WITH inv AS (
                            UPDATE invoices
                            SET created_date=@CreatedDate, due_date=@DueDate, comments=@Comments
                            WHERE id=@Id
                            RETURNING id
                        ),
                        upserted AS (
                            INSERT INTO invoice_items (invoice_id, line_no, item_id, version, quantity, comments)
                            SELECT inv.id, ii.ord, ii.item_id, ii.version, ii.quantity, ii.comments
                            FROM inv, unnest(@ItemIds::uuid[], @Versions::int[], @Quantities::numeric[], @ItemComments::text[])
                                WITH ORDINALITY AS ii(item_id, version, quantity, comments, ord)
                            ON CONFLICT (invoice_id, line_no) DO UPDATE
                            SET item_id=EXCLUDED.item_id, version=EXCLUDED.version, quantity=EXCLUDED.quantity, comments=EXCLUDED.comments
                        )
                        DELETE FROM invoice_items
                        WHERE invoice_id IN (SELECT id FROM inv)
                          AND line_no > cardinality(@ItemIds::uuid[])";

        await _dbConnection.ExecuteAsync(sql, WithItems(invoice, new
        {
            invoice.Id,
            invoice.CreatedDate,
            invoice.DueDate,
            invoice.Comments
        }));
    }

    public async Task UpdateStatus(Guid id, InvoiceStatus status)
    {
        string sql = @"UPDATE invoices
                        SET status=@status
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { Id = id, Status = status });
    }

    // Safe to repeat: it changes only Created and Send invoices, and nothing here sets them back.
    // The statuses are parameters, so renumbering the enum changes only C#.
    public async Task<int> MarkOverdue(DateTime today)
    {
        string sql = @"UPDATE invoices
                        SET status=@OverDue
                        WHERE status IN (@Created, @Send)
                          AND due_date < @Today";

        return await _dbConnection.ExecuteAsync(sql, new
        {
            Today = today,
            OverDue = InvoiceStatus.OverDue,
            Created = InvoiceStatus.Created,
            Send = InvoiceStatus.Send
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

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
