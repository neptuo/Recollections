using Microsoft.EntityFrameworkCore;
using System;
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Pass two arguments as two connection strings to two databases to migrate.");
                return;
            }

            string accountsConnectionString = args[0];
            string entriesConnectionString = args[1];

            Console.WriteLine("Creating contexts.");
            using var accounts = new AccountsDataContext(DbContextOptions<AccountsDataContext>(accountsConnectionString), Schema<AccountsDataContext>("Accounts"));
            using var entries = new EntriesDataContext(DbContextOptions<EntriesDataContext>(entriesConnectionString), Schema<EntriesDataContext>("Entries"));

            Console.WriteLine("Migrating accounts db.");
            accounts.Database.Migrate();
            
            Console.WriteLine("Migrating entries db.");
            entries.Database.Migrate();

            Console.WriteLine("Done.");
        }

        private static DbContextOptions<T> DbContextOptions<T>(string connectionString)
            where T : DbContext
        {
            return new DbContextOptionsBuilder<T>().UseSqlServer(connectionString).Options;
        }

        private static SchemaOptions<T> Schema<T>(string name)
            where T : DbContext
        {
            return new SchemaOptions<T>() { Name = name };
        }
}
}
