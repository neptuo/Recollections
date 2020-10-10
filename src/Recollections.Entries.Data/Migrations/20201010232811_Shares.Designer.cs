﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Neptuo.Recollections.Entries;

namespace Neptuo.Recollections.Entries.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20201010232811_Shares")]
    partial class Shares
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.4");

            modelBuilder.Entity("Neptuo.Recollections.Entries.Entry", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("ChapterId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

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

                    b.Property<DateTime>("When")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("EntryId");

                    b.ToTable("Images");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.Share", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("EntryId")
                        .HasColumnType("TEXT");

                    b.Property<string>("StoryId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ProfileUserId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Permission")
                        .HasColumnType("INTEGER");

                    b.HasKey("UserId", "EntryId", "StoryId", "ProfileUserId");

                    b.ToTable("Shares");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.Story", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Created")
                        .HasColumnType("TEXT");

                    b.Property<int>("Order")
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

                            b1.ToTable("EntriesLocations");

                            b1.WithOwner()
                                .HasForeignKey("EntryId");
                        });
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
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.StoryChapter", b =>
                {
                    b.HasOne("Neptuo.Recollections.Entries.Story", "Story")
                        .WithMany("Chapters")
                        .HasForeignKey("StoryId");
                });
#pragma warning restore 612, 618
        }
    }
}
