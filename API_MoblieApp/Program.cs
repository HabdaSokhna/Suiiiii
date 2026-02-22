using CURD;
using Database;
using Database.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SIRS_API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration (مرة واحدة فقط)
builder.Services.AddDbContext<Ai_Reports_Context>(options =>
 options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
 .AddEntityFrameworkStores<Ai_Reports_Context>()
 .AddDefaultTokenProviders();

// 3. Dependency Injection
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ICitizenRepository, CitizenRepository>();
builder.Services.AddScoped<ICitizenPhoneRepository, CitizenPhoneRepository>();
builder.Services.AddScoped<IHandleRepository, HandleRepository>();
builder.Services.AddScoped<IAuthorityRepository, AuthorityRepository>();
builder.Services.AddScoped<IAuthorityContactRepository, AuthorityContactRepository>();
builder.Services.AddScoped<ITbNotificationRepository, TbNotificationRepository>();
builder.Services.AddScoped<ITokenService, CreateToken>();


var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ml", "best.onnx");
// تسجيل خدمة الـ AI كـ Singleton (تحميل الموديل مرة واحدة فقط)
builder.Services.AddSingleton<YoloService>(sp =>
{
    // الحصول على المسار الفعلي لفولدر wwwroot
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var modelPath = Path.Combine(env.WebRootPath, "ml", "best.onnx");

    // التحقق من وجود الملف قبل تشغيل السيرفر (عشان ما يضربش Error مخفي)
    if (!File.Exists(modelPath))
    {
        throw new FileNotFoundException($"Model file not found at: {modelPath}");
    }

    return new YoloService(modelPath);
});



builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["JWT:Key"];
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    // --- أضف هذا الجزء الصغير للتأكد من وصول الخطأ للـ Console ---
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("JWT Failed: " + context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

// 5. Controllers & Swagger
builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// 6. Middleware Pipeline (الترتيب هنا هو القانون)
if (app.Environment.IsDevelopment() || true) // تفعيل Swagger دائماً في Somee للتجربة
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseStaticFiles();
app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run(); 