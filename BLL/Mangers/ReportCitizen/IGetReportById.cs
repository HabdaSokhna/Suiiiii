using BLL.DTO.Report;
using Database;
using Microsoft.EntityFrameworkCore;

namespace BLL.Managers.ReportCitizen
{
    public interface IGetReportById
    {
        Task<Report_Dto?> ExecuteAsync(int reportId, string userId, string baseUrl);
    }

    public class GetReportById : IGetReportById
    {
        private readonly Ai_Reports_Context _context;

        public GetReportById(Ai_Reports_Context context)
        {
            _context = context;
        }

        public async Task<Report_Dto?> ExecuteAsync(int reportId, string userId, string baseUrl)
        {
            // جلب البلاغ مع التأكد إنه تابع للمستخدم الحالي (Security Check)
            var report = await _context.TbReport
                .Include(r => r.LstHandle) // تحميل جهات المعالجة
                .Where(r => r.Report_ID == reportId && r.Citizen.ApplicationUserId == userId && !r.IsDeleted)
                .FirstOrDefaultAsync();

            if (report == null) return null;

            // استخراج العنوان والوصف من الحقل المدمج
            var descriptionParts = report.Report_Description.Split(new[] { "\nDescription: " }, StringSplitOptions.None);
            var title = descriptionParts[0].Replace("Title: ", "");
            var description = descriptionParts.Length > 1 ? descriptionParts[1] : report.Report_Description;

            // المابينج للـ Report_Dto (النسخة الكاملة)
            return new Report_Dto
            {
                Report_ID = report.Report_ID,
                Title = title,
                Description = description,
                Location = report.Report_GeoLocation,
                DisplayCategory = report.Report_Category ?? "General",
                UserSelectedCategory = report.Report_Category, // أو الحقل الأصلي لو متاح
                AiPredictedCategory = report.Report_Category,
                PhotoUrl = string.IsNullOrEmpty(report.PhotoPath) ? null : $"{baseUrl}{report.PhotoPath}",
                ConfidenceScore = report.Confidence_Score,
                SubmittedAt = report.Report_Submit,
                Status = report.LstHandle.OrderByDescending(h => h.LastUpdated).Select(h => h.Status).FirstOrDefault() ?? "Pending",

                // تحويل الـ Handles لـ DTO
                Handles = report.LstHandle.Select(h => new HandleInfo
                {
                    AuthorityName = "Government Authority", // ممكن تربطها بجدول الجهات لو عندك
                    Department = "Operations Dept",
                    Status = h.Status,
                    LastUpdated = h.LastUpdated
                }).ToList()
            };
        }
    }
}