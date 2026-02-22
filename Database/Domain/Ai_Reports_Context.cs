
using Database.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;


namespace Database
{
    public class Ai_Reports_Context : IdentityDbContext<ApplicationUser>
    {

        #region DB_Set
        public DbSet<Authority> TbAuthority { get; set; }
        public DbSet<Authority_Contact> TbAuthority_Contact { get; set; }
        public DbSet<Citizen> TbCitizen { get; set; }
        public DbSet<Citizen_Phone> TbCitizen_Phone { get; set; }
        public DbSet<Handle> TbHandle { get; set; }
        public DbSet<Report> TbReport { get; set; }
        public DbSet<Notification> TbNotification { get; set; }
        #endregion
        public Ai_Reports_Context(DbContextOptions<Ai_Reports_Context> options)
        : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. القوة السيادية: استدعاء أساس الهوية أولاً
            base.OnModelCreating(modelBuilder);

            // --- إعداد جدول الجهات المختصة ---
            modelBuilder.Entity<Authority>(entity =>
            {
                entity.ToTable("TbAuthority");
                entity.HasKey(e => e.Authority_ID);
                entity.HasIndex(e => new { e.Authority_Name, e.Department_Name }).IsUnique();
                entity.Property(e => e.Authority_Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Department_Name).IsRequired().HasMaxLength(100);
            });

            // --- إعداد جهات الاتصال للجهات ---
            modelBuilder.Entity<Authority_Contact>(entity =>
            {
                entity.ToTable("TbAuthority_Contact");
                entity.HasKey(e => e.Contact_Id);
                entity.HasIndex(e => new { e.Authority_ID, e.Contact_Info }).IsUnique();
                entity.Property(e => e.Contact_Info).IsRequired().HasMaxLength(200);

                entity.HasOne(d => d.Authority)
                      .WithMany(p => p.LstAuthorityContacts)
                      .HasForeignKey(d => d.Authority_ID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // --- إعداد جدول المواطنين ---
            modelBuilder.Entity<Citizen>(entity =>
            {
                entity.ToTable("TbCitizen");
                entity.HasKey(e => e.Citizen_ID);
                entity.HasIndex(e => e.Citizen_National_Id).IsUnique();
                entity.Property(e => e.Citizen_National_Id).IsRequired().HasMaxLength(14).IsFixedLength();

                entity.HasIndex(e => e.Citizen_Email).IsUnique();
                entity.Property(e => e.Citizen_Email).IsRequired().HasMaxLength(150);

                entity.Property(e => e.Citizen_Name).IsRequired().HasMaxLength(150);

                // ربط الـ Identity
                entity.HasOne(c => c.User)
                      .WithOne(u => u.CitizenProfile)
                      .HasForeignKey<Citizen>(c => c.ApplicationUserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            });

            // --- إعداد أرقام هواتف المواطنين ---
            modelBuilder.Entity<Citizen_Phone>(entity =>
            {
                entity.ToTable("TbCitizen_Phone");
                entity.HasKey(e => e.Phone_Id);
                entity.HasIndex(e => e.Phone_Number).IsUnique();
                entity.Property(e => e.Phone_Number).IsRequired().HasMaxLength(11).IsFixedLength();

                entity.HasOne(p => p.Citizen)
                      .WithMany(c => c.LstPhone)
                      .HasForeignKey(p => p.Citizen_ID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // --- إعداد جدول البلاغات ---
            modelBuilder.Entity<Report>(entity =>
            {
                entity.ToTable("TbReport");
                entity.HasKey(e => e.Report_ID);
                entity.Property(e => e.Report_Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Report_GeoLocation).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Report_Submit).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Confidence_Score).HasColumnType("decimal(5,2)");
                entity.Property(e => e.PhotoPath).HasMaxLength(500);

                entity.HasOne(r => r.Citizen)
                      .WithMany(c => c.LstReport)
                      .HasForeignKey(r => r.Citizen_ID)
                      .OnDelete(DeleteBehavior.Restrict); // حماية البلاغ من الحذف
            });

            // --- إعداد جدول المعالجة (Handle) - الربط بين البلاغ والجهة ---
            modelBuilder.Entity<Handle>(entity =>
            {
                entity.ToTable("TbHandle");
                entity.HasKey(h => new { h.Report_ID, h.Authority_ID });

                entity.HasOne(h => h.Report)
                      .WithMany(r => r.LstHandle)
                      .HasForeignKey(h => h.Report_ID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(h => h.Authority)
                      .WithMany(a => a.LstHandle)
                      .HasForeignKey(h => h.Authority_ID)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
           
        
   
