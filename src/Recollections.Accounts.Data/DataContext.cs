using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class DataContext : IdentityDbContext<User>
    {
        private readonly SchemaOptions schema;

        public DbSet<UserPropertyValue> UserProperties { get; set; }
        public DbSet<UserConnection> Connections { get; set; }

        public DataContext(DbContextOptions<DataContext> options, SchemaOptions<DataContext> schema)
            : base(options)
        {
            Ensure.NotNull(schema, "schema");
            this.schema = schema;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserPropertyValue>()
                .HasKey(p => new { p.UserId, p.Key });

            modelBuilder.Entity<UserPropertyValue>()
                .HasOne(p => p.User)
                .WithMany(u => u.Properties)
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<UserConnection>()
                .ToTable("UserConnections")
                .HasKey(p => new { p.UserId, p.OtherUserId });

            modelBuilder.Entity<UserConnection>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<UserConnection>()
                .HasOne(p => p.OtherUser)
                .WithMany()
                .HasForeignKey(p => p.OtherUserId);

            if (!String.IsNullOrEmpty(schema.Name))
            {
                modelBuilder.HasDefaultSchema(schema.Name);

                foreach (var entity in modelBuilder.Model.GetEntityTypes())
                    entity.SetSchema(schema.Name);
            }
        }
    }
}
