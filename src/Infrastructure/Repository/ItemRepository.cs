using Dapper;
using Domain.Entities;
using Domain.Repositories;
using System.Data;

namespace Infrastructure.Repository;

public class ItemRepository : IItemRepository
{
    private readonly IDbConnection _dbConnection;

    public ItemRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<ItemEntity?> Get(Guid id)
    {
        string sql = @"SELECT * FROM items
                        WHERE id=@Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<ItemEntity>(sql, new { id });
    }

    public async Task<IEnumerable<ItemEntity>> Get(List<Guid> ids)
    {
        string sql = @"SELECT * FROM items
                        WHERE id=ANY(@Ids)";

        return await _dbConnection.QueryAsync<ItemEntity>(sql, new { ids });
    }

    public async Task<(IEnumerable<ItemEntity> Items, int TotalCount, int Page)> Get(Guid? customerId, int page, int pageSize)
    {
        string filter = @"WHERE (@CustomerId IS NULL OR customer_id = @CustomerId)";

        string countSql = $"SELECT COUNT(*) FROM items {filter}";
        string dataSql = $@"SELECT *, COUNT(*) OVER() AS total_count FROM items {filter}
                            ORDER BY name, id
                            LIMIT @PageSize OFFSET @Offset";

        return await _dbConnection.QueryPage<ItemEntity>(dataSql, countSql, new { CustomerId = customerId }, page, pageSize);
    }

    public async Task<Guid> Add(ItemEntity item)
    {
        string sql = @"INSERT INTO items
                        (name, price, customer_id, quantity)
                        VALUES (@Name, @Price, @CustomerId, @Quantity)
                        RETURNING id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, item);
    }

    public async Task Update(ItemEntity item)
    {
        string sql = @"UPDATE items
                        SET name=@Name, price=@Price, quantity=@Quantity
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, item);
    }

    public async Task Delete(Guid id)
    {
        string sql = @"DELETE FROM items
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
