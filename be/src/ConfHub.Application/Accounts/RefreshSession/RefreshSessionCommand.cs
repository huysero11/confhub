using MediatR;

namespace ConfHub.Application.Accounts.RefreshSession;

// RefreshToken: chuỗi controller đọc từ cookie; null khi trình duyệt không gửi cookie.
public sealed record RefreshSessionCommand(string? RefreshToken) : IRequest<SessionResult>;
