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
    [Migration("20191125145135_Stories")]
    partial class Stories
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity("Neptuo.Recollections.Entries.Entry", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ChapterId");

                    b.Property<DateTime>("Created");

                    b.Property<string>("StoryId");

                    b.Property<string>("Text");

                    b.Property<string>("Title");

                    b.Property<string>("UserId");

                    b.Property<DateTime>("When");

                    b.HasKey("Id");

                    b.HasIndex("ChapterId");

                    b.HasIndex("StoryId");

                    b.ToTable("Entries");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.Image", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Created");

                    b.Property<string>("Description");

                    b.Property<string>("EntryId");

                    b.Property<string>("FileName");

                    b.Property<string>("Name");

                    b.Property<DateTime>("When");

                    b.HasKey("Id");

                    b.HasIndex("EntryId");

                    b.ToTable("Images");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.Story", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Created");

                    b.Property<int>("Order");

                    b.Property<string>("Text");

                    b.Property<string>("Title");

                    b.Property<string>("UserId");

                    b.HasKey("Id");

                    b.ToTable("Stories");
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.StoryChapter", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Created");

                    b.Property<int>("Order");

                    b.Property<string>("StoryId");

                    b.Property<string>("Text");

                    b.Property<string>("Title");

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
                            b1.Property<string>("EntryId");

                            b1.Property<int>("Order");

                            b1.Property<double?>("Altitude");

                            b1.Property<double?>("Latitude");

                            b1.Property<double?>("Longitude");

                            b1.HasKey("EntryId", "Order");

                            b1.ToTable("EntriesLocations");

                            b1.HasOne("Neptuo.Recollections.Entries.Entry")
                                .WithMany("Locations")
                                .HasForeignKey("EntryId")
                                .OnDelete(DeleteBehavior.Cascade);
                        });
                });

            modelBuilder.Entity("Neptuo.Recollections.Entries.Image", b =>
                {
                    b.HasOne("Neptuo.Recollections.Entries.Entry", "Entry")
                        .WithMany()
                        .HasForeignKey("EntryId");

                    b.OwnsOne("Neptuo.Recollections.Entries.Location", "Location", b1 =>
                        {
                            b1.Property<string>("ImageId");

                            b1.Property<double?>("Altitude");

                            b1.Property<double?>("Latitude");

                            b1.Property<double?>("Longitude");

                            b1.HasKey("ImageId");

                            b1.ToTable("Images");

                            b1.HasOne("Neptuo.Recollections.Entries.Image")
                                .WithOne("Location")
                                .HasForeignKey("Neptuo.Recollections.Entries.Location", "ImageId")
                                .OnDelete(DeleteBehavior.Cascade);
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