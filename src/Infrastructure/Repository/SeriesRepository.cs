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
                            ORDER BY name
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

    // Locks the series row, which every invoice write locks first too, so no invoice of the series is
    // created, changed or deleted until the update is done. Updates only when NextNumber is above the
    // highest invoice number and the name stays the same or the series has no invoices.
    // Returns null when the series doesn't exist.
    public async Task<(bool Updated, int MaxInvoiceNumber)?> Update(SeriesEntity series)
    {
        string maxInvoiceNumberSql = @"SELECT COALESCE(MAX(invoice_number), 0) FROM invoices
                                        WHERE series_id=@Id";
        string updateSql = @"UPDATE series
                            SET name=@Name, next_number=@NextNumber
                            WHERE id=@Id
                              AND @NextNumber > @MaxInvoiceNumber
                              AND (name=@Name OR NOT EXISTS (SELECT 1 FROM invoices WHERE series_id=@Id))";

        return await _dbConnection.InTransaction<(bool, int)?>(async transaction =>
        {
            if (!await _dbConnection.LockSeries(series.Id, transaction))
                return null;

            int maxInvoiceNumber = await _dbConnection.ExecuteScalarAsync<int>(maxInvoiceNumberSql, new { series.Id }, transaction);

            var parameters = new { series.Id, series.Name, series.NextNumber, MaxInvoiceNumber = maxInvoiceNumber };
            bool updated = await _dbConnection.ExecuteAsync(updateSql, parameters, transaction) == 1;

            return (updated, maxInvoiceNumber);
        });
    }

    public async Task Delete(Guid id)
    {
        string sql = @"DELETE FROM series
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
