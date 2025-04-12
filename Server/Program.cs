using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.Services;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole(); // Hiển thị log trên console
    logging.AddDebug();   // Hiển thị log trong debug output (nếu dùng IDE như Visual Studio)
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
    .EnableSensitiveDataLogging() // Bật ghi log dữ liệu nhạy cảm
    .EnableDetailedErrors()   // Bật thông tin lỗi chi tiết
);
builder.Services.AddScoped<CharacterService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<MatchService>();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<RoomService>();
// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is missing");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)) // Sửa từ issuerSigningKey thành IssuerSigningKey
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    // Thêm hỗ trợ Bearer token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field (e.g., 'Bearer {token}')",
        Name = "Authorization",
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
            new string[] { }
        }
    });
});

builder.Services.AddScoped<DataSyncService>(sp =>
    new DataSyncService(
        sp.GetRequiredService<AppDbContext>(),
        Path.Combine(Directory.GetCurrentDirectory(), "Json") // Đường dẫn đến thư mục chứa file JSON
    ));

builder.Services.AddScoped<CardSyncService>(provider =>
    new CardSyncService(
        provider.GetRequiredService<AppDbContext>(),
        Path.Combine(Directory.GetCurrentDirectory(), "Json")
    ));

var app = builder.Build();
app.UseSwagger(); // Kích hoạt Swagger
app.UseSwaggerUI(); // Kích hoạt giao diện Swagger UI
// Configure the HTTP request pipeline
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();