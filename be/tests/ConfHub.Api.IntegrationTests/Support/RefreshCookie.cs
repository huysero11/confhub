using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ConfHub.Api.IntegrationTests.Support;

// Test tự đọc / tự gửi cookie refresh token (HttpClient của test không tự giữ cookie)
// để chủ động gửi lại cookie cũ, cookie đã thu hồi...
public static class RefreshCookie
{
    public const string Name = "confhub_refresh";

    // Dòng Set-Cookie của cookie refresh, vd "confhub_refresh=abc; expires=...; path=/api/auth; secure; ...".
    public static string ReadHeader(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            foreach (var setCookieHeader in setCookieHeaders)
            {
                if (setCookieHeader.StartsWith(Name + "=", StringComparison.Ordinal))
                {
                    return setCookieHeader;
                }
            }
        }

        throw new InvalidOperationException("The response did not set the refresh cookie.");
    }

    // Giá trị của cookie (refresh token gốc); chuỗi rỗng khi server xóa cookie.
    public static string ReadValue(HttpResponseMessage response)
    {
        var header = ReadHeader(response);
        var firstPart = header.Split(';')[0];
        return firstPart[(Name.Length + 1)..];
    }

    public static HttpRequestMessage Post(string path, string? cookieValue, object? body = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative));
        if (cookieValue is not null)
        {
            request.Headers.Add("Cookie", $"{Name}={cookieValue}");
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    public static HttpRequestMessage Get(string path, string? accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return request;
    }
}
