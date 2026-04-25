using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Library.API.Data;
using Scalar.AspNetCore; // Hỗ trợ giao diện API mới

var builder = WebApplication.CreateBuilder(args);

// 1. ĐĂNG KÝ CÁC DỊCH VỤ (SERVICES)
builder.Services.AddControllers(); // Bắt buộc để nhận diện BooksController [cite: 735]

builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi(); // Tạo dữ liệu OpenAPI cho .NET 10 [cite: 728]
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
var app = builder.Build();

// 2. TỰ ĐỘNG TẠO/CẬP NHẬT DATABASE KHI KHỞI CHẠY
// Giúp né lỗi bị Windows chặn khi dùng lệnh CLI 'dotnet ef database update' [cite: 782]
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

// Ánh xạ các Controller để các đường dẫn như /api/books hoạt động [cite: 735]
app.MapControllers(); 

// API mẫu mặc định (WeatherForecast)
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

// Định nghĩa record cho WeatherForecast
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}