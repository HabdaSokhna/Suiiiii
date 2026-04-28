using System;
using System.Collections.Generic;
using System.Text;

using Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CURD
{
    // ===================================
    // Interface
    // ===================================
    public interface IHandleRepository
    {
        // Create
        Task<Handle> CreateAsync(Handle handle);

        // Read
        Task<IEnumerable<Handle>> GetAllAsync();
        Task<Handle?> GetByIdAsync(int reportId, int authorityId);
        Task<IEnumerable<Handle>> GetByReportIdAsync(int reportId);
        Task<IEnumerable<Handle>> GetByAuthorityIdAsync(int authorityId);
        Task<IEnumerable<Handle>> GetByStatusAsync(string status);

        // Update
        Task<Handle> UpdateAsync(Handle handle);
        Task<bool> UpdateStatusAsync(int reportId, int authorityId, string status);

        // Delete (Hard Delete - لأن مفيش IsDeleted)
        Task<bool> DeleteAsync(int reportId, int authorityId);

        // Helper Methods
        Task<bool> ExistsAsync(int reportId, int authorityId);
        Task<bool> ReportHasHandlesAsync(int reportId);
        Task<bool> AuthorityHasHandlesAsync(int authorityId);
        Task<int> GetHandlesCountByReportAsync(int reportId);
        Task<int> GetHandlesCountByAuthorityAsync(int authorityId);
        Task<int> GetHandlesCountByStatusAsync(string status);

        // Get with Relations
        Task<Handle?> GetByIdWithReportAsync(int reportId, int authorityId);
        Task<Handle?> GetByIdWithAuthorityAsync(int reportId, int authorityId);
        Task<Handle?> GetByIdWithAllAsync(int reportId, int authorityId);
        Task<IEnumerable<Handle>> GetByAuthorityIdWithReportsAsync(int authorityId);
        Task<IEnumerable<Handle>> GetReportsByAuthorityIdAsync(int authorityId);

        // Statistics
        Task<IEnumerable<Handle>> GetRecentHandlesAsync(int count);
        Task<IEnumerable<Handle>> GetPendingHandlesAsync();
    }

    // ===================================
    // Implementation
    // ===================================
    public class HandleRepository : IHandleRepository
    {
        private readonly Ai_Reports_Context _context;

        public HandleRepository(Ai_Reports_Context context)
        {
            _context = context;
        }

        // ===================================
        // Create
        // ===================================
        public async Task<Handle> CreateAsync(Handle handle)
        {
            handle.LastUpdated = DateTime.Now;

            _context.TbHandle.Add(handle);
            await _context.SaveChangesAsync();

            return handle;
        }

        // ===================================
        // Read
        // ===================================
        public async Task<IEnumerable<Handle>> GetAllAsync()
        {
            return await _context.TbHandle
                .Include(h => h.Report)
                .Include(h => h.Authority)
                .ToListAsync();
        }
        public async Task<IEnumerable<Handle>> GetReportsByAuthorityIdAsync(int authorityId)
        {
            return await _context.TbHandle
                .Include(h => h.Report) // عشان نجيب تفاصيل البلاغ معاه
                .Where(h => h.Authority_ID == authorityId && h.Status == "Pending")
                .ToListAsync();
        }
        public async Task<Handle?> GetByIdAsync(int reportId, int authorityId)
        {
            return await _context.TbHandle
                .FirstOrDefaultAsync(h => h.Report_ID == reportId && h.Authority_ID == authorityId);
        }

        public async Task<IEnumerable<Handle>> GetByReportIdAsync(int reportId)
        {
            return await _context.TbHandle
                .Include(h => h.Authority)
                .Where(h => h.Report_ID == reportId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Handle>> GetByAuthorityIdAsync(int authorityId)
        {
            return await _context.TbHandle
                .Include(h => h.Report)
                .Where(h => h.Authority_ID == authorityId)
                .OrderByDescending(h => h.LastUpdated)
                .ToListAsync();
        }

        public async Task<IEnumerable<Handle>> GetByStatusAsync(string status)
        {
            return await _context.TbHandle
                .Include(h => h.Report)
                .Include(h => h.Authority)
                .Where(h => h.Status == status)
                .OrderByDescending(h => h.LastUpdated)
                .ToListAsync();
        }

        // ===================================
        // Update
        // ===================================
        public async Task<Handle> UpdateAsync(Handle handle)
        {
            handle.LastUpdated = DateTime.Now;

            _context.TbHandle.Update(handle);
            await _context.SaveChangesAsync();

            return handle;
        }

        public async Task<bool> UpdateStatusAsync(int reportId, int authorityId, string status)
        {
            var handle = await GetByIdAsync(reportId, authorityId);
            if (handle == null) return false;

            handle.Status = status;
            handle.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        // ===================================
        // Delete (Hard Delete)
        // ===================================
        public async Task<bool> DeleteAsync(int reportId, int authorityId)
        {
            var handle = await GetByIdAsync(reportId, authorityId);
            if (handle == null) return false;

            _context.TbHandle.Remove(handle);
            await _context.SaveChangesAsync();

            return true;
        }

        // ===================================
        // Helper Methods
        // ===================================
        public async Task<bool> ExistsAsync(int reportId, int authorityId)
        {
            return await _context.TbHandle
                .AnyAsync(h => h.Report_ID == reportId && h.Authority_ID == authorityId);
        }

        public async Task<bool> ReportHasHandlesAsync(int reportId)
        {
            return await _context.TbHandle
                .AnyAsync(h => h.Report_ID == reportId);
        }

        public async Task<bool> AuthorityHasHandlesAsync(int authorityId)
        {
            return await _context.TbHandle
                .AnyAsync(h => h.Authority_ID == authorityId);
        }

        public async Task<int> GetHandlesCountByReportAsync(int reportId)
        {
            return await _context.TbHandle
                .CountAsync(h => h.Report_ID == reportId);
        }

        public async Task<int> GetHandlesCountByAuthorityAsync(int authorityId)
        {
            return await _context.TbHandle
                .CountAsync(h => h.Authority_ID == authorityId);
        }

        public async Task<int> GetHandlesCountByStatusAsync(string status)
        {
            return await _context.TbHandle
                .CountAsync(h => h.Status == status);
        }

        // ===================================
        // Get with Relations
        // ===================================
        public async Task<Handle?> GetByIdWithReportAsync(int reportId, int authorityId)
        {
            return await _context.TbHandle
                .Include(h => h.Report)
                    .ThenInclude(r => r.Citizen)
                .FirstOrDefaultAsync(h => h.Report_ID == reportId && h.Authority_ID == authorityId);
        }

        public async Task<Handle?> GetByIdWithAuthorityAsync(int reportId, int authorityId)
        {
            return await _context.TbHandle
                .Include(h => h.Authority)
                    .ThenInclude(a => a.LstAuthorityContacts)
                .FirstOrDefaultAsync(h => h.Report_ID == reportId && h.Authority_ID == authorityId);
        }

        public async Task<Handle?> GetByIdWithAllAsync(int reportId, int authorityId)
        {
            return await _context.TbHandle
                .Include(h => h.Report)
                    .ThenInclude(r => r.Citizen)
                .Include(h => h.Authority)
                    .ThenInclude(a => a.LstAuthorityContacts)
                .FirstOrDefaultAsync(h => h.Report_ID == reportId && h.Authority_ID == authorityId);
        }

        public async Task<IEnumerable<Handle>> GetByAuthorityIdWithReportsAsync(int authorityId)
        {
            return await _context.TbHandle
                .Include(h => h.Report)
                    .ThenInclude(r => r.Citizen)
                .Where(h => h.Authority_ID == authorityId)
                .OrderByDescending(h => h.LastUpdated)
                .ToListAsync();
        }

        // ===================================
        // Statistics
        // ===================================
        public async Task<IEnumerable<Handle>> GetRecentHandlesAsync(int count)
        {
            return await _context.TbHandle
                .Include(h => h.Report)
                .Include(h => h.Authority)
                .OrderByDescending(h => h.LastUpdated)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Handle>> GetPendingHandlesAsync()
        {
            return await _context.TbHandle
                .Include(h => h.Report)
                .Include(h => h.Authority)
                .Where(h => h.Status == "Pending")
                .OrderBy(h => h.LastUpdated)
                .ToListAsync();
        }
    }
}
