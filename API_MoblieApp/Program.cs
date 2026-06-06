using BLL.BackgroundJobs;
using BLL.Managers.Authority;
using BLL.Managers.Notification;
using BLL.Managers.Notifications;
using BLL.Managers.ReportCitizen;
using BLL.Managers.User;
using BLL.Mangers.Authority;
using BLL.Mangers.CitizenAccount;
using BLL.Service;
using CURD;
using Database;
using Database.Domain;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region Connection String
builder.Services.AddDbContext<Ai_Reports_Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<Ai_Reports_Context>()
    .AddDefaultTokenProviders();
#endregion

#region Firebase Configuration
var pathToKey = Path.Combine(Directory.GetCurrentDirectory(), "firebase-config.json");
if (FirebaseApp.DefaultInstance == null)
{
    using (var stream = new FileStream(pathToKey, FileMode.Open, FileAccess.Read))
    {
        FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromStream(stream),
            ProjectId = "sirs-e3927"
        });
    }
}
#endregion

#region Dependency Injection
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ICitizenRepository, CitizenRepository>();
builder.Services.AddScoped<ICitizenPhoneRepository, CitizenPhoneRepository>();
builder.Services.AddScoped<IHandleRepository, HandleRepository>();
builder.Services.AddScoped<IAuthorityRepository, AuthorityRepository>();
builder.Services.AddScoped<IAuthorityContactRepository, AuthorityContactRepository>();
builder.Services.AddScoped<ITbNotificationRepository, TbNotificationRepository>();
builder.Services.AddScoped<ITokenService, CreateToken>();
builder.Services.AddScoped<INotificationService, FirebaseNotificationService>();
builder.Services.AddScoped<IGetReportAuthority, GetReportAuthority>();
builder.Services.AddScoped<INotificationManager, NotificationManager>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<ILogin, LoginCitizenManager>();
builder.Services.AddScoped<IRegisters, Registers>();
builder.Services.AddScoped<IGetReportById, GetReportById>();
builder.Services.AddScoped<ISystemNotificationService, NotificationService>();
builder.Services.AddScoped<IGetHistoryManager, GetHistoryManager>();
builder.Services.AddScoped<ICreateReport, CreateReport>();
builder.Services.AddScoped<IProfileManager, ProfileManager>();
builder.Services.AddScoped<ILoginAuthority, LoginAuthority>();
builder.Services.AddScoped<ICitizenNotificationManager, CitizenNotificationManager>();
builder.Services.AddHttpClient<IGeocodingService, GeocodingService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<QrCodeService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddSingleton<OtpStore>();
builder.Services.AddScoped<IOtp, OtpManager>();
builder.Services.AddScoped<IForgetPassword, ForgetPassword>();
builder.Services.AddScoped<IAuthorityNotificationService, AuthorityNotificationService>();
builder.Services.AddScoped<IAuthorityNotificationManager, AuthorityNotificationManager>();
builder.Services.AddHostedService<PendingReportNotificationJob>();
builder.Services.AddHostedService<UnverifiedAccountCleanupJob>();
#endregion

#region Yolo Service
builder.Services.AddKeyedSingleton<YoloService>("FireService", (sp, key) => {
    var path = Path.Combine(sp.GetRequiredService<IWebHostEnvironment>().WebRootPath, "ml", "fire.onnx");
    return new YoloService(path, "Fire");
});
builder.Services.AddKeyedSingleton<YoloService>("AccidentService", (sp, key) => {
    var path = Path.Combine(sp.GetRequiredService<IWebHostEnvironment>().WebRootPath, "ml", "carraccident.onnx");
    return new YoloService(path, "Accident");
});
builder.Services.AddKeyedSingleton<YoloService>("PotholeService", (sp, key) => {
    var path = Path.Combine(sp.GetRequiredService<IWebHostEnvironment>().WebRootPath, "ml", "potholes.onnx");
    return new YoloService(path, "Pothole");
});
#endregion

#region JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["JWT:Key"] ?? "A_Very_Secret_Default_Key_For_SIRS_Project_2026";
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
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("JWT Auth Failed: " + context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});
#endregion

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();

#region Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("authority", new OpenApiInfo { Title = "SIRS - Authority API", Version = "v1" });
    options.SwaggerDoc("citizen", new OpenApiInfo { Title = "SIRS - Citizen API", Version = "v1" });

    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        var groupName = apiDesc.GroupName;
        return groupName == docName;
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token"
    });

    try
    {
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    }
    catch { }
});
#endregion

#region CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DynamicCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
#endregion


var app = builder.Build();



app.UseRouting();

app.UseCors("DynamicCorsPolicy");


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/authority/swagger.json", "Authority System");
    c.SwaggerEndpoint("/swagger/citizen/swagger.json", "Citizen System");
    c.RoutePrefix = "swagger";
});


app.UseStaticFiles();


app.MapGet("/firebase-messaging-sw.js", async context =>
{
    context.Response.ContentType = "application/javascript";
    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "firebase-messaging-sw.js");
    await context.Response.SendFileAsync(filePath);
});


app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();