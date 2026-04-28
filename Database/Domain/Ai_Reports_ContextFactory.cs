using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Text;

namespace Database.Domain
{
    public class Ai_Reports_ContextFactory : IDesignTimeDbContextFactory<Ai_Reports_Context>
    {
        public Ai_Reports_Context CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<Ai_Reports_Context>();

            // استخدم الـ Connection String بتاعتك هنا (زي اللي في الـ appsettings)
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SIRS_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

            return new Ai_Reports_Context(optionsBuilder.Options);
        }
    }
}
