using System.Diagnostics;
using ConfHub.Application.Common.Exceptions;
using ConfHub.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ConfHub.Api.ErrorHandling;

// Middleware bọc mọi thứ phía sau nó trong try/catch.
// Exception nào chưa được xử lý đều rơi vào catch ở đây và được đổi thành
// response lỗi JSON theo chuẩn ProblemDetails (RFC 9457) — một định dạng duy nhất cho cả API.
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IHostEnvironment environment,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Cho request đi tiếp vào các lớp phía sau (controller, MediatR, handler...).
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client đã tự ngắt kết nối (đóng tab, hủy request): không còn ai nhận response,
            // cũng không phải lỗi của hệ thống → bỏ qua, không ghi log lỗi.
        }
        catch (Exception exception)
        {
            // Response đã bắt đầu gửi về client thì không đổi được status/nội dung nữa:
            // ném lại để máy chủ tự ngắt kết nối.
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteProblemAsync(context, exception);
        }
    }

    // Dựng response lỗi 400 kèm danh sách mã lỗi theo từng trường.
    private static HttpValidationProblemDetails BuildValidationProblem(ValidationException exception)
    {
        // Gom mã lỗi theo từng trường: { "Title": ["NotEmptyValidator"] }. FE tự dịch mã sang vi/en.
        var codesByField = new Dictionary<string, List<string>>();
        foreach (var failure in exception.Errors)
        {
            // Trường gặp lần đầu thì tạo danh sách mới cho nó.
            if (!codesByField.TryGetValue(failure.PropertyName, out var codes))
            {
                codes = [];
                codesByField[failure.PropertyName] = codes;
            }

            codes.Add(failure.ErrorCode);
        }

        var problem = new HttpValidationProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
        };
        foreach (var (field, codes) in codesByField)
        {
            problem.Errors[field] = codes.ToArray();
        }

        problem.Extensions["code"] = "Validation";
        return problem;
    }

    // Dựng response lỗi thường: status + mã lỗi + mô tả.
    private static ProblemDetails BuildProblem(int status, string code, string? detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Detail = detail,
        };
        problem.Extensions["code"] = code;
        return problem;
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var problem = MapExceptionToProblem(exception);
        var status = problem.Status ?? StatusCodes.Status500InternalServerError;

        // Lỗi 4xx là kết quả nghiệp vụ bình thường (request logging đã ghi status);
        // chỉ lỗi 500 mới là sự cố cần ghi kèm stack trace.
        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }

        // Title: tên chuẩn của status (404 → "Not Found").
        // traceId: dùng để tìm đúng request này trong Seq.
        problem.Title = ReasonPhrases.GetReasonPhrase(status);
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        // Xóa header/nội dung dở dang (nếu có) rồi ghi response lỗi.
        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(
            problem,
            problem.GetType(),
            options: null,
            contentType: "application/problem+json");
    }

    // Chọn status + mã lỗi theo loại exception.
    private ProblemDetails MapExceptionToProblem(Exception exception)
    {
        if (exception is ValidationException validationException)
        {
            return BuildValidationProblem(validationException);
        }

        if (exception is NotFoundException)
        {
            return BuildProblem(StatusCodes.Status404NotFound, "NotFound", exception.Message);
        }

        if (exception is ConflictException conflictException)
        {
            return BuildProblem(StatusCodes.Status409Conflict, conflictException.Code, exception.Message);
        }

        // 422: dữ liệu đúng định dạng nhưng vi phạm quy tắc nghiệp vụ (khác 400 là sai định dạng).
        if (exception is DomainException domainException)
        {
            return BuildProblem(StatusCodes.Status422UnprocessableEntity, domainException.Code, exception.Message);
        }

        if (exception is UnauthorizedException unauthorizedException)
        {
            return BuildProblem(StatusCodes.Status401Unauthorized, unauthorizedException.Code, exception.Message);
        }

        if (exception is ForbiddenException forbiddenException)
        {
            return BuildProblem(StatusCodes.Status403Forbidden, forbiddenException.Code, exception.Message);
        }

        // Lỗi không lường trước → 500. Ngoài môi trường dev không trả message gốc
        // vì có thể lộ chi tiết nội bộ (tên bảng, đường dẫn...).
        var detail = environment.IsDevelopment() ? exception.Message : null;
        return BuildProblem(StatusCodes.Status500InternalServerError, "Internal", detail);
    }
}
