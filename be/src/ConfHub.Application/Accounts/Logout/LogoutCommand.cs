using MediatR;

namespace ConfHub.Application.Accounts.Logout;

// RefreshToken: chuỗi controller đọc từ cookie; null khi trình duyệt không gửi cookie.
public sealed record LogoutCommand(string? RefreshToken) : IRequest;
