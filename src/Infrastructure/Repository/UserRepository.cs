using Dapper;
using Domain.Entities;
using Domain.Repositories;
using System.Data;

namespace Infrastructure.Repository;

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _dbConnection;

    public UserRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<UserEntity?> Get(Guid id)
    {
        string sql = @"SELECT * FROM users
                        WHERE id=@Id";

        return await _dbConnection.QuerySingleOrDefaultAsync<UserEntity>(sql, new { id });
    }

    public async Task<IEnumerable<UserEntity>> Get()
    {
        string sql = @"SELECT * FROM users";

        return await _dbConnection.QueryAsync<UserEntity>(sql);
    }

    public async Task<Guid> Add(UserEntity item)
    {
        string sql = @"INSERT INTO users
                        (name, last_name, email, password, user_type)
                        VALUES (@Name, @LastName, @Email, @Password, @UserType)
                        RETURNING id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, item);
    }

    public async Task Update(UserEntity item)
    {
        string sql = @"UPDATE users
                        SET name=@Name, last_name=@LastName, password=@Password, user_type=@UserType
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, item);
    }

    public async Task Delete(Guid id)
    {
        string sql = @"DELETE FROM users
                        WHERE id=@Id";

        await _dbConnection.ExecuteAsync(sql, new { id });
    }
}
