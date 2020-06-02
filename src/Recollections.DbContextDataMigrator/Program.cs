using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Entries;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private static Task CopyDbSetAsync<TContext, TEntity>(TContext sourceContext, TContext targetContext, Func<TContext, DbSet<TEntity>> dbSetGetter, Action<TEntity> entityHandler = null, params string[] includes)
            where TEntity : class
        {
            var source = dbSetGetter(sourceContext);
            var target = dbSetGetter(targetContext);
            return CopyDbSetAsync(source, target, entityHandler, includes);
        }

        private async static Task CopyDbSetAsync<T>(DbSet<T> source, DbSet<T> target, Action<T> entityHandler = null, params string[] includes)
            where T : class
        {
            IQueryable<T> query = source;
            foreach (string include in includes)
                query = query.Include(include);

            var entities = await query.ToListAsync();

            foreach (var entity in entities)
            {
                if (target.Contains(entity))
                    target.Update(entity);
                else
                    target.Add(entity);
            }

            if (entityHandler != null)
            {
                foreach (var entity in entities)
                    entityHandler(entity);
            }
        }

        private async static Task MigrateAccountsAsync(string sourceConnectionString, string targetConnectionString)
        {
            using (var source = new AccountsDataContext(new DbContextOptionsBuilder<AccountsDataContext>().UseSqlite(sourceConnectionString).Options, new SchemaOptions<AccountsDataContext>()))
            using (var target = new AccountsDataContext(new DbContextOptionsBuilder<AccountsDataContext>().UseSqlServer(targetConnectionString).Options, new SchemaOptions<AccountsDataContext>() { Name = "Accounts" }))
            {
                await CopyDbSetAsync(source, target, c => c.Users);
                await CopyDbSetAsync(source, target, c => c.UserClaims);
                await CopyDbSetAsync(source, target, c => c.UserLogins);
                await CopyDbSetAsync(source, target, c => c.UserTokens);
                await CopyDbSetAsync(source, target, c => c.Roles);
                await CopyDbSetAsync(source, target, c => c.RoleClaims);
                await CopyDbSetAsync(source, target, c => c.UserRoles);

                await target.SaveChangesAsync();
            }
        }

        private async static Task MigrateEntriesAsync(string sourceConnectionString, string targetConnectionString)
        {
            using (var source = new EntriesDataContext(new DbContextOptionsBuilder<EntriesDataContext>().UseSqlite(sourceConnectionString).Options, new SchemaOptions<EntriesDataContext>()))
            using (var target = new EntriesDataContext(new DbContextOptionsBuilder<EntriesDataContext>().UseSqlServer(targetConnectionString).Options, new SchemaOptions<EntriesDataContext>() { Name = "Entries" }))
            {
                await CopyDbSetAsync(source, target, c => c.Entries);
                await CopyDbSetAsync(source, target, c => c.Images, image => target.Entry(image.Location).State = EntityState.Added);
                await CopyDbSetAsync(source, target, c => c.Stories, story =>
                {
                    foreach (var chapter in story.Chapters)
                    {
                        if (target.Entry(chapter).State != EntityState.Unchanged && target.Entry(chapter).State != EntityState.Modified)
                            target.Entry(chapter).State = EntityState.Added;
                    }
                }, nameof(Story.Chapters));

                await target.SaveChangesAsync();
            }
        }
    }
}
