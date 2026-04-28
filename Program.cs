using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Library.API.Data;
using Scalar.AspNetCore; // Hỗ trợ giao diện API mới
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. ĐĂNG KÝ CÁC DỊCH VỤ (SERVICES)
builder.Services.AddControllers(); // Bắt buộc để nhận diện BooksController
// ĐĂNG KÝ BẢO MẬT JWT
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi(); // Tạo dữ liệu OpenAPI cho .NET 10

// ĐĂNG KÝ GIAO DIỆN NHẬP TOKEN CHO OPENAPI/SCALAR
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Dán chuỗi Token của bạn vào đây"
        });

        var requirement = new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        };

        foreach (var path in document.Paths.Values)
        {
            foreach (var operation in path.Operations.Values)
            {
                operation.Security.Add(requirement);
            }
        }
        return Task.CompletedTask;
    });
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
var app = builder.Build();

// 2. TỰ ĐỘNG TẠO/CẬP NHẬT DATABASE KHI KHỞI CHẠY
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<LibraryDbContext>();
        context.Database.Migrate(); 
        Console.WriteLine("--- Database has been updated successfully! ---");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while migrating the database: {ex.Message}");
    }
}

// 3. CẤU HÌNH PIPELINE XỬ LÝ YÊU CẦU (MIDDLEWARE)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Mở giao diện tại /scalar/v1 
}

app.UseHttpsRedirection();

app.UseAuthentication(); 
app.UseAuthorization();
// Ánh xạ các Controller để các đường dẫn như /api/books hoạt động
app.MapControllers(); 
app.Run();