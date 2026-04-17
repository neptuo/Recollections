using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;
using System.Linq;

namespace Neptuo.Recollections
{
    internal static class DevelopmentDatabaseMigrator
    {
        private static readonly TimeSpan SqliteLockExpiration = TimeSpan.FromMinutes(5);

        public static void EnsureMigrated<TContext>(IServiceCollection services)
            where TContext : DbContext
        {
            try
            {
                using ServiceProvider serviceProvider = services.BuildServiceProvider();
                using IServiceScope scope = serviceProvider.CreateScope();
                TContext db = scope.ServiceProvider.GetRequiredService<TContext>();

                ClearStaleSqliteMigrationLock(db);

                if (db.Database.GetPendingMigrations().Any())
                    db.Database.Migrate();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void ClearStaleSqliteMigrationLock(DbContext db)
        {
            if (!db.Database.IsSqlite())
                return;

            var connection = db.Database.GetDbConnection();
            bool shouldClose = connection.State != ConnectionState.Open;
            if (shouldClose)
                connection.Open();

            try
            {
                using var tableCommand = connection.CreateCommand();
                tableCommand.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE name = '__EFMigrationsLock' AND type = 'table';";
                bool hasLockTable = Convert.ToInt32(tableCommand.ExecuteScalar()) > 0;
                if (!hasLockTable)
                    return;

                using var readCommand = connection.CreateCommand();
                readCommand.CommandText = "SELECT \"Timestamp\" FROM \"__EFMigrationsLock\" WHERE \"Id\" = 1 LIMIT 1;";
                object value = readCommand.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                    return;

                string text = value.ToString();
                if (!DateTimeOffset.TryParse(text, out DateTimeOffset timestamp))
                {
                    DeleteLockRow(connection, db, "timestamp could not be parsed");
                }
                else if ((DateTimeOffset.UtcNow - timestamp) > SqliteLockExpiration)
                {
                    DeleteLockRow(connection, db, $"lock is older than {SqliteLockExpiration.TotalMinutes:0} minutes");
                }
            }
            finally
            {
                if (shouldClose)
                    connection.Close();
            }
        }

        private static void DeleteLockRow(System.Data.Common.DbConnection connection, DbContext db, string reason)
        {
            using var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM \"__EFMigrationsLock\" WHERE \"Id\" = 1;";

            if (deleteCommand.ExecuteNonQuery() > 0)
                Console.WriteLine($"Removed stale EF Core SQLite migration lock for '{db.GetType().Name}' because the {reason}.");
        }
    }
}
