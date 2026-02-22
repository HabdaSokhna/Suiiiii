using System;
using System.Collections.Generic;
using System.Text;

using Database;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CURD
{
    // ===================================
    // Interface
    // ===================================
    public interface ICitizenPhoneRepository
    {
        // Create
        Task<Citizen_Phone> CreateAsync(Citizen_Phone phone);

        // Read
        Task<IEnumerable<Citizen_Phone>> GetAllAsync();
        Task<Citizen_Phone?> GetByIdAsync(int id);
        Task<Citizen_Phone?> GetByPhoneNumberAsync(string phoneNumber);
        Task<IEnumerable<Citizen_Phone>> GetByCitizenIdAsync(int citizenId);

        // Update
        Task<Citizen_Phone> UpdateAsync(Citizen_Phone phone);

        // Delete (Hard Delete - لأن مفيش IsDeleted)
        Task<bool> DeleteAsync(int id);

        // Helper Methods
        Task<bool> ExistsAsync(int id);
        Task<bool> PhoneNumberExistsAsync(string phoneNumber);
        Task<bool> CitizenHasPhonesAsync(int citizenId);
        Task<int> GetPhonesCountByCitizenAsync(int citizenId);
    }

    // ===================================
    // Implementation
    // ===================================
    public class CitizenPhoneRepository : ICitizenPhoneRepository
    {
        private readonly Ai_Reports_Context _context;

        public CitizenPhoneRepository(Ai_Reports_Context context)
        {
            _context = context;
        }

        // ===================================
        // Create
        // ===================================
        public async Task<Citizen_Phone> CreateAsync(Citizen_Phone phone)
        {

            _context.TbCitizen_Phone.Add(phone);
            await _context.SaveChangesAsync();

            return phone;
        }

        // ===================================
        // Read
        // ===================================
        public async Task<IEnumerable<Citizen_Phone>> GetAllAsync()
        {
            return await _context.TbCitizen_Phone
                .Include(p => p.Citizen)
                .ToListAsync();
        }

        public async Task<Citizen_Phone?> GetByIdAsync(int id)
        {
            return await _context.TbCitizen_Phone
                .Include(p => p.Citizen)
                .FirstOrDefaultAsync(p => p.Phone_Id == id);
        }

        public async Task<Citizen_Phone?> GetByPhoneNumberAsync(string phoneNumber)
        {
            return await _context.TbCitizen_Phone
                .Include(p => p.Citizen)
                .FirstOrDefaultAsync(p => p.Phone_Number == phoneNumber);
        }

        public async Task<IEnumerable<Citizen_Phone>> GetByCitizenIdAsync(int citizenId)
        {
            return await _context.TbCitizen_Phone
                .Where(p => p.Citizen_ID == citizenId)
                .ToListAsync();
        }

        // ===================================
        // Update
        // ===================================
        public async Task<Citizen_Phone> UpdateAsync(Citizen_Phone phone)
        {
            _context.TbCitizen_Phone.Update(phone);
            await _context.SaveChangesAsync();

            return phone;
        }

        // ===================================
        // Delete (Hard Delete)
        // ===================================
        public async Task<bool> DeleteAsync(int id)
        {
            var phone = await _context.TbCitizen_Phone
                .FirstOrDefaultAsync(p => p.Phone_Id == id);

            if (phone == null) return false;

            _context.TbCitizen_Phone.Remove(phone);
            await _context.SaveChangesAsync();

            return true;
        }

        // ===================================
        // Helper Methods
        // ===================================
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.TbCitizen_Phone
                .AnyAsync(p => p.Phone_Id == id);
        }

        public async Task<bool> PhoneNumberExistsAsync(string phoneNumber)
        {
            return await _context.TbCitizen_Phone
                .AnyAsync(p => p.Phone_Number == phoneNumber);
        }

        public async Task<bool> CitizenHasPhonesAsync(int citizenId)
        {
            return await _context.TbCitizen_Phone
                .AnyAsync(p => p.Citizen_ID == citizenId);
        }

        public async Task<int> GetPhonesCountByCitizenAsync(int citizenId)
        {
            return await _context.TbCitizen_Phone
                .CountAsync(p => p.Citizen_ID == citizenId);
        }
    }
}
