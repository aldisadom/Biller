using Dapper;
using Domain.Entities;
using Domain.Repositories;
using System.Data;

namespace Infrastructure.Repository;

public class SellerRepository : ISellerRepository
{
    private readonly IDbConnection _dbConnection;

    public SellerRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<SellerEntity?> Get(Guid id)
    {
        string sql = @"SELECT * FROM sellers
                        WHERE id=@Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<SellerEntity>(sql, new { id });
    }

    public async Task<IEnumerable<SellerEntity>> GetByUser(Guid userId)
    {
        string sql = @"SELECT * FROM sellers
                        WHERE user_id=@UserId";

        return await _dbConnection.QueryAsync<SellerEntity>(sql, new { userId });
    }

    public async Task<IEnumerable<SellerEntity>> Get()
    {
        string sql = @"SELECT * FROM sellers";

        return await _dbConnection.QueryAsync<SellerEntity>(sql);
    }

    public async Task<Guid> Add(SellerEntity item)
    {
        string sql = @"INSERT INTO sellers
                        (user_id, company_name, street, city, email, phone, state)
                        VALUES (@UserId, @CompanyName, @Street, @City, @Email, @Phone, @State)
                        RETURNING id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, item);
    }

    public async Task Update(SellerEntity item)
    {
        string sql = @"UPDATE sellers
                        SET company_name=@CompanyName, street=@Street, city=@City, email=@Email,
                            phone=@Phone, state=@State
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, item);
    }

    public async Task Delete(Guid id)
    {
        string sql = @"DELETE FROM sellers
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
