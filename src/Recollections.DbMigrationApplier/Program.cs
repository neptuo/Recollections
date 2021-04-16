using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Migrations;
using System;
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Pass one argument with connection string to database to migrate.");
                return;
            }

            string connectionString = args[0];

            Console.WriteLine("Creating contexts.");
            using var accounts = new AccountsDataContext(DbContextOptions<AccountsDataContext>(connectionString, "Accounts"), Schema<AccountsDataContext>("Accounts"));
            using var entries = new EntriesDataContext(DbContextOptions<EntriesDataContext>(connectionString, "Entries"), Schema<EntriesDataContext>("Entries"));

            Console.WriteLine("Migrating accounts db.");
            accounts.Database.Migrate();
            
            Console.WriteLine("Migrating entries db.");
            entries.Database.Migrate();

            Console.WriteLine("Done.");
        }

        private static DbContextOptions<T> DbContextOptions<T>(string connectionString, string schema)
            where T : DbContext
        {
            var builder = new DbContextOptionsBuilder<T>()
                .UseSqlServer(connectionString, sql =>
                {
                    if (!String.IsNullOrEmpty(schema))
                        sql.MigrationsHistoryTable("__EFMigrationsHistory", schema);
                });


            return builder.Options;
        }

        private static SchemaOptions<T> Schema<T>(string name)
            where T : DbContext
        {
            var schema = new SchemaOptions<T>() { Name = name };
            MigrationWithSchema.SetSchema(schema);
            return schema;
        }
}
}
