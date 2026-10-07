namespace ConfHub.Application.Common.Security;

// Value: chuỗi JWT. ExpiresInSeconds: còn bao nhiêu giây thì hết hạn (giao diện dùng để biết lúc làm mới).
public sealed record AccessToken(string Value, int ExpiresInSeconds);
