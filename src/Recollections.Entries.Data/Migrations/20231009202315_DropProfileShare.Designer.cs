﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Neptuo.Recollections.Entries;

#nullable disable

namespace Neptuo.Recollections.Entries.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20231009202315_DropProfileShare")]
    partial class DropProfileShare
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.10");

            modelBuilder.Entity("BeingEntry", b =>
                {
                    b.Property<string>("BeingsId")
                        .HasColumnType("TEXT");

                    b.Property<string>("EntriesId")
                        .HasColumnType("TEXT");

                    b.HasKey("BeingsId", "EntriesId");

                    b.HasIndex("EntriesId");

                    b.ToTable("BeingEntry");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.Being", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<string>("Icon")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsSharingInherited")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Text")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Beings");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.BeingShare", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("BeingId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Permission")
                        .HasColumnType("INTEGER");

                    b.HasKey("UserId", "BeingId");

                    b.ToTable("BeingShares");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.Entry", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("ChapterId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsSharingInherited")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StoryId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Text")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("When")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ChapterId");

                    b.HasIndex("StoryId");

                    b.ToTable("Entries");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.EntryShare", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("EntryId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Permission")
                        .HasColumnType("INTEGER");

                    b.HasKey("UserId", "EntryId");

                    b.ToTable("EntryShares");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.Image", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<string>("EntryId")
                        .HasColumnType("TEXT");

                    b.Property<string>("FileName")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<int>("OriginalHeight")
                        .HasColumnType("INTEGER");

                    b.Property<int>("OriginalWidth")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("When")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("EntryId");

                    b.ToTable("Images");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.Story", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsSharingInherited")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Text")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Stories");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.StoryChapter", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<int>("Order")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StoryId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Text")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("StoryId");

                    b.ToTable("StoryChapter");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.StoryShare", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("StoryId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Permission")
                        .HasColumnType("INTEGER");

                    b.HasKey("UserId", "StoryId");

                    b.ToTable("StoryShares");
                });

            modelBuilder.Entity("BeingEntry", b =>
                {
                    b.HasOne("Neptuo.Recollections.Entries.Being", null)
                        .WithMany()
                        .HasForeignKey("BeingsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Neptuo.Recollections.Entries.Entry", null)
                        .WithMany()
                        .HasForeignKey("EntriesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.Entry", b =>
                {
                    b.HasOne("Neptuo.Recollections.Entries.StoryChapter", "Chapter")
                        .WithMany()
                        .HasForeignKey("ChapterId");

                    b.HasOne("Neptuo.Recollections.Entries.Story", "Story")
                        .WithMany()
                        .HasForeignKey("StoryId");

                    b.OwnsMany("Neptuo.Recollections.Entries.OrderedLocation", "Locations", b1 =>
                        {
                            b1.Property<string>("EntryId")
                                .HasColumnType("TEXT");

                            b1.Property<int>("Order")
                                .HasColumnType("INTEGER");

                            b1.Property<double?>("Altitude")
                                .HasColumnType("REAL");

                            b1.Property<double?>("Latitude")
                                .HasColumnType("REAL");

                            b1.Property<double?>("Longitude")
                                .HasColumnType("REAL");

                            b1.HasKey("EntryId", "Order");

                            b1.ToTable("EntriesLocations", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("EntryId");
                        });

                    b.Navigation("Chapter");

                    b.Navigation("Locations");

                    b.Navigation("Story");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.Image", b =>
                {
                    b.HasOne("Neptuo.Recollections.Entries.Entry", "Entry")
                        .WithMany()
                        .HasForeignKey("EntryId");

                    b.OwnsOne("Neptuo.Recollections.Entries.ImageLocation", "Location", b1 =>
                        {
                            b1.Property<string>("ImageId")
                                .HasColumnType("TEXT");

                            b1.Property<double?>("Altitude")
                                .HasColumnType("REAL");

                            b1.Property<double?>("Latitude")
                                .HasColumnType("REAL");

                            b1.Property<double?>("Longitude")
                                .HasColumnType("REAL");

                            b1.HasKey("ImageId");

                            b1.ToTable("Images");

                            b1.WithOwner()
                                .HasForeignKey("ImageId");
                        });

                    b.Navigation("Entry");

                    b.Navigation("Location");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.StoryChapter", b =>
                {
                    b.HasOne("Neptuo.Recollections.Entries.Story", "Story")
                        .WithMany("Chapters")
                        .HasForeignKey("StoryId");

                    b.Navigation("Story");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.Story", b =>
                {
                    b.Navigation("Chapters");
                });
#pragma warning restore 612, 618
        }
    }
}