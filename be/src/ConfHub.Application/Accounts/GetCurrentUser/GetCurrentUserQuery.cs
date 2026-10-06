using MediatR;

namespace ConfHub.Application.Accounts.GetCurrentUser;

// Không có tham số: "người đang gọi" lấy từ ICurrentUser, không nhận từ client.
public sealed record GetCurrentUserQuery : IRequest<CurrentUserResponse>;
