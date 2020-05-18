using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string accountsSource = "";
            string accountsTarget = "";
            string entriesSource = "";
            string entriesTarget = "";

            await MigrateAccountsAsync(accountsSource, accountsTarget);
            await MigrateEntriesAsync(entriesSource, entriesTarget);
        }

        private static Task CopyDbSetAsync<TContext, TEntity>(TContext sourceContext, TContext targetContext, Func<TContext, DbSet<TEntity>> dbSetGetter, Action<TEntity> entityHandler = null)
            where TEntity : class
        {
            var source = dbSetGetter(sourceContext);
            var target = dbSetGetter(targetContext);
            return CopyDbSetAsync(source, target, entityHandler);
        }

        private async static Task CopyDbSetAsync<T>(DbSet<T> source, DbSet<T> target, Action<T> entityHandler = null)
            where T : class
        {
            var entities = await source.ToListAsync();
            await target.AddRangeAsync(entities);

            if (entityHandler != null)
            {
                foreach (var entity in entities)
                    entityHandler(entity);
            }
        }

        private async static Task MigrateAccountsAsync(string sourceConnectionString, string targetConnectionString)
        {
            using (var source = new AccountsDataContext(new DbContextOptionsBuilder<AccountsDataContext>().UseSqlite(sourceConnectionString).Options))
            using (var target = new AccountsDataContext(new DbContextOptionsBuilder<AccountsDataContext>().UseSqlServer(targetConnectionString).Options))
            {
                await CopyDbSetAsync(source, target, c => c.Users);
                await CopyDbSetAsync(source, target, c => c.UserClaims);
                await CopyDbSetAsync(source, target, c => c.UserLogins);
                await CopyDbSetAsync(source, target, c => c.UserTokens);
                await CopyDbSetAsync(source, target, c => c.Roles);
                await CopyDbSetAsync(source, target, c => c.RoleClaims);
                await CopyDbSetAsync(source, target, c => c.UserRoles);
            }
        }

        private async static Task MigrateEntriesAsync(string sourceConnectionString, string targetConnectionString)
        {
            using (var source = new EntriesDataContext(new DbContextOptionsBuilder<EntriesDataContext>().UseSqlite(sourceConnectionString).Options))
            using (var target = new EntriesDataContext(new DbContextOptionsBuilder<EntriesDataContext>().UseSqlServer(targetConnectionString).Options))
            {
                await CopyDbSetAsync(source, target, c => c.Entries);
                await CopyDbSetAsync(source, target, c => c.Images, image => target.Entry(image.Location).State = EntityState.Modified);
                await CopyDbSetAsync(source, target, c => c.Stories);

                await target.SaveChangesAsync();
            }
        }
    }
}
