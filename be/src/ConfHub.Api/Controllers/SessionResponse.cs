using ConfHub.Application.Accounts;

namespace ConfHub.Api.Controllers;

// Body của login / refresh. ExpiresIn: số giây access token còn hạn.
// Không có refresh token ở đây: nó chỉ đi trong cookie HttpOnly.
public sealed record SessionResponse(string AccessToken, int ExpiresIn, CurrentUserResponse User);
