using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TwinYields.DB.Models;
using Microsoft.Extensions.Configuration;
using Models;

namespace TwinYields.DB
{
    public partial class TwinYieldsContext : DbContext
    {
        public TwinYieldsContext()
        {
        }

        public TwinYieldsContext(DbContextOptions<TwinYieldsContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Farm> Farms { get; set; }
        public virtual DbSet<Field> Fields { get; set; }
        public virtual DbSet<Zone> Zones { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var config = new ConfigurationBuilder()
                    .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
                    .Build();
                optionsBuilder.UseNpgsql(config.GetConnectionString("TwinYields"), x => x.UseNetTopologySuite());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("postgis");

            modelBuilder.Entity<Farm>(entity =>
            {
                entity.ToTable("Farm");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(120)
                    .HasColumnName("name");
            });

            modelBuilder.Entity<Field>(entity =>
            {
                entity.ToTable("Field");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.FarmId).HasColumnName("farm_id");

                entity.Property(e => e.Geometry)
                    .IsRequired()
                    .HasColumnName("geometry");

                entity.Property(e => e.LpisId).HasColumnName("lpis_id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnName("name");

                entity.HasOne(d => d.Farm)
                    .WithMany(p => p.Fields)
                    .HasForeignKey(d => d.FarmId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Farm");
            });

            modelBuilder.Entity<Zone>(entity =>
            {
                entity.ToTable("Zone");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Crop)
                    .HasMaxLength(200)
                    .HasColumnName("crop");

                entity.Property(e => e.Cultivar)
                    .HasMaxLength(200)
                    .HasColumnName("cultivar");

                entity.Property(e => e.FieldId).HasColumnName("field_id");

                entity.Property(e => e.Geometry).HasColumnName("geometry");

                entity.HasOne(d => d.Field)
                    .WithMany(p => p.Zones)
                    .HasForeignKey(d => d.FieldId)
                    .HasConstraintName("Field");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
