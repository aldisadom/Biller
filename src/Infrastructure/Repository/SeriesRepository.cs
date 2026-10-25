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

    public async Task<int> GetMaxInvoiceNumber(Guid id)
    {
        string sql = @"SELECT COALESCE(MAX(invoice_number), 0) FROM invoices
                        WHERE series_id=@Id";

        return await _dbConnection.ExecuteScalarAsync<int>(sql, new { id });
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

    // Returns false when the series is gone, when an invoice already has NextNumber or higher,
    // or when next_number is no longer expectedNextNumber (an invoice was added or the latest
    // one deleted since the caller read the series).
    public async Task<bool> Update(SeriesEntity series, int expectedNextNumber)
    {
        string sql = @"UPDATE series
                        SET name=@Name, next_number=@NextNumber
                        WHERE id=@Id
                          AND next_number=@ExpectedNextNumber
                          AND @NextNumber > (SELECT COALESCE(MAX(invoice_number), 0) FROM invoices WHERE series_id=@Id)";

        var parameters = new { series.Id, series.Name, series.NextNumber, ExpectedNextNumber = expectedNextNumber };

        return await _dbConnection.ExecuteAsync(sql, parameters) == 1;
    }

    public async Task Delete(Guid id)
    {
        string sql = @"DELETE FROM series
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
