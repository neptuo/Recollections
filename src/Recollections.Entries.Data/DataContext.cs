using Microsoft.EntityFrameworkCore;
using Neptuo;
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
        private readonly SchemaOptions schema;

        public DbSet<Entry> Entries { get; set; }
        public DbSet<EntryShare> EntryShares { get; set; }

        public DbSet<Image> Images { get; set; }

        public DbSet<Video> Videos { get; set; }

        public DbSet<Story> Stories { get; set; }
        public DbSet<StoryShare> StoryShares { get; set; }

        public DbSet<Being> Beings { get; set; }
        public DbSet<BeingShare> BeingShares { get; set; }

        public DataContext(DbContextOptions<DataContext> options, SchemaOptions<DataContext> schema)
            : base(options)
        {
            Ensure.NotNull(schema, "schema");
            this.schema = schema;
        }

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

            modelBuilder.Entity<EntryShare>()
                .HasKey(s => new { s.UserId, s.EntryId });

            modelBuilder.Entity<StoryShare>()
                .HasKey(s => new { s.UserId, s.StoryId });

            modelBuilder.Entity<BeingShare>()
                .HasKey(s => new { s.UserId, s.BeingId });

            if (!String.IsNullOrEmpty(schema.Name))
            {
                modelBuilder.HasDefaultSchema(schema.Name);

                foreach (var entity in modelBuilder.Model.GetEntityTypes())
                    entity.SetSchema(schema.Name);
            }
        }
    }
}
