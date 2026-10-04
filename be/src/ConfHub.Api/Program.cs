using ConfHub.Api.ErrorHandling;
using ConfHub.Api.RateLimiting;
using ConfHub.Application;
using ConfHub.Infrastructure;
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
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

// ----- Pipeline: request đi qua theo đúng thứ tự các dòng dưới -----
// Request logging đứng ngoài cùng để ghi được status cuối cùng (kể cả khi đã thành response lỗi).
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthorization();

// ----- Endpoint -----
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
