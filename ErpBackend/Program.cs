using System.Text;
using ErpBackend.Data;
using ErpBackend.Repositories;
using ErpBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

// ─── Bootstrap Serilog trước mọi thứ ─────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/erp-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

// Postgres Npgsql 6.0+ yêu cầu UTC cho timestamp. 
// Bật switch này để giữ nguyên logic DateTime hiện tại của ứng dụng.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

try
{
    Log.Information("🚀 ERP.Vibe API đang khởi động...");

    var builder = WebApplication.CreateBuilder(args);

    // Sử dụng Serilog thay thế logging mặc định
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // CORS – cho phép cả dev (5173) lẫn production (port 3000)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
            policy.SetIsOriginAllowed(_ => true) // Cho phép tất cả origin ở Production (Render)
            .AllowAnyHeader()
            .AllowAnyMethod());
    });

    builder.Services.AddSwaggerGen();

    // Configure Entity Framework (SQLite/PostgreSQL)
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        if (connectionString != null && (connectionString.Contains("Host=") || connectionString.Contains("Server=")))
        {
            options.UseNpgsql(connectionString);
            Log.Information("🗄️ Sử dụng Database: PostgreSQL");
        }
        else
        {
            options.UseSqlite(connectionString);
            Log.Information("🗄️ Sử dụng Database: SQLite");
        }
    });
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();

    // Alert System
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<TelegramNotifier>();
    builder.Services.AddScoped<EmailNotifier>();
    builder.Services.AddScoped<AlertCheckerService>();
    builder.Services.AddScoped<PdfGeneratorService>();
    builder.Services.AddScoped<ExchangeRateService>();
    builder.Services.AddMemoryCache();
    builder.Services.AddHostedService<AlertBackgroundService>();

    // Authorization & RLS Services
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ErpBackend.Authorization.PermissionHandler>();

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("product.view", policy => policy.Requirements.Add(new ErpBackend.Authorization.PermissionRequirement("product.view")));
        options.AddPolicy("product.create", policy => policy.Requirements.Add(new ErpBackend.Authorization.PermissionRequirement("product.create")));
        options.AddPolicy("invoice.approve", policy => policy.Requirements.Add(new ErpBackend.Authorization.PermissionRequirement("invoice.approve")));
        options.AddPolicy("invoice.create", policy => policy.Requirements.Add(new ErpBackend.Authorization.PermissionRequirement("invoice.create")));
        options.AddPolicy("invoice.payment", policy => policy.Requirements.Add(new ErpBackend.Authorization.PermissionRequirement("invoice.payment")));
        options.AddPolicy("report.export", policy => policy.Requirements.Add(new ErpBackend.Authorization.PermissionRequirement("report.export")));
        options.AddPolicy("stock.import", policy => policy.Requirements.Add(new ErpBackend.Authorization.PermissionRequirement("stock.import")));
    });

    // Configure JWT Authentication
    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Secret not found.");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

    var app = builder.Build();

    // Auto-migrate database on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        Log.Information("✅ Database đã sẵn sàng.");
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        Log.Information("📖 Swagger UI: http://localhost:5013/swagger");
    }

    // Log mọi HTTP request vào/ra
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0}ms)";
    });

    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("✅ ERP.Vibe API chạy tại http://localhost:5013");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 API bị crash khi khởi động!");
}
finally
{
    Log.CloseAndFlush();
}
