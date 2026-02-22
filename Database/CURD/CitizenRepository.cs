using Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CURD
{
    public interface ICitizenRepository
    {
        // Create
        Task<Citizen> CreateAsync(Citizen citizen);

        // Read
        Task<IEnumerable<Citizen>> GetAllAsync();
        Task<Citizen?> GetByIdAsync(int id);
        Task<Citizen?> GetByNationalIdAsync(string nationalId);
        Task<Citizen?> GetByEmailAsync(string email);

        // Update
        Task<Citizen> UpdateAsync(Citizen citizen);

        // Delete (Soft Delete)
        Task<bool> SoftDeleteAsync(int id);
        Task<bool> RestoreAsync(int id);

        // Helper Methods
        Task<bool> ExistsAsync(int id);
        Task<bool> NationalIdExistsAsync(string nationalId);
        Task<bool> EmailExistsAsync(string email);

        // Get with Relations
        Task<Citizen?> GetByIdWithPhonesAsync(int id);
        Task<Citizen?> GetByIdWithReportsAsync(int id);
        Task<Citizen?> GetByIdWithAllAsync(int id);
    }


    public class CitizenRepository : ICitizenRepository
    {
        private readonly Ai_Reports_Context _context;

        public CitizenRepository(Ai_Reports_Context context)
        {
            _context = context;
        }

        public async Task<Citizen> CreateAsync(Citizen citizen)
        {
            citizen.CreatedAt = DateTime.Now;
            citizen.IsDeleted = false;

            _context.TbCitizen.Add(citizen);
            await _context.SaveChangesAsync();

            return citizen;
        }
        public async Task<IEnumerable<Citizen>> GetAllAsync()
        {
            return await _context.TbCitizen.ToListAsync();
        }
        public async Task<Citizen?> GetByIdAsync(int id)
        {
            return await _context.TbCitizen
                .FirstOrDefaultAsync(c => c.Citizen_ID == id);
        }
        public async Task<Citizen?> GetByNationalIdAsync(string nationalId)
        {
            return await _context.TbCitizen
                .FirstOrDefaultAsync(c => c.Citizen_National_Id == nationalId);
        }
        public async Task<Citizen?> GetByEmailAsync(string email)
        {
            return await _context.TbCitizen
                .FirstOrDefaultAsync(c => c.Citizen_Email == email);
        }
        public async Task<Citizen> UpdateAsync(Citizen citizen)
        {
            _context.TbCitizen.Update(citizen);
            await _context.SaveChangesAsync();

            return citizen;
        }
        public async Task<bool> SoftDeleteAsync(int id)
        {
            var citizen = await GetByIdAsync(id);
            if (citizen == null) return false;

            citizen.IsDeleted = true;

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> RestoreAsync(int id)
        {
            var citizen = await _context.TbCitizen
                .IgnoreQueryFilters() 
                .FirstOrDefaultAsync(c => c.Citizen_ID == id && c.IsDeleted);

            if (citizen == null) return false;

            citizen.IsDeleted = false;

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.TbCitizen
                .AnyAsync(c => c.Citizen_ID == id);
        }
        public async Task<bool> NationalIdExistsAsync(string nationalId)
        {
            return await _context.TbCitizen
                .AnyAsync(c => c.Citizen_National_Id == nationalId);
        }
        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.TbCitizen
                .AnyAsync(c => c.Citizen_Email == email);
        }

        //Get By Relations
        public async Task<Citizen?> GetByIdWithPhonesAsync(int id)
        {
            return await _context.TbCitizen
                .Include(c => c.LstPhone)
                .FirstOrDefaultAsync(c => c.Citizen_ID == id);
        }

        public async Task<Citizen?> GetByIdWithReportsAsync(int id)
        {
            return await _context.TbCitizen
                .Include(c => c.LstReport)
                .FirstOrDefaultAsync(c => c.Citizen_ID == id);
        }

        public async Task<Citizen?> GetByIdWithAllAsync(int id)
        {
            return await _context.TbCitizen
                .Include(c => c.LstPhone)
                .Include(c => c.LstReport)
                .FirstOrDefaultAsync(c => c.Citizen_ID == id);
        }
    }
}