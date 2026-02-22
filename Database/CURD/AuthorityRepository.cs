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
    public interface IAuthorityRepository
    {
        // Create
        Task<Authority> CreateAsync(Authority authority);

        // Read
        Task<IEnumerable<Authority>> GetAllAsync();
        Task<Authority?> GetByIdAsync(int id);
        Task<Authority?> GetByNameAsync(string authorityName);
        Task<IEnumerable<Authority>> GetByDepartmentAsync(string departmentName);

        // Update
        Task<Authority> UpdateAsync(Authority authority);

        // Delete (Hard Delete - لأن مفيش IsDeleted)
        Task<bool> DeleteAsync(int id);

        // Helper Methods
        Task<bool> ExistsAsync(int id);
        Task<bool> NameExistsAsync(string authorityName);

        // Get with Relations
        Task<Authority?> GetByIdWithContactsAsync(int id);
        Task<Authority?> GetByIdWithHandlesAsync(int id);
        Task<Authority?> GetByIdWithAllAsync(int id);

        // Statistics
        Task<int> GetHandlesCountAsync(int id);
        Task<int> GetContactsCountAsync(int id);
    }
    // Implementation

    public class AuthorityRepository : IAuthorityRepository
    {
        private readonly Ai_Reports_Context _context;

        public AuthorityRepository(Ai_Reports_Context context)
        {
            _context = context;
        }
        // Create
     
        public async Task<Authority> CreateAsync(Authority authority)
        {
            _context.TbAuthority.Add(authority);
            await _context.SaveChangesAsync();

            return authority;
        }
        // Read

        public async Task<IEnumerable<Authority>> GetAllAsync()
        {
            return await _context.TbAuthority.ToListAsync();
        }

        public async Task<Authority?> GetByIdAsync(int id)
        {
            return await _context.TbAuthority
                .FirstOrDefaultAsync(a => a.Authority_ID == id);
        }

        public async Task<Authority?> GetByNameAsync(string authorityName)
        {
            return await _context.TbAuthority
                .FirstOrDefaultAsync(a => a.Authority_Name == authorityName);
        }

        public async Task<IEnumerable<Authority>> GetByDepartmentAsync(string departmentName)
        {
            return await _context.TbAuthority
                .Where(a => a.Department_Name == departmentName)
                .ToListAsync();
        }


        // Update
        public async Task<Authority> UpdateAsync(Authority authority)
        {
            _context.TbAuthority.Update(authority);
            await _context.SaveChangesAsync();

            return authority;
        }
        // Delete (Hard Delete)
     
        public async Task<bool> DeleteAsync(int id)
        {
            var authority = await _context.TbAuthority
                .FirstOrDefaultAsync(a => a.Authority_ID == id);

            if (authority == null) return false;

            _context.TbAuthority.Remove(authority);
            await _context.SaveChangesAsync();

            return true;
        }
        // Helper Methods
  
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.TbAuthority
                .AnyAsync(a => a.Authority_ID == id);
        }

        public async Task<bool> NameExistsAsync(string authorityName)
        {
            return await _context.TbAuthority
                .AnyAsync(a => a.Authority_Name == authorityName);
        }
        // Get with Relations
        
        public async Task<Authority?> GetByIdWithContactsAsync(int id)
        {
            return await _context.TbAuthority
                .Include(a => a.LstAuthorityContacts)
                .FirstOrDefaultAsync(a => a.Authority_ID == id);
        }

        public async Task<Authority?> GetByIdWithHandlesAsync(int id)
        {
            return await _context.TbAuthority
                .Include(a => a.LstHandle)
                    .ThenInclude(h => h.Report)
                .FirstOrDefaultAsync(a => a.Authority_ID == id);
        }

        public async Task<Authority?> GetByIdWithAllAsync(int id)
        {
            return await _context.TbAuthority
                .Include(a => a.LstAuthorityContacts)
                .Include(a => a.LstHandle)
                    .ThenInclude(h => h.Report)
                .FirstOrDefaultAsync(a => a.Authority_ID == id);
        }

        
        // Statistics
        
        public async Task<int> GetHandlesCountAsync(int id)
        {
            return await _context.TbHandle
                .CountAsync(h => h.Authority_ID == id);
        }

        public async Task<int> GetContactsCountAsync(int id)
        {
            return await _context.TbAuthority_Contact
                .CountAsync(c => c.Authority_ID == id);
        }
    }
}
