using ConfHub.Api.ErrorHandling;
using ConfHub.Application;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ----- Log: cấu hình đọc từ mục "Serilog" trong appsettings.json (Console + Seq) -----
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

// ----- Service (DI) -----
builder.Services.AddApplication();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

// ----- Pipeline: request đi qua theo đúng thứ tự các dòng dưới -----
// Request logging đứng ngoài cùng để ghi được status cuối cùng (kể cả khi đã thành response lỗi).
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
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
