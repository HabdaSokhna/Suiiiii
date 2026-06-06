using BLL.DTO.Ai_Model;
using BLL.DTO.Report;
using BLL.DTO.Responce;
using BLL.Helpers;
using BLL.Managers.Notification;
using BLL.Managers.Notifications;
using BLL.Service;
using CURD;
using Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Managers.ReportCitizen
{
    public interface ICreateReport
    {
        Task<ReportResponseDto> ExecuteAsync(ReportCreate_Dto model, string userEmail);
    }

    public class CreateReport : ICreateReport
    {
        private readonly IReportRepository _reportRepo;
        private readonly ICitizenRepository _citizenRepo;
        private readonly ICitizenNotificationManager _notificationManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IAuthorityRepository _authorityRepo;
        private readonly IHandleRepository _handleRepo;
        private readonly IAuthorityNotificationService _authorityNotif;

        private readonly YoloService _fireService;
        private readonly YoloService _accidentService;
        private readonly YoloService _potholeService;

        public CreateReport(
            IReportRepository reportRepo,
            ICitizenRepository citizenRepo,
            [FromKeyedServices("FireService")] YoloService fireService,
            [FromKeyedServices("AccidentService")] YoloService accidentService,
            [FromKeyedServices("PotholeService")] YoloService potholeService,
            ICitizenNotificationManager notificationManager,
            IWebHostEnvironment environment,
            IAuthorityRepository authorityRepo,
            IHandleRepository handleRepo,
            IAuthorityNotificationService authorityNotif)
        {
            _reportRepo = reportRepo;
            _citizenRepo = citizenRepo;
            _fireService = fireService;
            _accidentService = accidentService;
            _potholeService = potholeService;
            _notificationManager = notificationManager;
            _environment = environment;
            _authorityRepo = authorityRepo;
            _handleRepo = handleRepo;
            _authorityNotif = authorityNotif;
        }

        public async Task<ReportResponseDto> ExecuteAsync(ReportCreate_Dto model, string userEmail)
        {
            // 1. جيب الـ Citizen
            var citizen = await _citizenRepo.GetByEmailAsync(userEmail);
            if (citizen == null)
                return new ReportResponseDto { IsSuccess = false, Message = "User Not Found" };

            // 2. تحليل الصورة بالـ AI
            string? photoPath = null;
            string finalTag = "General";
            float topConfidence = 0;
            string aiScores = "";

            if (model.Photo != null && model.Photo.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await model.Photo.CopyToAsync(ms);
                    var imageBytes = ms.ToArray();

                    var fireTask = Task.Run(() => _fireService.AnalyzeImage(imageBytes));
                    var accidentTask = Task.Run(() => _accidentService.AnalyzeImage(imageBytes));
                    var potholeTask = Task.Run(() => _potholeService.AnalyzeImage(imageBytes));

                    var results = await Task.WhenAll(fireTask, accidentTask, potholeTask);

                    // ✅ كل النتايج فوق 50%
                    var validResults = results
                        .Where(r => r.Tag != "None" && r.Confidence >= 0.5)
                        .OrderByDescending(r => r.Confidence)
                        .ToList();

                    if (validResults.Any())
                    {
                        finalTag = string.Join(",", validResults.Select(r => r.Tag));
                        topConfidence = validResults.First().Confidence;

                        // ✅ "Fire:0.85,Accident:0.72"
                        aiScores = string.Join(",", validResults.Select(r => $"{r.Tag}:{r.Confidence:0.00}"));
                    }
                }

                photoPath = await FileHelper.SaveFileAsync(model.Photo, _environment.WebRootPath, "reports");
            }

            var now = DateTime.UtcNow.AddHours(3);

            // 3. إنشاء الـ Report
            var report = new Report
            {
                Report_Description = $"Title: {model.Title}\nDescription: {model.Description}",
                Report_GeoLocation = model.Location,
                Report_Category = finalTag,
                AI_Category = finalTag,
                AI_Scores = aiScores,  // ✅ "Fire:0.85,Accident:0.72"
                Report_Submit = now.AddHours(3),
                CreatedAt = now,
                PhotoPath = photoPath,
                Citizen_ID = citizen.Citizen_ID,
                Confidence_Score = (float)Math.Round(topConfidence, 4),
            };

            var createdReport = await _reportRepo.CreateAsync(report);

            // 4. ربط الـ Report بالجهات المختصة لكل Category
            var categories = createdReport.AI_Category.Split(',');
            foreach (var category in categories)
            {
                var authorities = await _authorityRepo.GetByCategoryAsync(category.Trim());
                if (authorities == null || !authorities.Any()) continue;

                foreach (var auth in authorities)
                {
                    await _handleRepo.CreateAsync(new Handle
                    {
                        Report_ID = createdReport.Report_ID,
                        Authority_ID = auth.Authority_ID,
                        Status = "Pending",
                        LastUpdated = now
                    });

                    // ✅ إرسال Notification للـ Authority
                    await _authorityNotif.SendAsync(auth.Authority_ID, "NewReport", createdReport.Report_ID);
                }
            }

            // 5. إرسال Notification للـ Citizen
            await _notificationManager.FillAndSendAsync(citizen.Citizen_ID, "CreateReport");

            // 6. الـ Response
            return new ReportResponseDto
            {
                IsSuccess = true,
                ReportId = createdReport.Report_ID,
                FinalCategory = createdReport.AI_Category,
                AiScores = createdReport.AI_Scores,  // ✅
                FormattedConfidence = $"{createdReport.Confidence_Score * 100:0.#}%",
                SubmittedAt = createdReport.Report_Submit
            };
        }
    }
}