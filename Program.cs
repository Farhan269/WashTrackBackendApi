using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using wsahRecieveDelivary.Data;
using wsahRecieveDelivary.Services;
using wsahRecieveDelivary.Filters;
using OfficeOpenXml;
using CsvHelper;
using System.Globalization;
using wsahRecieveDelivary.Repository;
using wsahRecieveDelivary.IRepository;
using wsahRecieveDelivary.WashService;
using wsahRecieveDelivary.Dapper;

// ✅ NEW: Setup Serilog before building app
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/wsah-sync-.txt",
        rollingInterval: RollingInterval.Day,  // New file each day
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "wsahRecieveDelivary")
    .CreateLogger();

try
{
    Log.Information("🚀 Application starting...");

    // Set EPPlus License Context
    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    var builder = WebApplication.CreateBuilder(args);

    // ✅ NEW: Add Serilog to ASP.NET Core
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();

    // ==========================================
    // ? ADD CORS CONFIGURATION - ALLOW ALL ORIGINS
    // ==========================================
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
    });

    // Database Configuration
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not found");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();


    ///Dapper
    builder.Services.AddSingleton<WashDhuContext>();
    builder.Services.AddSingleton<WashDhuTWLContext>();
    builder.Services.AddSingleton<TusukaExtremeContext>();

    // Register Services
    builder.Services.AddScoped<IJwtService, JwtService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IWorkOrderService, WorkOrderService>();
    builder.Services.AddScoped<IWashTransactionService, WashTransactionService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IWashPlan, WashPlanService>();
    builder.Services.AddScoped<IOutServiceApi, OutApiService>();
    builder.Services.AddScoped<IWashDhuRepository, WashDhuRepository>();
    builder.Services.AddScoped<ITusukaExtremeRepository, TusukaExtremeRepository>();

    // ✅ External API Sync
    builder.Services.AddHttpClient<IExternalApiSyncService, ExternalApiSyncService>();
    builder.Services.AddScoped<IExternalApiSyncService, ExternalApiSyncService>();

    //service
    builder.Services.AddScoped<WashDhuService>();

    // ✅ Auto Sync Background Service (every 10 minutes)
    builder.Services.AddHostedService<WorkOrderAutoSyncService>();

    // Swagger Configuration
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Wsah Receive Delivery API",
            Version = "v1",
            Description = "Authentication & WorkOrder Management API with JWT"
        });

        c.OperationFilter<FileUploadOperationFilter>();

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // ✅ NEW: Add Serilog request logging
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowAll");

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("🌍 Application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// For EF Core Migrations (Design Time)
public partial class Program { }

//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;
//using System.Text;
//using wsahRecieveDelivary.Data;
//using wsahRecieveDelivary.Services;
//using wsahRecieveDelivary.Filters;
//using OfficeOpenXml;
//using CsvHelper;
//using System.Globalization;
//// Set EPPlus License Context
//ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.AddControllers();

//// ==========================================
//// ? ADD CORS CONFIGURATION - ALLOW ALL ORIGINS
//// ==========================================
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll",
//        policy =>
//        {
//            policy.AllowAnyOrigin()      // Allow any origin
//                  .AllowAnyMethod()      // Allow any HTTP method (GET, POST, PUT, DELETE, etc.)
//                  .AllowAnyHeader();     // Allow any header
//        });
//});

//// Database Configuration
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//// JWT Authentication
//var jwtSettings = builder.Configuration.GetSection("JwtSettings");
//var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not found");

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = jwtSettings["Issuer"],
//        ValidAudience = jwtSettings["Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
//        ClockSkew = TimeSpan.Zero
//    };
//});

//builder.Services.AddAuthorization();

//// Register Services
//builder.Services.AddScoped<IJwtService, JwtService>();
//builder.Services.AddScoped<IAuthService, AuthService>();
//builder.Services.AddScoped<IWorkOrderService, WorkOrderService>();
//builder.Services.AddScoped<IWashTransactionService, WashTransactionService>();
//builder.Services.AddScoped<IUserService, UserService>();

//// ✅ NEW: External API Sync
//builder.Services.AddHttpClient<IExternalApiSyncService, ExternalApiSyncService>();
//builder.Services.AddScoped<IExternalApiSyncService, ExternalApiSyncService>();

//// ✅ NEW: Auto Sync Background Service (every 15 minutes)
//builder.Services.AddHostedService<WorkOrderAutoSyncService>();
//// Swagger Configuration
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "Wsah Receive Delivery API",
//        Version = "v1",
//        Description = "Authentication & WorkOrder Management API with JWT"
//    });

//    // File Upload Support
//    c.OperationFilter<FileUploadOperationFilter>();

//    // Add JWT Authentication to Swagger
//    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
//        Name = "Authorization",
//        In = ParameterLocation.Header,
//        Type = SecuritySchemeType.ApiKey,
//        Scheme = "Bearer"
//    });

//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            Array.Empty<string>()
//        }
//    });
//});

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
//app.UseSwagger();
//app.UseSwaggerUI();
//app.UseCors("AllowAll");


//app.UseHttpsRedirection();

//// ==========================================
//// ? USE CORS POLICY - MUST BE BEFORE Authentication/Authorization
//// ==========================================


//app.UseAuthentication();
//app.UseAuthorization();
//app.MapControllers();
//app.Run();

//// For EF Core Migrations (Design Time)
//public partial class Program { }