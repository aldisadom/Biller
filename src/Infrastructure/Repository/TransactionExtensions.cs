using Dapper;
using System.Data;
using System.Data.Common;

namespace Infrastructure.Repository;

internal static class TransactionExtensions
{
    // All repositories share one scoped connection, so a transaction must start and end inside one
    // repository method, never across repository calls.
    public static async Task<T> InTransaction<T>(this IDbConnection connection, Func<IDbTransaction, Task<T>> work)
    {
        DbConnection dbConnection = (DbConnection)connection;
        bool wasClosed = dbConnection.State == ConnectionState.Closed;
        if (wasClosed)
            await dbConnection.OpenAsync();

        try
        {
            await using DbTransaction transaction = await dbConnection.BeginTransactionAsync();
            T result = await work(transaction);
            await transaction.CommitAsync();

            return result;
        }
        finally
        {
            if (wasClosed)
                await dbConnection.CloseAsync();
        }
    }

    public static Task InTransaction(this IDbConnection connection, Func<IDbTransaction, Task> work)
    {
        return connection.InTransaction(async transaction =>
        {
            await work(transaction);
            return true;
        });
    }

    // Every invoice write and every series update takes this lock first, so they never run at the same time,
    // and statements after it see everything committed before it. A customer update that changes the series
    // takes it after locking the customer row. NO KEY UPDATE is the lock an UPDATE of the series takes,
    // so foreign-key checks from customers and invoices don't wait for it. A rename takes FOR UPDATE,
    // because the name is in a unique key, so those checks wait for it.
    // Returns false when the series doesn't exist.
    public static async Task<bool> LockSeries(this IDbConnection connection, Guid seriesId, IDbTransaction transaction)
    {
        string sql = @"SELECT id FROM series
                        WHERE id=@SeriesId
                        FOR NO KEY UPDATE";

        return await connection.ExecuteScalarAsync<Guid?>(sql, new { seriesId }, transaction) is not null;
    }
}
