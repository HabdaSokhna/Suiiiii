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
    public interface IReportRepository
    {
        // Create
        Task<Report> CreateAsync(Report report);

        // Read
        Task<IEnumerable<Report>> GetAllAsync();
        Task<Report?> GetByIdAsync(int id);
        Task<IEnumerable<Report>> GetByCitizenIdAsync(int citizenId);
        Task<IEnumerable<Report>> GetByCategoryAsync(string category);
        Task<IEnumerable<Report>> GetByPredictedCategoryAsync(string predictedCategory);
        Task<IEnumerable<Report>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        // Update
        Task<Report> UpdateAsync(Report report);
        Task<bool> UpdateAiPredictionAsync(int reportId, string predictedCategory, float confidenceScore, DateTime aiTime);

        // Delete (Soft Delete)
        Task<bool> SoftDeleteAsync(int id);
        Task<bool> RestoreAsync(int id);

        // Helper Methods
        Task<bool> ExistsAsync(int id);
        Task<int> GetReportsCountByCitizenAsync(int citizenId);
        Task<int> GetReportsCountByCategoryAsync(string category);

        // Get with Relations
        Task<Report?> GetByIdWithCitizenAsync(int id);
        Task<Report?> GetByIdWithHandlesAsync(int id);
        Task<Report?> GetByIdWithAllAsync(int id);

        // Statistics
        Task<IEnumerable<Report>> GetRecentReportsAsync(int count);
        Task<IEnumerable<Report>> GetPendingAiReportsAsync();
        Task<IEnumerable<Report>> GetLowConfidenceReportsAsync(float threshold);
    }

    // ===================================
    // Implementation
    // ===================================
    public class ReportRepository : IReportRepository
    {
        private readonly Ai_Reports_Context _context;

        public ReportRepository(Ai_Reports_Context context)
        {
            _context = context;
        }

        // ===================================
        // Create
        // ===================================
        public async Task<Report> CreateAsync(Report report)
        {
            report.CreatedAt = DateTime.Now;
            report.Report_Submit = DateTime.Now;
            report.IsDeleted = false;

            _context.TbReport.Add(report);
            await _context.SaveChangesAsync();

            return report;
        }

        // ===================================
        // Read
        // ===================================
        public async Task<IEnumerable<Report>> GetAllAsync()
        {
            return await _context.TbReport.ToListAsync();
        }

        public async Task<Report?> GetByIdAsync(int id)
        {
            return await _context.TbReport
                .FirstOrDefaultAsync(r => r.Report_ID == id);
        }

        public async Task<IEnumerable<Report>> GetByCitizenIdAsync(int citizenId)
        {
            return await _context.TbReport
                .Where(r => r.Citizen_ID == citizenId)
                .OrderByDescending(r => r.Report_Submit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetByCategoryAsync(string category)
        {
            return await _context.TbReport
                .Where(r => r.Report_Category == category)
                .OrderByDescending(r => r.Report_Submit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetByPredictedCategoryAsync(string predictedCategory)
        {
            return await _context.TbReport
                .Where(r => r.Report_PredictedCategory == predictedCategory)
                .OrderByDescending(r => r.Report_Submit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.TbReport
                .Where(r => r.Report_Submit >= startDate && r.Report_Submit <= endDate)
                .OrderByDescending(r => r.Report_Submit)
                .ToListAsync();
        }

        // ===================================
        // Update
        // ===================================
        public async Task<Report> UpdateAsync(Report report)
        {
            report.UpdatedAt = DateTime.Now;

            _context.TbReport.Update(report);
            await _context.SaveChangesAsync();

            return report;
        }

        public async Task<bool> UpdateAiPredictionAsync(int reportId, string predictedCategory, float confidenceScore, DateTime aiTime)
        {
            var report = await GetByIdAsync(reportId);
            if (report == null) return false;

            report.Report_PredictedCategory = predictedCategory;
            report.Confidence_Score = confidenceScore;
            report.AiTime = aiTime;
            report.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        // ===================================
        // Delete (Soft Delete)
        // ===================================
        public async Task<bool> SoftDeleteAsync(int id)
        {
            var report = await GetByIdAsync(id);
            if (report == null) return false;

            report.IsDeleted = true;
            report.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreAsync(int id)
        {
            var report = await _context.TbReport
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Report_ID == id && r.IsDeleted);

            if (report == null) return false;

            report.IsDeleted = false;
            report.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        // ===================================
        // Helper Methods
        // ===================================
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.TbReport
                .AnyAsync(r => r.Report_ID == id);
        }

        public async Task<int> GetReportsCountByCitizenAsync(int citizenId)
        {
            return await _context.TbReport
                .CountAsync(r => r.Citizen_ID == citizenId);
        }

        public async Task<int> GetReportsCountByCategoryAsync(string category)
        {
            return await _context.TbReport
                .CountAsync(r => r.Report_Category == category);
        }

        // ===================================
        // Get with Relations
        // ===================================
        public async Task<Report?> GetByIdWithCitizenAsync(int id)
        {
            return await _context.TbReport
                .Include(r => r.Citizen)
                .FirstOrDefaultAsync(r => r.Report_ID == id);
        }

        public async Task<Report?> GetByIdWithHandlesAsync(int id)
        {
            return await _context.TbReport
                .Include(r => r.LstHandle)
                    .ThenInclude(h => h.Authority)
                .FirstOrDefaultAsync(r => r.Report_ID == id);
        }

        public async Task<Report?> GetByIdWithAllAsync(int id)
        {
            return await _context.TbReport
                .Include(r => r.Citizen)
                .Include(r => r.LstHandle)
                    .ThenInclude(h => h.Authority)
                .FirstOrDefaultAsync(r => r.Report_ID == id);
        }

        // ===================================
        // Statistics
        // ===================================
        public async Task<IEnumerable<Report>> GetRecentReportsAsync(int count)
        {
            return await _context.TbReport
                .OrderByDescending(r => r.Report_Submit)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetPendingAiReportsAsync()
        {
            return await _context.TbReport
                .Where(r => r.Report_PredictedCategory == null || r.AiTime == null)
                .OrderBy(r => r.Report_Submit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetLowConfidenceReportsAsync(float threshold)
        {
            return await _context.TbReport
                .Where(r => r.Confidence_Score < threshold && r.Report_PredictedCategory != null)
                .OrderBy(r => r.Confidence_Score)
                .ToListAsync();
        }
    }
}
