using ConfHub.Api.Authentication;
using ConfHub.Api.Authorization;
using ConfHub.Api.ErrorHandling;
using ConfHub.Api.RateLimiting;
using ConfHub.Application;
using ConfHub.Infrastructure;
using ConfHub.Infrastructure.Persistence.Seeding;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ----- Log: cấu hình đọc từ mục "Serilog" trong appsettings.json (Console + Seq) -----
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

// ----- Service (DI) -----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers(options =>
{
    // Không tự bắt buộc thuộc tính string không-null: để FluentValidation kiểm và trả lỗi
    // theo đúng 1 định dạng (mã lỗi từng trường) thay vì lỗi mặc định của ASP.NET Core.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddAuthRateLimiting(builder.Configuration);
builder.Services.AddJwtAuthentication();
builder.Services.AddPermissionAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

// ----- Pipeline: request đi qua theo đúng thứ tự các dòng dưới -----
// Request logging đứng ngoài cùng để ghi được status cuối cùng (kể cả khi đã thành response lỗi).
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseRateLimiter();

// Authentication (đây là ai?) phải đứng trước Authorization (được làm việc này không?).
app.UseAuthentication();
app.UseAuthorization();

// ----- Endpoint -----
// Mặc định mọi endpoint phải đăng nhập (FallbackPolicy) → trang công khai phải ghi rõ AllowAnonymous.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

// ----- Tài khoản mẫu cho máy dev (bật bằng DevSeed:Enabled, cần chạy migration trước) -----
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DevAccountSeeder>().SeedAsync(CancellationToken.None);
}

await app.RunAsync();
