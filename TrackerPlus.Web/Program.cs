using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Serilog;
using System.Globalization;
using TrackerPlus.Core.Common;
using TrackerPlus.Web.Localization;
using TrackerPlus.Web.Resources;
using TrackerPlus.Core.Interfaces.Repositories;
using TrackerPlus.Core.Interfaces.Services;
using TrackerPlus.Repository.Infrastructure;
using TrackerPlus.Repository.Repositories;
using TrackerPlus.Services.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration));

// Configuration
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.Configure<AdminAuthSettings>(builder.Configuration.GetSection("AdminAuth"));
var adminAuthDefaults = builder.Configuration.GetSection("AdminAuth").Get<AdminAuthSettings>() ?? new AdminAuthSettings();

// Database
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

// Repositories
builder.Services.AddScoped<ITrackerRepository, TrackerRepository>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IIMEIRepository, IMEIRepository>();
builder.Services.AddScoped<IPayLogRepository, PayLogRepository>();
builder.Services.AddScoped<IWebServiceIPRepository, WebServiceIPRepository>();
builder.Services.AddScoped<IFirmwareRepository, FirmwareRepository>();
builder.Services.AddScoped<IHistoryRepository, HistoryRepository>();
builder.Services.AddScoped<ILabelRepository, LabelRepository>();
builder.Services.AddScoped<IGeofenceRepository, GeofenceRepository>();
builder.Services.AddScoped<IOBMRepository, OBMRepository>();
builder.Services.AddScoped<IMapMarkRepository, MapMarkRepository>();
builder.Services.AddScoped<IDeviceSettingsRepository, DeviceSettingsRepository>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IDealerRepository, DealerRepository>();
builder.Services.AddScoped<ISubAdminRepository, SubAdminRepository>();
builder.Services.AddScoped<IUdFieldRepository, UdFieldRepository>();
builder.Services.AddScoped<IReportFieldsRepository, ReportFieldsRepository>();

// Services
builder.Services.AddScoped<ITrackerService, TrackerService>();
builder.Services.AddScoped<IDeviceSettingsService, DeviceSettingsService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IIMEIService, IMEIService>();
builder.Services.AddScoped<IPayLogService, PayLogService>();
builder.Services.AddScoped<IWebServiceIPService, WebServiceIPService>();
builder.Services.AddScoped<IFirmwareService, FirmwareService>();
builder.Services.AddScoped<IHistoryService, HistoryService>();
builder.Services.AddScoped<ILabelService, LabelService>();
builder.Services.AddScoped<IGeofenceService, GeofenceService>();
builder.Services.AddScoped<IOBMService, OBMService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IDealerService, DealerService>();
builder.Services.AddSingleton<IGoogleApiKeyService, GoogleApiKeyService>();

// Authentication - member cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie("ExternalCookie", options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        options.Cookie.Name = "TrackerPlus.External";
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["AppSettings:OAuth:GmailClientId"] ?? "";
        options.ClientSecret = builder.Configuration["AppSettings:OAuth:GmailKey"] ?? "";
        options.SignInScheme = "ExternalCookie";
        options.CallbackPath = "/signin-google";
        options.Scope.Add("email");
        options.Scope.Add("profile");
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "TrackerPlus.Auth";
    })
    .AddCookie("AdminCookie", options =>
    {
        options.LoginPath = "/Admin/Account/Login";
        options.LogoutPath = "/Admin/Account/Logout";
        options.AccessDeniedPath = "/Admin/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(adminAuthDefaults.RememberMeMinutes);
        options.SlidingExpiration = false;
        options.Cookie.Name = "TrackerPlus.Admin";
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = context =>
            {
                var settings = context.HttpContext.RequestServices
                    .GetRequiredService<IOptions<AdminAuthSettings>>().Value;
                var idleLimit = TimeSpan.FromMinutes(settings.IdleTimeoutMinutes);
                var now = DateTimeOffset.UtcNow;

                if (!context.Properties.Items.TryGetValue("LastActivityUtc", out var raw)
                    || !DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var last))
                    last = context.Properties.IssuedUtc ?? now;

                if (now - last > idleLimit)
                {
                    context.RejectPrincipal();
                    return Task.CompletedTask;
                }

                context.Properties.Items["LastActivityUtc"] = now.ToString("o", CultureInfo.InvariantCulture);
                context.ShouldRenew = true;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddLocalization();

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (_, factory) =>
            factory.Create(typeof(SharedResources));
    });

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = LocalizationCulture.SupportedCultures
        .Select(c => new CultureInfo(c))
        .ToList();
    options.DefaultRequestCulture = new RequestCulture(LocalizationCulture.DefaultCulture);
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
    options.RequestCultureProviders.Add(new UserLanguageRequestCultureProvider());
    options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
});

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Area route for Admin
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Tracker}/{action=Index}/{id?}");

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
