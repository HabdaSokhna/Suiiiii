using Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CURD
{
    public interface IAuthorityContactRepository
    {
        // Create
        Task<Authority_Contact> CreateAsync(Authority_Contact contact);

        // Read
        Task<IEnumerable<Authority_Contact>> GetAllAsync();
        Task<Authority_Contact?> GetByIdAsync(int id);
        Task<IEnumerable<Authority_Contact>> GetByAuthorityIdAsync(int authorityId);

        // Update
        Task<Authority_Contact> UpdateAsync(Authority_Contact contact);

        // Delete (Hard Delete - لأن مفيش IsDeleted)
        Task<bool> DeleteAsync(int id);

        // Helper Methods
        Task<bool> ExistsAsync(int id);
        Task<bool> AuthorityHasContactsAsync(int authorityId);
        Task<int> GetContactsCountByAuthorityAsync(int authorityId);
    }

    public class AuthorityContactRepository : IAuthorityContactRepository
    {
        private readonly Ai_Reports_Context _context;

        public AuthorityContactRepository(Ai_Reports_Context context)
        {
            _context = context;
        }
        // Create
        public async Task<Authority_Contact> CreateAsync(Authority_Contact contact)
        {
            _context.TbAuthority_Contact.Add(contact);
            await _context.SaveChangesAsync();

            return contact;
        }
        // Read
        public async Task<IEnumerable<Authority_Contact>> GetAllAsync()
        {
            return await _context.TbAuthority_Contact
                .Include(c => c.Authority)
                .ToListAsync();
        }
        public async Task<Authority_Contact?> GetByIdAsync(int id)
        {
            return await _context.TbAuthority_Contact
                .Include(c => c.Authority)
                .FirstOrDefaultAsync(c => c.Contact_Id == id);
        }

        public async Task<IEnumerable<Authority_Contact>> GetByAuthorityIdAsync(int authorityId)
        {
            return await _context.TbAuthority_Contact
                .Where(c => c.Authority_ID == authorityId)
                .ToListAsync();
        }
        // Update
        public async Task<Authority_Contact> UpdateAsync(Authority_Contact contact)
        {
            _context.TbAuthority_Contact.Update(contact);
            await _context.SaveChangesAsync();

            return contact;
        }
        // Delete (Hard Delete)
        public async Task<bool> DeleteAsync(int id)
        {
            var contact = await _context.TbAuthority_Contact
                .FirstOrDefaultAsync(c => c.Contact_Id == id);

            if (contact == null) return false;

            _context.TbAuthority_Contact.Remove(contact);
            await _context.SaveChangesAsync();

            return true;
        }
        // Helper Methods
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.TbAuthority_Contact
                .AnyAsync(c => c.Contact_Id == id);
        }
        public async Task<bool> AuthorityHasContactsAsync(int authorityId)
        {
            return await _context.TbAuthority_Contact
                .AnyAsync(c => c.Authority_ID == authorityId);
        }
        public async Task<int> GetContactsCountByAuthorityAsync(int authorityId)
        {
            return await _context.TbAuthority_Contact
                .CountAsync(c => c.Authority_ID == authorityId);
        }
    }
}