using Dapper;
using Domain.Entities;
using Domain.Helpers;
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

    public async Task<(IEnumerable<SellerEntity>, int)> Get(Guid? userId, int page, int pageSize)
    {
        string filter = @"WHERE (@UserId IS NULL OR user_id = @UserId)";

        string countSql = $"SELECT COUNT(*) FROM sellers {filter}";
        string dataSql = $@"SELECT * FROM sellers {filter}
                            ORDER BY company_name, id
                            LIMIT @PageSize OFFSET @Offset";

        int totalCount = await _dbConnection.ExecuteScalarAsync<int>(countSql, new { UserId = userId });

        page = Paging.PageInBounds(page, pageSize, totalCount);
        var parameters = new { UserId = userId, PageSize = pageSize, Offset = (page - 1) * pageSize };

        IEnumerable<SellerEntity> sellers = await _dbConnection.QueryAsync<SellerEntity>(dataSql, parameters);

        return (sellers, totalCount);
    }

    public async Task<Guid> Add(SellerEntity item)
    {
        string sql = @"INSERT INTO sellers
                        (user_id, company_name, street, city, email, phone, state, company_number, bank_name, bank_number)
                        VALUES (@UserId, @CompanyName, @Street, @City, @Email, @Phone, @State, @CompanyNumber, @BankName, @BankNumber)
                        RETURNING id";

        return await _dbConnection.ExecuteScalarAsync<Guid>(sql, item);
    }

    public async Task Update(SellerEntity item)
    {
        string sql = @"UPDATE sellers
                        SET company_name=@CompanyName, street=@Street, city=@City, email=@Email,
                            phone=@Phone, state=@State, company_number=@CompanyNumber, bank_name=@BankName, bank_number=@BankNumber
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
