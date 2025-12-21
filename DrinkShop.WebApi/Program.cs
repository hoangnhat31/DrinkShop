using DrinkShop.Application.Interfaces;
using DrinkShop.Application.Services;
using DrinkShop.Infrastructure;
using Microsoft.EntityFrameworkCore;
using DrinkShop.WebApi.Utilities;
using DrinkShop.WebApi.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using DrinkShop.Application.constance;
using DrinkShop.Application.Settings;
using DrinkShop.Domain.Interfaces;
using DrinkShop.Infrastructure.Repositories;
using dotenv.net;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("DrinkShopCorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Dev: Cho phép localhost các cổng phổ biến để tránh lỗi khi đổi cổng Frontend
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Prod: Đọc từ Configuration (tốt hơn GetEnvironmentVariable vì nó đọc cả appsettings và env)
            var allowedOrigins = builder.Configuration["ALLOWED_ORIGINS"]?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) 
                ?? new[] { "https://yourdomain.com" };

            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Bắt buộc nếu dùng Cookie/Identity
        }
    });
});
// ==========================================
// 1. CẤU HÌNH CONTROLLERS & JSON & FILTER
// ==========================================
builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
});

// 2. Custom lại format lỗi trả về khi dữ liệu không hợp lệ (BadRequest)
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value != null && e.Value.Errors.Count > 0)
            .Select(e => $"{e.Key}: {string.Join(", ", e.Value!.Errors.Select(er => er.ErrorMessage))}")
            .ToList();

        return new BadRequestObjectResult(new
        {
            success = false,
            message = "Dữ liệu không hợp lệ",
            errors = errors
        });
    };
});
// ==========================================
// 2. CẤU HÌNH DATABASE
// ==========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString,
        b => b.MigrationsAssembly("DrinkShop.Infrastructure")));
// ==========================================
// 3. CẤU HÌNH AUTHENTICATION (JWT)
// ==========================================
var jwtSecret = builder.Configuration["JWT_SECRET"]; 
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "DrinkShop", // 👈 Phải khớp chính xác
            
            ValidateAudience = true,
            ValidAudience = "DrinkShopClient", // 👈 Phải khớp chính xác (không được để trống)
            
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT_SECRET"])),
            
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.WebHost.ConfigureKestrel(options =>
{
    // Giới hạn 20MB (tính bằng byte: 20 * 1024 * 1024)
    options.Limits.MaxRequestBodySize = 20971520; 
});

// ==========================================
// 4. CẤU HÌNH AUTHORIZATION (PHÂN QUYỀN)
// ==========================================
builder.Services.AddAuthorization(options =>
{
    // ... (Giữ nguyên các Policy của bạn) ...
    options.AddPolicy("CanViewProduct", policy => 
        policy.RequireClaim("Permission", Permissions.Product.View));

    options.AddPolicy("CanManageProduct", policy => 
        policy.RequireClaim("Permission", 
            Permissions.Product.Manage, 
            Permissions.Product.Create, 
            Permissions.Product.Edit, 
            Permissions.Product.Delete));

    options.AddPolicy("CanManageOrder", policy => 
        policy.RequireClaim("Permission", Permissions.Order.Manage));

    options.AddPolicy(Permissions.Order.ViewAll, policy => 
        policy.RequireClaim("Permission", Permissions.Order.ViewAll));

    options.AddPolicy(Permissions.Order.ViewMine, policy => 
        policy.RequireClaim("Permission", Permissions.Order.ViewMine));

    options.AddPolicy("CanManageVoucher", policy => 
        policy.RequireClaim("Permission",
            Permissions.Voucher.ViewAll, 
            Permissions.Voucher.Create, 
            Permissions.Voucher.Edit, 
            Permissions.Voucher.Delete)); 

    options.AddPolicy(Permissions.Statistic.ViewRevenue, policy => 
        policy.RequireClaim("Permission", Permissions.Statistic.ViewRevenue));

    options.AddPolicy(Permissions.Statistic.ViewTopProducts, policy => 
        policy.RequireClaim("Permission", Permissions.Statistic.ViewTopProducts));

    options.AddPolicy(Permissions.Statistic.ViewRating, policy => 
        policy.RequireClaim("Permission", Permissions.Statistic.ViewRating));

    options.AddPolicy(Permissions.Pos.CreateOrder, policy => 
        policy.RequireClaim("Permission", Permissions.Pos.CreateOrder));
});

// ==========================================
// 5. CẤU HÌNH SWAGGER
// ==========================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DrinkShop.WebApi",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập token theo định dạng: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ==========================================
// 6. ĐĂNG KÝ DEPENDENCY INJECTION (DI)
// ==========================================
builder.Services.Configure<MinioSetting>(builder.Configuration.GetSection("MinIO"));
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();

builder.Services.AddScoped<IPhanLoaiService, PhanLoaiService>();
builder.Services.AddScoped<ISanPhamService, SanPhamService>();
builder.Services.AddScoped<IGioHangService, GioHangService>();
builder.Services.AddScoped<IDonHangService, DonHangService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IDanhGiaService, DanhGiaService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<INguyenLieuService, NguyenLieuService>();
builder.Services.AddScoped<IPosService, PosService>();
builder.Services.AddScoped<IStatisticRepository, StatisticRepository>();
builder.Services.AddScoped<IThongKeService, ThongKeService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>(); 
        
        context.Database.Migrate(); 
        
    }
    catch (Exception ex)
    {
        // Ghi lại lỗi nếu có vấn đề về kết nối DB hoặc Migration
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

app.UseMiddleware<ExceptionMiddleware>();

app.UseRouting(); 
app.UseCors("DrinkShopCorsPolicy");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else 
{
    // Bảo mật Production
    app.UseHsts();
}

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();
app.Run();