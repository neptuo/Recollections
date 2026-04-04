using Microsoft.Extensions.Configuration;
using Neptuo.Recollections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore
{
    public static class EntityFrameworkCoreExtensions
    {
        public static void UseDbServer(this DbContextOptionsBuilder options, IConfiguration configuration, PathResolver pathResolver, string schema)
        {
            if (configuration.GetValue("Server", DbServer.Sqlite) == DbServer.SqlServer)
            {
                options.UseSqlServer(configuration.GetValue<string>("ConnectionString"), sql =>
                {
                    if (!String.IsNullOrEmpty(schema))
                        sql.MigrationsHistoryTable("__EFMigrationsHistory", schema);
                });
            }
            else
            {
                var connectionString = pathResolver(configuration.GetValue<string>("ConnectionString"));

                var dbPath = connectionString
                    .Split(';')
                    .Select(p => p.Trim())
                    .Where(p => p.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.Substring("Filename=".Length))
                    .FirstOrDefault();

                if (dbPath != null)
                {
                    var dir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
                    if (!String.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                }

                options.UseSqlite(connectionString);
            }
        }
    }
}
