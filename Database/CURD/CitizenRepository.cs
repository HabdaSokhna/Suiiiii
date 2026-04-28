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
        Task<Citizen> CreateAsync(Citizen citizen);
        Task<IEnumerable<Citizen>> GetAllAsync();
        Task<Citizen?> GetByIdAsync(int id);
        Task<Citizen?> GetByNationalIdAsync(string nationalId);

        
        Task<Citizen?> GetByEmailAsync(string email);
        Task<bool> EmailExistsAsync(string email);

        Task<Citizen> UpdateAsync(Citizen citizen);
        Task<bool> SoftDeleteAsync(int id);
        Task<bool> RestoreAsync(int id);

        Task<bool> ExistsAsync(int id);
        Task<bool> NationalIdExistsAsync(string nationalId);

        Task<Citizen?> GetByIdWithPhonesAsync(int id);
        Task<Citizen?> GetByIdWithReportsAsync(int id);
        Task<Citizen?> GetByIdWithAllAsync(int id);

        
        Task<string?> GetTokenByIdAsync(int id);
    }
    public class CitizenRepository : ICitizenRepository
    {
        private readonly Ai_Reports_Context _context;

        public CitizenRepository(Ai_Reports_Context context)
        {
            _context = context;
        }

        // البحث بالإيميل من خلال جدول الـ Identity
        public async Task<Citizen?> GetByEmailAsync(string email)
        {
            return await _context.TbCitizen
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.User.Email == email);
        }

        // جلب التوكن من جدول الـ Identity
        public async Task<string?> GetTokenByIdAsync(int id)
        {
            return await _context.TbCitizen
                .Where(c => c.Citizen_ID == id)
                .Select(c => c.User.DeviceToken) // التوكن دلوقتى جوه الـ User
                .FirstOrDefaultAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.TbCitizen
                .AnyAsync(c => c.User.Email == email);
        }

        // الـ Soft Delete دلوقتى بيقفل حساب الـ Identity
        public async Task<bool> SoftDeleteAsync(int id)
        {
            var citizen = await _context.TbCitizen
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Citizen_ID == id);

            if (citizen == null || citizen.User == null) return false;

            citizen.User.IsDeleted = true; // الـ Flag بقى في الـ User
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreAsync(int id)
        {
            var citizen = await _context.TbCitizen
                .IgnoreQueryFilters()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Citizen_ID == id);

            if (citizen == null || citizen.User == null) return false;

            citizen.User.IsDeleted = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // باقي الميثودز التقليدية
        public async Task<Citizen> CreateAsync(Citizen citizen)
        {
            _context.TbCitizen.Add(citizen);
            await _context.SaveChangesAsync();
            return citizen;
        }

        public async Task<IEnumerable<Citizen>> GetAllAsync()
        {
            return await _context.TbCitizen.Include(c => c.User).ToListAsync();
        }

        public async Task<Citizen?> GetByIdAsync(int id)
        {
            return await _context.TbCitizen
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Citizen_ID == id);
        }

        public async Task<Citizen?> GetByNationalIdAsync(string nationalId)
        {
            return await _context.TbCitizen
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Citizen_National_Id == nationalId);
        }

        public async Task<Citizen> UpdateAsync(Citizen citizen)
        {
            _context.TbCitizen.Update(citizen);
            await _context.SaveChangesAsync();
            return citizen;
        }

        public async Task<bool> ExistsAsync(int id) => await _context.TbCitizen.AnyAsync(c => c.Citizen_ID == id);

        public async Task<bool> NationalIdExistsAsync(string nationalId) =>
            await _context.TbCitizen.AnyAsync(c => c.Citizen_National_Id == nationalId);

        public async Task<Citizen?> GetByIdWithPhonesAsync(int id) =>
            await _context.TbCitizen.Include(c => c.LstPhone).FirstOrDefaultAsync(c => c.Citizen_ID == id);

        public async Task<Citizen?> GetByIdWithReportsAsync(int id) =>
            await _context.TbCitizen.Include(c => c.LstReport).FirstOrDefaultAsync(c => c.Citizen_ID == id);

        public async Task<Citizen?> GetByIdWithAllAsync(int id) =>
            await _context.TbCitizen.Include(c => c.User).Include(c => c.LstPhone).Include(c => c.LstReport).FirstOrDefaultAsync(c => c.Citizen_ID == id);
    }



}