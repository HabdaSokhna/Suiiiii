using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;


namespace Database
{
    public class Ai_Reports_Context : DbContext
    {

        #region DB_Set
        public DbSet<TbAuthority> Authority { get; set; }
        public DbSet<TbAuthority_Contact> Authority_Contact { get; set; }
        public DbSet<TbCitizen> Citizen { get; set; }
        public DbSet<TbCitizen_Phone> Citizen_Phone { get; set; }
        public DbSet<TbHandle> Handle { get; set; }
        public DbSet<TbReport> Report { get; set; }
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
            modelBuilder.Entity<TbAuthority>(entity =>
          
            {
                //PrimaryKey is Authority_ID
                entity.ToTable("TbAuthority");
                entity.HasKey(e => e.Authority_ID);

                //It is necessary that the name of the authority and the department be unique.
                entity.HasIndex(e => new { e.Authority_Name, e.Department_Name }).IsUnique();

                //Max Length = 100 and Required
                entity.Property(e => e.Authority_Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Department_Name).IsRequired().HasMaxLength(100);

            });
            modelBuilder.Entity<TbAuthority_Contact>(entity =>
            {
                //PrimaryKey
                entity.ToTable("TbAuthority_Contact");
                entity.HasKey(e => e.Contact_Id);

                //AuthoryID and Contact-Info is Uniqe
                entity.HasIndex(e => new { e.Authority_ID, e.Contact_Info }).IsUnique();
                //Contact_info Required and MaxLength = 200
                entity.Property(e => e.Contact_Info).IsRequired().HasMaxLength(200);
                //One Authority Many AuthorityContacts
                entity.HasOne(d => d.Authority)
                      .WithMany(p => p.LstAuthorityContacts)
                      .HasForeignKey(d => d.Authority_ID)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<TbCitizen>(entity =>
            {
                entity.ToTable("TbCitizen");
                entity.HasKey(e => e.Citizen_ID);

                //National ID  "Egyptian People"
                entity.HasIndex(e => e.Citizen_National_Id).IsUnique();
                entity.Property(e => e.Citizen_National_Id)
                      .IsRequired()
                      .HasMaxLength(14)
                      .IsFixedLength();

                //Email is Uniqe and Required and Max Length = 150 
                entity.HasIndex(e => e.Citizen_Email).IsUnique();
                entity.Property(e => e.Citizen_Email)
                      .IsRequired()
                      .HasMaxLength(150);

                //Name is Required and Max Length = 150 
                entity.Property(e => e.Citizen_Name)
                      .IsRequired()
                      .HasMaxLength(150);
              
            });
            modelBuilder.Entity<TbCitizen_Phone>(entity =>
            {
                //Primary Key
                entity.ToTable("TbCitizen_Phone");
                entity.HasKey(e => e.Phone_Id);

               
                entity.HasIndex(e => e.Phone_Number).IsUnique();

                entity.Property(e => e.Phone_Number)
                      .IsRequired()
                      .HasMaxLength(11)
                      .IsFixedLength();

                //RelationShip
                entity.HasOne(p => p.Citizen)
                      .WithMany(c => c.LstPhone)
                      .HasForeignKey(p => p.Citizen_ID)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<TbReport>(entity =>
            {
                //Primary Key
                entity.ToTable("TbReport");
                entity.HasKey(e => e.Report_ID);

              
                entity.Property(e => e.Report_Description)
                      .IsRequired()
                      .HasMaxLength(1000);

              
                entity.Property(e => e.Report_GeoLocation)
                      .IsRequired()
                      .HasMaxLength(100);

              
                entity.Property(e => e.Status)
                      .HasDefaultValue("In Progress")
                      .HasMaxLength(50);

              
                entity.Property(e => e.Report_Submit)
                      .HasDefaultValueSql("GETDATE()");

              
                entity.Property(e => e.Confidence_Score)
                      .HasColumnType("decimal(5,2)");

              
                entity.Property(e => e.PhotoPath)
                      .HasMaxLength(500);

               
                //RelationShip
                entity.HasOne(r => r.Citizen)
                      .WithMany(c => c.LstReport)
                      .HasForeignKey(r => r.Citizen_ID)
                      .OnDelete(DeleteBehavior.Restrict);

           
                entity.HasMany(r => r.LstHandle)
                      .WithOne(h => h.Report)
                      .HasForeignKey(h => h.Report_ID)
                      .OnDelete(DeleteBehavior.Cascade);

            });
            modelBuilder.Entity<TbHandle>(entity =>
            {
                entity.ToTable("TbHandle");

               //Composite Primry Key
                entity.HasKey(h => new { h.Report_ID, h.Authority_ID });

               
                //One Report Many Handle
                entity.HasOne(h => h.Report)
                      .WithMany(r => r.LstHandle)
                      .HasForeignKey(h => h.Report_ID)
                      .OnDelete(DeleteBehavior.Cascade);

                //One Authority Many Handle
                entity.HasOne(h => h.Authority)
                      .WithMany(a => a.LstHandle) 
                      .HasForeignKey(h => h.Authority_ID)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
           
        
   
