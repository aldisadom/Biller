using Dapper;
using Domain.Entities;
using Domain.Repositories;
using System.Data;

namespace Infrastructure.Repository;

public class SeriesRepository : ISeriesRepository
{
    private readonly IDbConnection _dbConnection;

    public SeriesRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<SeriesEntity?> Get(Guid id)
    {
        string sql = @"SELECT * FROM series
                        WHERE id=@Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<SeriesEntity>(sql, new { id });
    }

    public async Task<(IEnumerable<SeriesEntity>, int)> Get(Guid? sellerId, int page, int pageSize)
    {
        string filter = @"WHERE (@SellerId IS NULL OR seller_id = @SellerId)";

        string countSql = $"SELECT COUNT(*) FROM series {filter}";
        string dataSql = $@"SELECT * FROM series {filter}
                            ORDER BY name, id
                            LIMIT @PageSize OFFSET @Offset";

        var parameters = new { SellerId = sellerId, PageSize = pageSize, Offset = (page - 1) * pageSize };

        int totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql, parameters);
        IEnumerable<SeriesEntity> series = await _dbConnection.QueryAsync<SeriesEntity>(dataSql, parameters);

        return (series, totalCount);
    }

    public async Task<SeriesEntity?> GetByName(Guid sellerId, string name)
    {
        string sql = @"SELECT * FROM series
                        WHERE seller_id=@SellerId AND name=@Name";

        return await _dbConnection.QuerySingleOrDefaultAsync<SeriesEntity>(sql, new { sellerId, name });
    }

    public async Task<bool> IsInUse(Guid id)
    {
        string sql = @"SELECT EXISTS (SELECT 1 FROM customers WHERE series_id=@Id)
                        OR EXISTS (SELECT 1 FROM invoices WHERE series_id=@Id)";

        return await _dbConnection.ExecuteScalarAsync<bool>(sql, new { id });
    }

    public async Task<Guid> Add(SeriesEntity series)
    {
        string sql = @"INSERT INTO series
                        (seller_id, name, next_number)
                        VALUES (@SellerId, @Name, @NextNumber)
                        RETURNING id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, series);
    }

    // Updates only when NextNumber is above the highest invoice number and the name stays the same or
    // the series has no invoices. Returns null when the series doesn't exist.
    public async Task<(bool Updated, int MaxInvoiceNumber)?> Update(SeriesEntity series)
    {
        string sql = @"WITH existing AS (
                            SELECT COALESCE(MAX(invoice_number), 0) AS max_number, COUNT(*) > 0 AS has_invoices FROM invoices
                            WHERE series_id=@Id
                        ),
                        updated AS (
                            UPDATE series
                            SET name=@Name, next_number=@NextNumber
                            FROM existing
                            WHERE id=@Id
                              AND @NextNumber > existing.max_number
                              AND (name=@Name OR NOT existing.has_invoices)
                            RETURNING id
                        )
                        SELECT EXISTS (SELECT 1 FROM updated) AS updated, (SELECT max_number FROM existing) AS max_invoice_number
                        FROM series
                        WHERE id=@Id";

        SeriesUpdateResult? result = await _dbConnection.QuerySingleOrDefaultAsync<SeriesUpdateResult>(sql, series);

        return result is null ? null : (result.Updated, result.MaxInvoiceNumber);
    }

    private sealed class SeriesUpdateResult
    {
        public bool Updated { get; set; }
        public int MaxInvoiceNumber { get; set; }
    }

    public async Task Delete(Guid id)
    {
        string sql = @"DELETE FROM series
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
