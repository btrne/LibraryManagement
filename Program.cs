using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Library.API.Data;
using Scalar.AspNetCore;
using Library.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 1. ĐĂNG KÝ CÁC DỊCH VỤ (DEPENDENCY INJECTION)

// 1.1. Core Services
builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// 1.2. Database Context
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1.3. Authentication & Authorization (JWT)
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

// 1.4. OpenAPI & Giao diện Scalar (Kèm cấu hình nhập Token)
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

// 2. XÂY DỰNG ỨNG DỤNG VÀ CHẠY MIGRATION

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

// Tự động tạo/cập nhật Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<LibraryDbContext>();
        await context.Database.MigrateAsync(); 
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
    app.MapScalarApiReference(); // Mở giao diện API tại /scalar/v1 
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers(); 

app.Run();