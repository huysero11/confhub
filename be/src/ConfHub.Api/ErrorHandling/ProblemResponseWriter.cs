using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ConfHub.Api.ErrorHandling;

// Ghi response lỗi ProblemDetails cho những chỗ KHÔNG đi qua ExceptionHandlingMiddleware:
// rate limiter (429) và xác thực / phân quyền (401, 403) tự trả response chứ không ném exception.
// Nhờ vậy mọi lỗi của API vẫn cùng 1 định dạng (status, title, detail, code, traceId).
public static class ProblemResponseWriter
{
    public static Task WriteAsync(
        HttpContext httpContext,
        int status,
        string code,
        string detail,
        CancellationToken cancellationToken)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = detail,
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        return httpContext.Response.WriteAsJsonAsync(
            problem,
            problem.GetType(),
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    }
}
