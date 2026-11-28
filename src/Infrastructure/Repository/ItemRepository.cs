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

    public async Task<ItemEntity?> Get(Guid id, int? version)
    {
        // The item with the name and price of one version: version, or the latest when version is null.
        // Both tables have an id column, so every column is qualified.
        string sql = @"SELECT i.id, i.customer_id, i.quantity, v.version, v.name, v.price
                        FROM items i
                        JOIN item_versions v ON v.item_id = i.id
                            AND v.version = COALESCE(@Version, (SELECT MAX(version) FROM item_versions WHERE item_id = i.id))
                        WHERE i.id=@Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<ItemEntity>(sql, new { id, version });
    }

    public async Task<(IEnumerable<ItemEntity> Items, int TotalCount, int Page)> Get(Guid? customerId, int page, int pageSize)
    {
        string filter = @"WHERE (@CustomerId IS NULL OR i.customer_id = @CustomerId)";

        string countSql = $"SELECT COUNT(*) FROM items i {filter}";
        string dataSql = $@"SELECT i.id, i.customer_id, i.quantity, v.version, v.name, v.price, COUNT(*) OVER() AS total_count
                            FROM items i
                            JOIN item_versions v ON v.item_id = i.id
                                AND v.version = (SELECT MAX(version) FROM item_versions WHERE item_id = i.id)
                            {filter}
                            ORDER BY v.name, i.id
                            LIMIT @PageSize OFFSET @Offset";

        return await _dbConnection.QueryPage<ItemEntity>(dataSql, countSql, new { CustomerId = customerId }, page, pageSize);
    }

    public async Task<IEnumerable<ItemVersionEntity>> GetVersions(Guid itemId)
    {
        // No join to items: a version exists only while its item does (ON DELETE CASCADE)
        string sql = @"SELECT * FROM item_versions
                        WHERE item_id=@ItemId
                        ORDER BY version";

        return await _dbConnection.QueryAsync<ItemVersionEntity>(sql, new { itemId });
    }

    public async Task<IEnumerable<Guid>> GetIdsForCustomer(List<Guid> ids, Guid customerId)
    {
        // Which of these ids are the customer's items
        string sql = @"SELECT id FROM items
                        WHERE id = ANY(@Ids) AND customer_id = @CustomerId";

        return await _dbConnection.QueryAsync<Guid>(sql, new { ids, customerId });
    }

    public async Task<Guid> Add(ItemEntity item)
    {
        // The item and its version 1 in one statement
        string sql = @"WITH new_item AS (
                            INSERT INTO items (customer_id, quantity)
                            VALUES (@CustomerId, @Quantity)
                            RETURNING id)
                        INSERT INTO item_versions (item_id, version, name, price)
                        SELECT id, 1, @Name, @Price FROM new_item
                        RETURNING item_id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, item);
    }

    public async Task Update(ItemEntity item)
    {
        // Sets the quantity, and adds a version only when the name or price differs from the latest one
        string sql = @"WITH updated AS (
                            UPDATE items SET quantity=@Quantity
                            WHERE id=@Id
                            RETURNING id),
                        latest AS (
                            SELECT version, name, price FROM item_versions
                            WHERE item_id=@Id
                            ORDER BY version DESC
                            LIMIT 1)
                        INSERT INTO item_versions (item_id, version, name, price)
                        SELECT updated.id, latest.version + 1, @Name, @Price
                        FROM updated, latest
                        WHERE (latest.name, latest.price) IS DISTINCT FROM (@Name::varchar, @Price::numeric)";

        await _dbConnection.ExecuteAsync(sql, item);
    }

    public async Task Delete(Guid id)
    {
        // invoice_items' foreign keys refuse an item that an invoice uses (400); its item_versions
        // rows go with it (ON DELETE CASCADE).
        string sql = @"DELETE FROM items
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
