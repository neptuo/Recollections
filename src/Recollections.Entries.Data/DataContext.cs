using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class DataContext : DbContext
    {
        public DbSet<Entry> Entries { get; set; }
        public DbSet<Image> Images { get; set; }

        public DbSet<Story> Stories { get; set; }

        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Entry>().OwnsMany(e => e.Locations, e =>
            {
                e.ToTable("EntriesLocations");
                e.Property("EntryId");
                e.Property("Order").ValueGeneratedNever(); // Based on https://github.com/dotnet/efcore/issues/11162.
                e.HasKey("EntryId", nameof(OrderedLocation.Order));
            });
        }
    }
}
