using BLL.DTO.Authorization;
using BLL.DTO.Responce;
using BLL.Managers.Notification;
using BLL.Managers.Notifications;
using BLL.Service;
using Database;
using Database.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace BLL.Mangers.CitizenAccount
{
    public interface IRegisters
    {
        Task<RegisterResponceDto> ExecuteAsync(RegisterDto model);
    }

    public class Registers : IRegisters
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Ai_Reports_Context _context;
        private readonly ICitizenNotificationManager _notificationManager;
        private readonly EmailService _emailService;
        private readonly OtpStore _otpStore;
        private readonly ILogger<Registers> _logger;

        public Registers(
            UserManager<ApplicationUser> userManager,
            Ai_Reports_Context context,
            ICitizenNotificationManager notificationManager,
            EmailService emailService,
            OtpStore otpStore,
            ILogger<Registers> logger)
        {
            _userManager = userManager;
            _context = context;
            _notificationManager = notificationManager;
            _emailService = emailService;
            _otpStore = otpStore;
            _logger = logger;
        }

        public async Task<RegisterResponceDto> ExecuteAsync(RegisterDto model)
        {
            // ✅ جيب المستخدم مرة واحدة بس
            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                // لو موجود بس مش مفعّل → امسحه وابدأ من أول
                if (!existingUser.EmailConfirmed)
                {
                    var existingCitizen = await _context.TbCitizen
                        .Include(c => c.LstPhone)
                        .FirstOrDefaultAsync(c => c.ApplicationUserId == existingUser.Id);

                    if (existingCitizen != null)
                    {
                        // ✅ امسح الـ Phones الأول عشان ما يحصلش Foreign Key error
                        _context.TbCitizen_Phone.RemoveRange(existingCitizen.LstPhone);
                        _context.TbCitizen.Remove(existingCitizen);
                        await _context.SaveChangesAsync();
                    }

                    await _userManager.DeleteAsync(existingUser);
                }
                else
                {
                    return new RegisterResponceDto
                    {
                        IsSuccess = false,
                        Message = "البريد الإلكتروني مستخدم بالفعل."
                    };
                }
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                ApplicationUser? user = null;

                try
                {
                    user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        CreatedAt = DateTime.UtcNow,
                        EmailConfirmed = false  // مش مفعّل لحد ما يدخل OTP
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (!result.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return new RegisterResponceDto
                        {
                            IsSuccess = false,
                            Message = "فشل إنشاء الحساب",
                            Errors = result.Errors.Select(e => e.Description)
                        };
                    }

                    await _userManager.AddToRoleAsync(user, "Citizen");

                    var citizenProfile = new Citizen
                    {
                        ApplicationUserId = user.Id,
                        Citizen_Name = model.FullName,
                        Citizen_National_Id = model.NationalId,
                        CreatedAt = DateTime.UtcNow
                    };
                    citizenProfile.LstPhone.Add(new Citizen_Phone { Phone_Number = model.PhoneNumber });

                    _context.TbCitizen.Add(citizenProfile);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // ✅ ابعت OTP بعد ما الحساب اتعمل
                    var otpCode = new Random().Next(100000, 999999).ToString();
                    _otpStore.Save(model.Email, otpCode);
                    await _emailService.SendOtpAsync(model.Email, otpCode);
                    _logger.LogInformation("OTP أُرسل للمستخدم {Email} عند التسجيل", model.Email);

                    _ = _notificationManager.FillAndSendAsync(citizenProfile.Citizen_ID, "Register");

                    return new RegisterResponceDto
                    {
                        IsSuccess = true,
                        Message = "تم إنشاء الحساب، تحقق من الإيميل لتأكيد حسابك"
                    };
                }
                catch (Exception ex)
                {
                    if (transaction.GetDbTransaction().Connection != null)
                        await transaction.RollbackAsync();

                    // ✅ لو حصل error امسح الـ User لو اتعمل
                    if (user != null)
                        await _userManager.DeleteAsync(user);

                    _logger.LogError("خطأ في التسجيل: {Message}", ex.Message);

                    return new RegisterResponceDto
                    {
                        IsSuccess = false,
                        Message = "حصل خطأ، حاول تاني",
                        Errors = new List<string> { ex.Message }
                    };
                }
            });
        }
    }
}