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
        public DbSet<UserNotificationSettings> NotificationSettings { get; set; }
        public DbSet<UserNotificationNewEntriesSettings> NotificationNewEntriesSettings { get; set; }
        public DbSet<UserNotificationNewEntriesDispatch> NotificationNewEntriesDispatches { get; set; }
        public DbSet<UserNotificationPushSubscription> PushSubscriptions { get; set; }

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

            modelBuilder.Entity<UserNotificationSettings>()
                .ToTable("UserNotificationSettings")
                .HasKey(p => p.UserId);

            modelBuilder.Entity<UserNotificationSettings>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<UserNotificationNewEntriesSettings>()
                .ToTable("UserNotificationNewEntriesSettings")
                .HasKey(p => p.UserId);

            modelBuilder.Entity<UserNotificationNewEntriesSettings>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<UserNotificationNewEntriesDispatch>()
                .ToTable("UserNotificationNewEntriesDispatches")
                .HasKey(p => p.Id);

            modelBuilder.Entity<UserNotificationNewEntriesDispatch>()
                .Property(p => p.UserId)
                .IsRequired();

            modelBuilder.Entity<UserNotificationNewEntriesDispatch>()
                .Property(p => p.EntryId)
                .IsRequired();

            modelBuilder.Entity<UserNotificationNewEntriesDispatch>()
                .HasIndex(p => new { p.UserId, p.EntryId })
                .IsUnique();

            modelBuilder.Entity<UserNotificationNewEntriesDispatch>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<UserNotificationPushSubscription>()
                .ToTable("PushSubscriptions")
                .HasKey(p => p.Id);

            modelBuilder.Entity<UserNotificationPushSubscription>()
                .Property(p => p.UserId)
                .IsRequired();

            modelBuilder.Entity<UserNotificationPushSubscription>()
                .Property(p => p.Endpoint)
                .IsRequired();

            modelBuilder.Entity<UserNotificationPushSubscription>()
                .Property(p => p.P256dh)
                .IsRequired();

            modelBuilder.Entity<UserNotificationPushSubscription>()
                .Property(p => p.Auth)
                .IsRequired();

            modelBuilder.Entity<UserNotificationPushSubscription>()
                .HasIndex(p => p.Endpoint)
                .IsUnique();

            modelBuilder.Entity<UserNotificationPushSubscription>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId);

            if (!String.IsNullOrEmpty(schema.Name))
            {
                modelBuilder.HasDefaultSchema(schema.Name);

                foreach (var entity in modelBuilder.Model.GetEntityTypes())
                    entity.SetSchema(schema.Name);
            }
        }
    }
}
